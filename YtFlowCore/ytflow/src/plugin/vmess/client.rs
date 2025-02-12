use std::sync::{Arc, Weak};
use std::time::{SystemTime, UNIX_EPOCH};

use async_trait::async_trait;
use getrandom::getrandom;
use rand::prelude::*;

use super::protocol::body::{
    AesCfbCryptoFactory, AesGcmCryptoFactory, BodyCryptoFactory, ChachaPolyCryptoFactory,
    NoneCryptoFactory, ShakeSizeCrypto, TxCrypto,
};
use super::protocol::header::{
    AeadRequestEnc, AesCfbRequestEnc, RequestHeader, RequestHeaderEnc, VMESS_HEADER_CMD_TCP,
    VMESS_HEADER_OPT_SHAKE, VMESS_HEADER_OPT_STD,
};
use super::protocol::USER_ID_LEN;
use super::stream::VMessClientStream;
use super::SupportedSecurity;
use crate::flow::*;

pub struct VMessStreamOutboundFactory {
    // TODO: store cmd_key
    user_id: [u8; USER_ID_LEN],
    use_aead: bool,
    security: SupportedSecurity,
    next: Weak<dyn StreamOutboundFactory>,
}

impl VMessStreamOutboundFactory {
    pub fn new(
        user_id: [u8; USER_ID_LEN],
        alter_id: u16,
        security: SupportedSecurity,
        next: Weak<dyn StreamOutboundFactory>,
    ) -> Self {
        Self {
            user_id,
            use_aead: alter_id == 0,
            security,
            next,
        }
    }
}

struct StreamCreator<'a, RE> {
    context: &'a mut FlowContext,
    initial_data: &'a [u8],
    req_enc: RE,
    next: Arc<dyn StreamOutboundFactory>,
}

async fn create_client_stream<RE: RequestHeaderEnc, F: BodyCryptoFactory>(
    context: &mut FlowContext,
    initial_data: &'_ [u8],
    req_enc: RE,
    body_crypto_factory: F,
    next: Arc<dyn StreamOutboundFactory>,
) -> FlowResult<Box<dyn Stream>>
where
    RE::Dec: Send + Sync + 'static,
    <F as BodyCryptoFactory>::Tx<ShakeSizeCrypto>: Send + Sync + 'static,
    <F as BodyCryptoFactory>::Rx<ShakeSizeCrypto>: Send + Sync + 'static,
{
    let mut tx_crypto;
    let rx_crypto;
    let rx_size_crypto;
    let header_dec;
    let (stream, initial_res) = {
        let mut req_buf = vec![0; RE::REQUIRED_SIZE];
        let mut request = RequestHeader {
            ver: 1,
            res_auth: rand::thread_rng().gen(),
            opt: VMESS_HEADER_OPT_STD | VMESS_HEADER_OPT_SHAKE,
            cmd: VMESS_HEADER_CMD_TCP,
            port: context.remote_peer.port,
            addr: (&context.remote_peer.host).into(),
            ..Default::default()
        };
        getrandom(&mut request.data_iv).unwrap();
        getrandom(&mut request.data_key).unwrap();
        request.set_padding_len(rand::thread_rng().gen_range(0..=0b1111));
        getrandom(request.padding_mut()).unwrap();
        request.set_encryption(F::HEADER_SEC_TYPE);
        let res_iv = req_enc.derive_res_iv(&request);
        let res_key = req_enc.derive_res_key(&request);
        let (req_len, dec) = req_enc.encrypt_req(&mut request, &mut req_buf).unwrap();
        header_dec = dec;
        req_buf.truncate(req_len);

        rx_size_crypto = ShakeSizeCrypto::new(&res_iv);

        tx_crypto = body_crypto_factory.new_tx(
            &request.data_key,
            &request.data_iv,
            ShakeSizeCrypto::new(&request.data_iv),
        );
        rx_crypto = body_crypto_factory.new_rx(&res_key, &res_iv, rx_size_crypto);
        if !initial_data.is_empty() {
            let (pre_overhead_len, post_overhead_len) =
                tx_crypto.calculate_overhead(initial_data.len());
            req_buf.reserve(pre_overhead_len + initial_data.len() + post_overhead_len);
            let offset = req_buf.len();
            req_buf.resize(offset + pre_overhead_len, 0);
            req_buf.extend_from_slice(initial_data);
            req_buf.resize(req_buf.len() + post_overhead_len, 0);
            let (pre_overhead, remaining) = req_buf[offset..].split_at_mut(pre_overhead_len);
            let (payload, post_overhead) = remaining.split_at_mut(initial_data.len());
            tx_crypto.seal(pre_overhead, payload, post_overhead)
        }

        next.create_outbound(context, &req_buf).await?
    };

    let reader = StreamReader::new(4096, initial_res);
    Ok(Box::new(VMessClientStream::new(
        stream, reader, header_dec, rx_crypto, tx_crypto,
    )))
}

impl<'a, RE: RequestHeaderEnc> StreamCreator<'a, RE>
where
    RE::Dec: Send + Sync + 'static,
{
    async fn create_stream(
        self,
        security: SupportedSecurity,
        header_aead: bool,
    ) -> FlowResult<Box<dyn Stream>> {
        match security {
            SupportedSecurity::None => {
                create_client_stream(
                    self.context,
                    self.initial_data,
                    self.req_enc,
                    NoneCryptoFactory {},
                    self.next,
                )
                .await
            }
            SupportedSecurity::Auto => panic!("Auto is not a valid factory type"),
            SupportedSecurity::Aes128Cfb => {
                create_client_stream(
                    self.context,
                    self.initial_data,
                    self.req_enc,
                    AesCfbCryptoFactory {
                        process_header_ciphertext: !header_aead,
                    },
                    self.next,
                )
                .await
            }
            SupportedSecurity::Aes128Gcm => {
                create_client_stream(
                    self.context,
                    self.initial_data,
                    self.req_enc,
                    AesGcmCryptoFactory {},
                    self.next,
                )
                .await
            }
            SupportedSecurity::Chacha20Poly1305 => {
                create_client_stream(
                    self.context,
                    self.initial_data,
                    self.req_enc,
                    ChachaPolyCryptoFactory {},
                    self.next,
                )
                .await
            }
        }
    }
}

impl VMessStreamOutboundFactory {
    async fn create_outbound_core(
        &self,
        context: &mut FlowContext,
        initial_data: &[u8],
        next: Arc<dyn StreamOutboundFactory>,
    ) -> FlowResult<Box<dyn Stream>> {
        let timestamp = SystemTime::now().duration_since(UNIX_EPOCH).unwrap();

        let stream = if self.use_aead {
            let rand = rand::thread_rng().gen();
            StreamCreator {
                context,
                initial_data,
                req_enc: AeadRequestEnc::new(timestamp.as_secs(), &self.user_id, rand),
                next,
            }
            .create_stream(self.security, true)
            .await
        } else {
            StreamCreator {
                context,
                initial_data,
                req_enc: AesCfbRequestEnc::new(timestamp.as_secs(), &self.user_id),
                next,
            }
            .create_stream(self.security, false)
            .await
        }?;
        Ok(stream)
    }
}

#[async_trait]
impl StreamOutboundFactory for VMessStreamOutboundFactory {
    async fn create_outbound(
        &self,
        context: &mut FlowContext,
        initial_data: &[u8],
    ) -> FlowResult<(Box<dyn Stream>, Buffer)> {
        let next = self.next.upgrade().ok_or(FlowError::UnexpectedData)?;

        let stream = self
            .create_outbound_core(context, initial_data, next)
            .await?;
        Ok((stream, Buffer::new()))
    }
}

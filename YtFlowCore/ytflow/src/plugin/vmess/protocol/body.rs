mod aead;
mod aes_cfb;
mod factory;
mod none;
mod shake;

use crate::flow::FlowResult;
pub use aead::{AesGcmCryptoFactory, ChachaPolyCryptoFactory};
pub use aes_cfb::AesCfbCryptoFactory;
pub use factory::BodyCryptoFactory;
pub use none::NoneCryptoFactory;
pub use shake::ShakeSizeCrypto;

pub trait SizeCrypto {
    const LEN: usize;
    fn encode_size(&mut self, size: usize) -> [u8; Self::LEN];
    fn decode_size(&mut self, size_bytes: &mut [u8; Self::LEN]) -> FlowResult<usize>;
}

pub trait RxCrypto {
    fn peek_header_ciphertext(&mut self, _header_ciphertext: &mut [u8]) {}
    fn expected_next_size_len(&mut self) -> usize;
    fn on_size(&mut self, size_bytes: &mut [u8]) -> FlowResult<usize>;
    fn expected_next_chunk_len(&mut self) -> usize;
    fn on_chunk<'c>(&mut self, chunk: &'c mut [u8]) -> FlowResult<&'c mut [u8]>;
}

pub trait TxCrypto {
    fn calculate_overhead(&mut self, next_payload_len: usize) -> (usize, usize);
    fn seal(&mut self, pre_overhead: &mut [u8], payload: &mut [u8], post_overhead: &mut [u8]);
}

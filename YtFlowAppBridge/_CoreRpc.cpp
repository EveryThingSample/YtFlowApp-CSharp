#include "pch.h"
#include "_CoreRpc.h"
#include "Bridge.CoreRpc.g.cpp"
#include "cppcoro/async_mutex.hpp"
#include "extension.h"
#include <CoreRpc.h>
#include <VectorBuffer.h>
#include <coroutine>
#include <nlohmann/json.hpp>
#include <ppltasks.h>
#include <tinycbor/cbor.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Networking.Sockets.h>
#include <winrt/Windows.Storage.Streams.h>

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace nlohmann;
using namespace Windows::Networking;
using namespace Sockets;
using namespace Windows::Storage::Streams;

namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    winrt::Windows::Foundation::IAsyncOperation<winrt::YtFlowAppBridge::Bridge::CoreRpc> CoreRpc::ConnectAsync()
    {
        Windows::Networking::Sockets::StreamSocket socket;
        socket.Control().NoDelay(true);
        auto const port = Windows::Storage::ApplicationData::Current()
                              .LocalSettings()
                              .Values()
                              .TryLookup(L"YTFLOW_CORE_RPC_PORT")
                              .try_as<hstring>()
                              .value_or(L"9097");
        co_await socket.ConnectAsync(HostName{L"127.0.0.1"}, port);
        WriteLine("port:" + ToStdString(port));
        co_return winrt::make<CoreRpc>(socket);
    }
    winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Foundation::Collections::IVectorView<uint8_t>> CoreRpc::
        CollectAllPluginInfoAsync(winrt::Windows::Foundation::Collections::IMapView<uint32_t, uint32_t> _hashcodes)
    {
        auto sharedMap = std::make_shared<std::map<uint32_t, uint32_t>>();
        for (const auto &pair : _hashcodes)
        {
            sharedMap->emplace(pair.Key(), pair.Value());
        }

        auto hashcodes = sharedMap;
        //------------------

        auto const writeStream{m_socket.OutputStream()};
        auto readStream{m_socket.InputStream()};
        auto const ioLock{m_ioLock};
        auto const _scope = co_await ioLock->scoped_lock_async();
        // Use tinycbor instead of nlohmann/json here because the latter does
        // not support using a non-string value as key.
        CborEncoder enc, mainMapEnc, cMapEnc, hashMapEnc;
        std::vector<uint8_t> reqData(hashcodes->size() * 10 + 16);
        cbor_encoder_init(&enc, reqData.data() + 4, reqData.size() - 4, 0);
        assert(cbor_encoder_create_map(&enc, &mainMapEnc, 1) == CborNoError);
        {

            assert(cbor_encode_text_string(&mainMapEnc, "c", 1) == CborNoError);
            assert(cbor_encoder_create_map(&mainMapEnc, &cMapEnc, 1) == CborNoError);
            {
                assert(cbor_encode_text_string(&cMapEnc, "h", 1) == CborNoError);
                assert(cbor_encoder_create_map(&cMapEnc, &hashMapEnc, hashcodes->size()) == CborNoError);
                for (auto const &[k, v] : *hashcodes)
                {
                    assert(cbor_encode_int(&hashMapEnc, k) == CborNoError);
                    assert(cbor_encode_int(&hashMapEnc, v) == CborNoError);
                }
                assert(cbor_encoder_close_container(&cMapEnc, &hashMapEnc) == CborNoError);
            }
            assert(cbor_encoder_close_container(&mainMapEnc, &cMapEnc) == CborNoError);
        }
        assert(cbor_encoder_close_container(&enc, &mainMapEnc) == CborNoError);
        auto const reqDataLen{cbor_encoder_get_buffer_size(&enc, reqData.data() + 4)};
        reqData[0] = static_cast<uint8_t>(reqDataLen >> 24);
        reqData[1] = static_cast<uint8_t>(reqDataLen >> 16);
        reqData[2] = static_cast<uint8_t>(reqDataLen >> 8);
        reqData[3] = static_cast<uint8_t>(reqDataLen);
        auto reqBuf{winrt::make<winrt::YtFlowAppBridge::implementation::VectorBuffer>(std::move(reqData))};

        get_self<winrt::YtFlowAppBridge::implementation::VectorBuffer>(reqBuf)->m_length =
            static_cast<uint32_t>(reqDataLen + 4);

        co_await writeStream.WriteAsync(std::move(reqBuf));

        co_await writeStream.FlushAsync();
        WriteLine("CollectAllPluginInfoAsync:" + std::to_string(__LINE__));
        auto const resData = co_await ReadChunk(std::move(readStream));
        WriteLine("CollectAllPluginInfoAsync:" + std::to_string(__LINE__));
        auto res{json::from_cbor(resData)};
        if (res.at("c") != "Ok")
        {
            winrt::YtFlowAppBridge::implementation::RpcException ex;
            ex.msg = res.at("e");
            throw std::move(ex);
        }
        WriteLine("CollectAllPluginInfoAsync:" + std::to_string(__LINE__));
        std::vector<winrt::YtFlowAppBridge::implementation::RpcPluginInfo> const ret = std::move(res.at("d"));
        WriteLine("CollectAllPluginInfoAsync:" + std::to_string(__LINE__));
        //-----------------
        co_return ToCBORVectorView(ret);
    }
    winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Foundation::Collections::IVectorView<uint8_t>> CoreRpc::
        SendRequestToPluginAsync(uint32_t pluginIdParam, hstring _func,
                                 winrt::Windows::Foundation::Collections::IVector<uint8_t> _params)
    {
        //(uint32_t pluginIdParam, std::string_view funcParam,  std::vector<uint8_t> paramsParam)
        // auto res = co_await _coreRpc.SendRequestToPlugin(pluginId, , );
        auto funcParam = std::string_view(winrt::to_string(_func));
        auto paramsParam = ConvertToStdVector(_params);
        auto const pluginId = pluginIdParam;
        auto const func = funcParam;
        auto params = std::move(paramsParam);

        auto const writeStream{m_socket.OutputStream()};
        auto readStream{m_socket.InputStream()};
        auto const ioLock{m_ioLock};

        auto const _scope = co_await ioLock->scoped_lock_async();

        json reqDoc{"{ \"p\": {} }"_json};
        auto &reqDocInner{reqDoc["p"]};
        reqDocInner["id"] = pluginId;
        reqDocInner["fn"] = func;
        reqDocInner["p"] = json::binary_t{std::move(params)};
        auto reqData{json::to_cbor(reqDoc)};

        auto const reqDataLen{reqData.size()};
        reqData.insert(reqData.begin(), {static_cast<uint8_t>(reqDataLen >> 24), static_cast<uint8_t>(reqDataLen >> 16),
                                         static_cast<uint8_t>(reqDataLen >> 8), static_cast<uint8_t>(reqDataLen)});
        auto reqBuf{winrt::make<winrt::YtFlowAppBridge::implementation::VectorBuffer>(std::move(reqData))};
        get_self<winrt::YtFlowAppBridge::implementation::VectorBuffer>(reqBuf)->m_length =
            static_cast<uint32_t>(reqDataLen + 4);
        co_await writeStream.WriteAsync(std::move(reqBuf));
        co_await writeStream.FlushAsync();

        auto const resData = co_await ReadChunk(std::move(readStream));
        auto res{json::from_cbor(resData)};
        if (res.at("c") != "Ok")
        {
            winrt::YtFlowAppBridge::implementation::RpcException ex;
            ex.msg = res.at("e");
            throw std::move(ex);
        }

        // co_return std::vector(std::move(res.at("d")).get_binary());

        co_return ToVectorView(std::move(res.at("d")).get_binary());
    }

    winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Foundation::Collections::IVectorView<uint8_t>> CoreRpc::
        ReadChunk(IInputStream stream)
    {

        DataReader const reader{std::move(stream)};
        reader.ByteOrder(ByteOrder::BigEndian);
        uint32_t readLen{4};
        WriteLine("ReadChunk:" + std::to_string(__LINE__) + " readLen:" + std::to_string(readLen));
        while (readLen > 0)
        {
            WriteLine("ReadChunk:" + std::to_string(__LINE__));
            auto const len = co_await reader.LoadAsync(readLen);
            WriteLine("ReadChunk:" + std::to_string(__LINE__));
            if (len == 0)
            {
                WriteLine("ReadChunk:" + std::to_string(__LINE__) + " len == 0");
                winrt::YtFlowAppBridge::implementation::RpcException ex;
                ex.msg = "RPC EOF";
                throw std::move(ex);
            }
            readLen -= len;
        }
        WriteLine("ReadChunk:" + std::to_string(__LINE__) + " readLen:" + std::to_string(readLen));
        uint32_t const chunkSize{reader.ReadUInt32()};
        WriteLine("ReadChunk:" + std::to_string(__LINE__) + " chunkSize:" + std::to_string(chunkSize));
        while (readLen < chunkSize)
        {
            auto const len = co_await reader.LoadAsync(chunkSize - readLen);
            if (len == 0)
            {
                winrt::YtFlowAppBridge::implementation::RpcException ex;
                ex.msg = "RPC EOF";
                throw std::move(ex);
            }
            readLen += len;
        }
        std::vector<uint8_t> ret(chunkSize);
        reader.ReadBytes(ret);
        reader.DetachStream();
        reader.Close();
        co_return ToVectorView(ret);
    }
}
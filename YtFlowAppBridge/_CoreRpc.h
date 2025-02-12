#pragma once
#include <pch.h>
#include "Bridge.CoreRpc.g.h"
#include "FfiConn.h"

#include "cppcoro/async_mutex.hpp"
#include <coroutine>
#include <extension.h>
#include <nlohmann/json.hpp>
#include <ppltasks.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Networking.Sockets.h>
#include <winrt/Windows.Storage.Streams.h>
namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    struct CoreRpc : CoreRpcT<CoreRpc>
    {

        CoreRpc() : disposed(false)
        {
        }
        CoreRpc(Windows::Networking::Sockets::StreamSocket &m_socket) : m_socket(std::move(m_socket)), disposed(false)
        {
        }
        ~CoreRpc()
        {

            if (!disposed)
            {
                if (m_socket != nullptr)
                {
                    m_socket.Close();
                    m_socket = nullptr;
                }
            }
        }
        // IDisposable µÄÊµÏÖ
        void Dispose()
        {
            if (!disposed)
            {
                if (m_socket != nullptr)
                {
                    m_socket.Close();
                }

                disposed = true;
            }
            else
            {
            }
        }
        void Close()
        {
            Dispose();
        }

        static winrt::Windows::Foundation::IAsyncOperation<winrt::YtFlowAppBridge::Bridge::CoreRpc> ConnectAsync();

        winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Foundation::Collections::IVectorView<uint8_t>>
        CollectAllPluginInfoAsync(winrt::Windows::Foundation::Collections::IMapView<uint32_t, uint32_t> hashcodes);

        winrt::Windows::Foundation::IAsyncOperation<winrt::Windows::Foundation::Collections::IVectorView<uint8_t>>
        SendRequestToPluginAsync(uint32_t pluginId, hstring func,
                                 winrt::Windows::Foundation::Collections::IVector<uint8_t> params);

      private:
        Windows::Networking::Sockets::StreamSocket m_socket{nullptr};
        bool disposed;

        static winrt::Windows::Foundation::IAsyncOperation<
            winrt::Windows::Foundation::Collections::IVectorView<uint8_t>>
        ReadChunk(Windows::Storage::Streams::IInputStream stream);
        std::shared_ptr<cppcoro::async_mutex> m_ioLock{std::make_shared<cppcoro::async_mutex>()};
    };
}

namespace winrt::YtFlowAppBridge::Bridge::factory_implementation
{
    struct CoreRpc : CoreRpcT<CoreRpc, implementation::CoreRpc>
    {
    };
}

#pragma once
#include "pch.h"
#include "cppcoro/async_mutex.hpp"
#include <coroutine>
#include <nlohmann/json.hpp>
#include <ppltasks.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Networking.Sockets.h>
#include <winrt/Windows.Storage.Streams.h>

using namespace winrt::Windows::Foundation;

namespace winrt::YtFlowAppBridge::implementation
{
    struct RpcException : public std::exception
    {
        std::string msg;

        const char *what() const throw() override;
    };
    struct RpcPluginInfo
    {
        // https://github.com/ReactiveX/RxCpp/issues/420
        bool operator==(const RpcPluginInfo &) const;

        uint32_t id{};
        std::string name;
        std::string plugin;
        nlohmann::json::binary_t info;
        uint32_t hashcode{};
    };
    NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(RpcPluginInfo, id, name, plugin, info, hashcode)

}

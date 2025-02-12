#pragma once

#include "Bridge.YtflowCore.g.h"
#include "CoreFfi.h"

namespace winrt::YtFlowAppBridge::Bridge::implementation
{

    struct YtflowCore : YtflowCoreT<YtflowCore>
    {

        YtflowCore() = delete;
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t> Verify(
            winrt::hstring plugin, std::uint16_t plugin_version,
            winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param);
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ytflow_app_proxy_data_proxy_compose_v1(
            winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param);
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ytflow_app_proxy_data_proxy_analyze(
            hstring name, winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy_data, uint16_t version);
        static winrt::hstring ConvertDataProxyToShareLink(
            winrt::hstring name, winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy_data,
            uint16_t version);
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ytflow_app_share_link_decode(
            winrt::hstring name);
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t>
        DecodeSubscriptionUserInfoFromResponseHeaderValue(winrt::hstring str);
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ytflow_app_subscription_decode(
            winrt::Windows::Foundation::Collections::IVectorView<uint8_t> data, hstring &decodedFormat);
        static winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ytflow_app_subscription_decode_with_format(
            winrt::Windows::Foundation::Collections::IVectorView<uint8_t> data, hstring decodedFormat);
    };
}

namespace winrt::YtFlowAppBridge::Bridge::factory_implementation
{
    struct YtflowCore : YtflowCoreT<YtflowCore, implementation::YtflowCore>
    {
    };
}

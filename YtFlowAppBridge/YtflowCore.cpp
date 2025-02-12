#include "pch.h"
#include "YtflowCore.h"
#include "Bridge.YtflowCore.g.cpp"
#include <CoreProxy.h>
#include <extension.h>

namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> YtflowCore::Verify(
        winrt::hstring plugin, uint16_t plugin_version,
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param)
    {

        try
        {
            auto res = winrt::YtFlowAppBridge::implementation::FfiPlugin::verify(
                ToStdString(plugin).c_str(), plugin_version, ConvertToVector(param).data(), param.Size());

            return ToCBORVectorView(res);
        }
        catch (winrt::YtFlowAppBridge::implementation::FfiException ex)
        {

            return ToCBORVectorView(ex.msg);
        }
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> implementation::YtflowCore::
        ytflow_app_proxy_data_proxy_compose_v1(winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy)
    {
        auto res = winrt::YtFlowAppBridge::implementation::unwrap_ffi_byte_buffer(
            ytflow_core::ytflow_app_proxy_data_proxy_compose_v1(ConvertToVector(proxy).data(), proxy.Size()));
        return ToCBORVectorView(res);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> implementation::YtflowCore::
        ytflow_app_proxy_data_proxy_analyze(hstring name,
                                            winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy_data,
                                            uint16_t version)
    {
        auto res = winrt::YtFlowAppBridge::implementation::unwrap_ffi_byte_buffer(
            ytflow_core::ytflow_app_proxy_data_proxy_analyze(
                ToStdString(name).c_str(), ConvertToVector(proxy_data).data(), proxy_data.Size(), version));
        return ToVectorView(res);
    }
    winrt::hstring YtflowCore::ConvertDataProxyToShareLink(
        winrt::hstring name, winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy_data, uint16_t version)
    {
        auto res = winrt::YtFlowAppBridge::implementation::ConvertDataProxyToShareLink(
            ToStdString(name), ConvertToVector(proxy_data), version);
        if (res.has_value())

            return winrt::to_hstring(*res);
        return L"";
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> YtflowCore::ytflow_app_share_link_decode(
        winrt::hstring name)
    {
        auto res = ytflow_core::ytflow_app_share_link_decode(ToStdString(name).c_str());

        auto const proxy_buffer = winrt::YtFlowAppBridge::implementation::unwrap_ffi_byte_buffer(res);
        return ToVectorView(proxy_buffer);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> YtflowCore::
        DecodeSubscriptionUserInfoFromResponseHeaderValue(winrt::hstring str)
    {
        return ToVectorView(winrt::YtFlowAppBridge::implementation::unwrap_ffi_byte_buffer(
            ytflow_core::ytflow_app_subscription_userinfo_header_decode(ToStdString(str).c_str())));
    }

    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> YtflowCore::ytflow_app_subscription_decode(
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> data, hstring &decodedFormat)
    {

        char *p = nullptr;
        auto res =
            ytflow_core::ytflow_app_subscription_decode(ConvertToVector(data).data(), data.Size(), (const char **)&p);
        if (p != nullptr)
            decodedFormat = winrt::to_hstring(p);
        return ToVectorView(winrt::YtFlowAppBridge::implementation::unwrap_ffi_byte_buffer(res));
    }

    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> YtflowCore::
        ytflow_app_subscription_decode_with_format(winrt::Windows::Foundation::Collections::IVectorView<uint8_t> data,
                                                   hstring decodedFormat)
    {
        auto res = ytflow_core::ytflow_app_subscription_decode_with_format(ConvertToVector(data).data(), data.Size(),
                                                                           ToStdString(decodedFormat).c_str());

        return ToVectorView(winrt::YtFlowAppBridge::implementation::unwrap_ffi_byte_buffer(res));
    }

}
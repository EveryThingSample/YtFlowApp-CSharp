#pragma once
#include "Bridge.FfiConn.g.h"
#include "CoreFfi.h"

namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    struct FfiConn : FfiConnT<FfiConn>
    {
      public:
        FfiConn() : _FfiConn(nullptr)
        {
        }
        FfiConn(winrt::YtFlowAppBridge::implementation::FfiConn &_FfiConn) : _FfiConn(std::move(_FfiConn))
        {
        }
        // ConvertToCborVectorView
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetProfiles();
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetPluginsByProfile(uint32_t profileId);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetEntryPluginsByProfile(uint32_t profileId);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetProxyGroupById(uint32_t id);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetResources();
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetProxiesByProxyGroup(uint32_t proxyGroupId);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> GetProxyGroups();

        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /*FfiResourceGitHubRelease*/
        GetResourceGitHubReleaseByResourceId(uint32_t resourceId);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /* FfiProxyGroupSubscription*/
        GetProxySubscriptionByProxyGroup(uint32_t proxyGroupId);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /*FfiResourceUrl*/
        GetResourceUrlByResourceId(uint32_t resourceId);
        winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /*FfiParsedTomlProfile*/
        ParseProfileToml(winrt::Windows::Foundation::Collections::IVectorView<uint8_t> toml);

        void DeleteProfile(uint32_t id);
        uint32_t CreateProfile(winrt::param::hstring const &name, winrt::param::hstring const &locale);
        void UpdateProfile(uint32_t id, winrt::param::hstring const &name, winrt::param::hstring const &locale);
        winrt::hstring ExportProfileToml(uint32_t id);

        void SetPluginAsEntry(uint32_t pluginId, uint32_t profileId);
        void UnsetPluginAsEntry(uint32_t pluginId, uint32_t profileId);
        void DeletePlugin(uint32_t id);
        uint32_t CreatePlugin(uint32_t profileId, winrt::param::hstring const &name, winrt::param::hstring const &desc,
                              winrt::param::hstring const &plugin, uint16_t pluginVersion,
                              winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param);
        void UpdatePlugin(uint32_t id, uint32_t profileId, winrt::param::hstring const &name,
                          winrt::param::hstring const &desc, winrt::param::hstring const &plugin,
                          uint16_t pluginVersion, winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param);

        void RenameProxyGroup(uint32_t id, winrt::param::hstring const &name);
        void DeleteProxyGroup(uint32_t id);
        uint32_t CreateProxyGroup(winrt::param::hstring const &name, winrt::param::hstring const &type);
        uint32_t CreateProxySubscriptionGroup(winrt::param::hstring const &name, winrt::param::hstring const &format,
                                              winrt::param::hstring const &url);

        void UpdateProxySubscriptionRetrievedByProxyGroup(
            uint32_t proxyGroupId, const winrt::Windows::Foundation::IReference<uint64_t> uploadBytes,
            const winrt::Windows::Foundation::IReference<uint64_t> downloadBytes,
            const winrt::Windows::Foundation::IReference<uint64_t> totalBytes, winrt::param::hstring const &expiresAt);
        uint32_t CreateProxy(uint32_t proxyGroupId, winrt::param::hstring const &name,
                             winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy,
                             uint16_t proxyVersion);
        void UpdateProxy(uint32_t proxyId, winrt::param::hstring const &name,
                         winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy, uint16_t proxyVersion);
        void DeleteProxy(uint32_t proxyId);
        void ReorderProxy(uint32_t proxyGroupId, int32_t rangeStartOrder, int32_t rangeEndOrder, int32_t moves);
        void BatchUpdateProxyInGroup(uint32_t proxyGroupId,
                                     winrt::Windows::Foundation::Collections::IVectorView<uint8_t> newProxyBuf);

        void DeleteResource(uint32_t resourceId);
        uint32_t CreateResourceWithUrl(winrt::param::hstring const &key, winrt::param::hstring const &type,
                                       winrt::param::hstring const &local_file, winrt::param::hstring const &url);
        uint32_t CreateResourceWithGitHubRelease(winrt::param::hstring const &key, winrt::param::hstring const &type,
                                                 winrt::param::hstring const &local_file,
                                                 winrt::param::hstring const &github_username,
                                                 winrt::param::hstring const &github_repo,
                                                 winrt::param::hstring const &asset_name);

        void UpdateResourceUrlRetrievedByResourceId(uint32_t resourceId, winrt::param::hstring const &etag,
                                                    winrt::param::hstring const &lastModified);

        void UpdateResourceGitHubReleaseRetrievedByResourceId(uint32_t resourceId, winrt::param::hstring const &gitTag,
                                                              winrt::param::hstring const &releaseTitle);

      private:
        // FfiDb(winrt::YtFlowAppBridge::implementation::FfiDb *_ffiDb);

        // Static member variable to store the singleton instance of FfiDb
        winrt::YtFlowAppBridge::implementation::FfiConn _FfiConn;
    };
}

namespace winrt::YtFlowAppBridge::Bridge::factory_implementation
{
    struct FfiConn : FfiConnT<FfiConn, implementation::FfiConn>
    {
    };
}

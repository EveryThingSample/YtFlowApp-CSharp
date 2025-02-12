#include "pch.h"
#include "FfiConn.h"
#include "Bridge.FfiConn.g.cpp"
#include "extension.h"
using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;

namespace winrt::YtFlowAppBridge::Bridge::implementation
{

    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetEntryPluginsByProfile(uint32_t profileId)
    {
        auto data = _FfiConn.GetEntryPluginsByProfile(profileId);
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetProxyGroups()
    {
        auto data = _FfiConn.GetProxyGroups();
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /*FfiResourceGitHubRelease*/
    FfiConn::GetResourceGitHubReleaseByResourceId(uint32_t resourceId)
    {
        auto data = _FfiConn.GetResourceGitHubReleaseByResourceId(resourceId);
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /* FfiProxyGroupSubscription*/
    FfiConn::GetProxySubscriptionByProxyGroup(uint32_t proxyGroupId)
    {
        auto data = _FfiConn.GetProxySubscriptionByProxyGroup(proxyGroupId);
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> /*FfiResourceUrl*/
    FfiConn::GetResourceUrlByResourceId(uint32_t resourceId)
    {

        auto data = _FfiConn.GetResourceUrlByResourceId(resourceId);
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> winrt::YtFlowAppBridge::Bridge::implementation::
        FfiConn::ParseProfileToml(winrt::Windows::Foundation::Collections::IVectorView<uint8_t> toml)
    {
        auto vec = ConvertToVector(toml);
        auto data = _FfiConn.ParseProfileToml(vec.data(), vec.size());
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetProxyGroupById(uint32_t id)
    {
        auto data = _FfiConn.GetProxyGroupById(id);
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetProfiles()
    {
        auto profiles_ = _FfiConn.GetProfiles();
        return ToCBORVectorView(profiles_);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetProxiesByProxyGroup(uint32_t proxyGroupId)
    {
        auto data = _FfiConn.GetProxiesByProxyGroup(proxyGroupId);
        return ToCBORVectorView(data);
    }
    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetResources()
    {
        auto data = _FfiConn.GetResources();
        return ToCBORVectorView(data);
    }

    void FfiConn::DeleteProfile(uint32_t id)
    {
        _FfiConn.DeleteProfile(id);
    }

    uint32_t FfiConn::CreateProfile(winrt::param::hstring const &name, winrt::param::hstring const &locale)
    {
        return _FfiConn.CreateProfile(ToStdString(name).c_str(), ToStdString(locale).c_str());
    }

    void FfiConn::UpdateProfile(uint32_t id, winrt::param::hstring const &name, winrt::param::hstring const &locale)
    {
        _FfiConn.UpdateProfile(id, ToStdString(name).c_str(), ToStdString(locale).c_str());
    }

    winrt::hstring FfiConn::ExportProfileToml(uint32_t id)
    {
        return winrt::to_hstring(_FfiConn.ExportProfileToml(id));
    }

    winrt::Windows::Foundation::Collections::IVectorView<uint8_t> FfiConn::GetPluginsByProfile(uint32_t profileId)
    {
        auto data = _FfiConn.GetPluginsByProfile(profileId);
        return ToCBORVectorView(data);
    }

    void FfiConn::SetPluginAsEntry(uint32_t pluginId, uint32_t profileId)
    {
        _FfiConn.SetPluginAsEntry(pluginId, profileId);
    }

    void FfiConn::UnsetPluginAsEntry(uint32_t pluginId, uint32_t profileId)
    {
        _FfiConn.UnsetPluginAsEntry(pluginId, profileId);
    }

    void FfiConn::DeletePlugin(uint32_t id)
    {
        _FfiConn.DeletePlugin(id);
    }

    uint32_t FfiConn::CreatePlugin(uint32_t profileId, winrt::param::hstring const &name,
                                   winrt::param::hstring const &desc, winrt::param::hstring const &plugin,
                                   uint16_t pluginVersion,
                                   winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param)
    {

        auto data = ConvertToVector(param);
        return _FfiConn.CreatePlugin(profileId, ToStdString(name).c_str(), ToStdString(desc).c_str(),
                                     ToStdString(plugin).c_str(), pluginVersion, data.data(), data.size());
    }

    void FfiConn::UpdatePlugin(uint32_t id, uint32_t profileId, winrt::param::hstring const &name,
                               winrt::param::hstring const &desc, winrt::param::hstring const &plugin,
                               uint16_t pluginVersion,
                               winrt::Windows::Foundation::Collections::IVectorView<uint8_t> param)
    {
        auto data = ConvertToVector(param);
        _FfiConn.UpdatePlugin(id, profileId, ToStdString(name).c_str(), ToStdString(desc).c_str(),
                              ToStdString(plugin).c_str(), pluginVersion, data.data(), data.size());
    }

    void FfiConn::RenameProxyGroup(uint32_t id, winrt::param::hstring const &name)
    {
        _FfiConn.RenameProxyGroup(id, ToStdString(name).c_str());
    }

    void FfiConn::DeleteProxyGroup(uint32_t id)
    {
        _FfiConn.DeleteProxyGroup(id);
    }

    uint32_t FfiConn::CreateProxyGroup(winrt::param::hstring const &name, winrt::param::hstring const &type)
    {
        return _FfiConn.CreateProxyGroup(ToStdString(name).c_str(), ToStdString(type).c_str());
    }

    uint32_t FfiConn::CreateProxySubscriptionGroup(winrt::param::hstring const &name,
                                                   winrt::param::hstring const &format,
                                                   winrt::param::hstring const &url)
    {
        return _FfiConn.CreateProxySubscriptionGroup(ToStdString(name).c_str(), ToStdString(format).c_str(),
                                                     ToStdString(url).c_str());
    }

    void FfiConn::UpdateProxySubscriptionRetrievedByProxyGroup(
        uint32_t proxyGroupId, const winrt::Windows::Foundation::IReference<uint64_t> uploadBytes,
        const winrt::Windows::Foundation::IReference<uint64_t> downloadBytes,
        const winrt::Windows::Foundation::IReference<uint64_t> totalBytes, winrt::param::hstring const &expiresAt)
    {
        _FfiConn.UpdateProxySubscriptionRetrievedByProxyGroup(
            proxyGroupId, ConvertToOptional(uploadBytes), ConvertToOptional(downloadBytes),
            ConvertToOptional(totalBytes), ToStdString(expiresAt).c_str());
    }

    uint32_t FfiConn::CreateProxy(uint32_t proxyGroupId, winrt::param::hstring const &name,
                                  winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy,
                                  uint16_t proxyVersion)
    {
        auto data = ConvertToVector(proxy);
        return _FfiConn.CreateProxy(proxyGroupId, ToStdString(name).c_str(), data.data(), data.size(), proxyVersion);
    }

    void FfiConn::UpdateProxy(uint32_t proxyId, winrt::param::hstring const &name,
                              winrt::Windows::Foundation::Collections::IVectorView<uint8_t> proxy,
                              uint16_t proxyVersion)
    {
        auto data = ConvertToVector(proxy);
        _FfiConn.UpdateProxy(proxyId, ToStdString(name).c_str(), data.data(), data.size(), proxyVersion);
    }

    void FfiConn::DeleteProxy(uint32_t proxyId)
    {
        _FfiConn.DeleteProxy(proxyId);
    }

    void FfiConn::ReorderProxy(uint32_t proxyGroupId, int32_t rangeStartOrder, int32_t rangeEndOrder, int32_t moves)
    {
        _FfiConn.ReorderProxy(proxyGroupId, rangeStartOrder, rangeEndOrder, moves);
    }

    void FfiConn::BatchUpdateProxyInGroup(uint32_t proxyGroupId,
                                          winrt::Windows::Foundation::Collections::IVectorView<uint8_t> newProxyBuf)
    {
        auto data = ConvertToVector(newProxyBuf);
        _FfiConn.BatchUpdateProxyInGroup(proxyGroupId, data.data(), data.size());
    }

    void FfiConn::DeleteResource(uint32_t resourceId)
    {
        _FfiConn.DeleteResource(resourceId);
    }

    uint32_t FfiConn::CreateResourceWithUrl(winrt::param::hstring const &key, winrt::param::hstring const &type,
                                            winrt::param::hstring const &localFile, winrt::param::hstring const &url)
    {
        return _FfiConn.CreateResourceWithUrl(ToStdString(key).c_str(), ToStdString(type).c_str(),
                                              ToStdString(localFile).c_str(), ToStdString(url).c_str());
    }

    uint32_t FfiConn::CreateResourceWithGitHubRelease(winrt::param::hstring const &key,
                                                      winrt::param::hstring const &type,
                                                      winrt::param::hstring const &localFile,
                                                      winrt::param::hstring const &githubUsername,
                                                      winrt::param::hstring const &githubRepo,
                                                      winrt::param::hstring const &assetName)
    {
        return _FfiConn.CreateResourceWithGitHubRelease(
            ToStdString(key).c_str(), ToStdString(type).c_str(), ToStdString(localFile).c_str(),
            ToStdString(githubUsername).c_str(), ToStdString(githubRepo).c_str(), ToStdString(assetName).c_str());
    }

    void FfiConn::UpdateResourceUrlRetrievedByResourceId(uint32_t resourceId, winrt::param::hstring const &etag,
                                                         winrt::param::hstring const &lastModified)
    {
        auto _lastModified = ToStdString(lastModified);
        _FfiConn.UpdateResourceUrlRetrievedByResourceId(resourceId, ToStdString(etag).c_str(),
                                                        _lastModified.empty() ? nullptr : _lastModified.c_str());
    }

    void FfiConn::UpdateResourceGitHubReleaseRetrievedByResourceId(uint32_t resourceId,
                                                                   winrt::param::hstring const &gitTag,
                                                                   winrt::param::hstring const &releaseTitle)
    {
        _FfiConn.UpdateResourceGitHubReleaseRetrievedByResourceId(resourceId, ToStdString(gitTag).c_str(),
                                                                  ToStdString(releaseTitle).c_str());
    }

}
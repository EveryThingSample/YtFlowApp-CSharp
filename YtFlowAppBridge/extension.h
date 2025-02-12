#pragma once
#include "pch.h"
#include <CoreFfi.h>
#include <codecvt>
#include <fstream>
#include <nlohmann/json.hpp>
#include <sstream>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Storage.Pickers.h>
#include <winrt/Windows.Storage.h>
namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    inline std::optional<uint64_t> ConvertToOptional(winrt::Windows::Foundation::IReference<uint64_t> reference)
    {
        if (reference == nullptr)
        {
            return std::nullopt;
        }
        else
        {
            return std::optional<uint64_t>{reference.Value()};
        }
    }
    template <typename T>
    inline std::vector<T> ConvertToVector(winrt::Windows::Foundation::Collections::IVectorView<T> param)
    {
        std::vector<uint8_t> data(param.Size());
        for (uint32_t i = 0; i < param.Size(); ++i)
        {
            data[i] = param.GetAt(i);
        }
        return data;
    }

    inline const std::string ToStdString(const winrt::hstring &hstr)
    {

        std::string nameStr = winrt::to_string(hstr);
        return nameStr;
    }
    inline std::vector<uint8_t> ConvertToStdVector(
        const winrt::Windows::Foundation::Collections::IVector<uint8_t> &winrtVector)
    {
        std::vector<uint8_t> result;
        result.reserve(winrtVector.Size());

        for (const auto &value : winrtVector)
        {
            result.push_back(value);
        }

        return result;
    }
    inline std::string WStringToString(const std::wstring_view &wstr)
    {
        if (wstr.empty())
        {
            return {};
        }

        int size_needed = WideCharToMultiByte(CP_UTF8, 0, wstr.data(), -1, nullptr, 0, nullptr, nullptr);
        std::string result(size_needed - 1, '\0'); // -1 是因为 WideCharToMultiByte 包括了 null 终止符
        WideCharToMultiByte(CP_UTF8, 0, wstr.data(), -1, &result[0], size_needed, nullptr, nullptr);
        return result;
    }
    inline std::string WStringToString(const std::wstring &wstr)
    {
        if (wstr.empty())
        {
            return {};
        }

        int size_needed = WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), -1, nullptr, 0, nullptr, nullptr);
        std::string result(size_needed - 1, '\0'); // -1 是因为 WideCharToMultiByte 包括了 null 终止符
        WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), -1, &result[0], size_needed, nullptr, nullptr);
        return result;
    }

    inline std::wstring StringToWString(const std::string &str)
    {
        if (str.empty())
        {
            return {};
        }

        int size_needed = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
        std::wstring result(size_needed - 1, '\0'); // -1 是因为 MultiByteToWideChar 包括了 null 终止符
        MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, &result[0], size_needed);
        return result;
    }
    template <typename T>
    inline winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ToCBORVectorView(const T &info)
    {
        auto vector = winrt::single_threaded_vector<uint8_t>();
        for (const auto &value : nlohmann::json::to_cbor(info))
        {
            vector.Append(value);
        }
        return vector.GetView();
    }

    inline winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ToVectorView(const std::vector<uint8_t> &info)
    {
        auto vector = winrt::single_threaded_vector<uint8_t>();
        for (const auto &value : info)
        {
            vector.Append(value);
        }
        return vector.GetView();
    }

    inline winrt::Windows::Foundation::Collections::IVectorView<uint8_t> ToCBORVectorView(
        const std::vector<uint8_t> &info)
    {
        return ToVectorView(info);
    }
    template <typename T>
    inline T FromCBORVectorView(winrt::Windows::Foundation::Collections::IVectorView<uint8_t> vectorView)
    {
        std::vector<uint8_t> stdVector;
        stdVector.reserve(vectorView.Size());
        for (uint32_t i = 0; i < vectorView.Size(); ++i)
        {
            stdVector.push_back(vectorView.GetAt(i));
        }
        auto ptr = (const uint8_t *)stdVector.data();
        auto meta = (size_t)stdVector.size();
        T json = nlohmann::json::from_cbor(ptr, ptr + meta);
        return json;
    }

    inline std::string GetCurrentTime()
    {
        auto now = std::chrono::system_clock::now();
        auto in_time_t = std::chrono::system_clock::to_time_t(now);
        std::tm local_time;
        localtime_s(&local_time, &in_time_t);

        std::wstringstream ss;
        ss << std::put_time(&local_time, L"%Y-%m-%d %X ");
        return WStringToString(ss.str());
    }
    inline void WriteLine(const std::string &message)
    {
        // 创建一个日志通道

        static winrt::hstring path =
            winrt::Windows::Storage::ApplicationData::Current().TemporaryFolder().Path() + L"\\log.txt";
        static std::ofstream logFile(path.c_str(), std::ios::out | std::ios::app);

        logFile << GetCurrentTime();
        logFile << message << std::endl;
        // 使用示例 GUID // 记录消息
        // loggingChannel.LogMessage(message, winrt::Windows::Foundation::Diagnostics::LoggingLevel::Verbose);
    }
}

namespace winrt::YtFlowAppBridge::implementation
{

    //----

    inline void to_json(nlohmann::json &json, const FfiPluginVerifyResult &r)
    {
        json["required"] = r.required;
        json["provides"] = r.provides;
    }
    inline void to_json(nlohmann::json &json, const FfiResourceUrl &r)
    {
        json["id"] = r.id;
        json["url"] = r.url;
        if (r.etag)
            json["etag"] = r.etag.value();
        if (r.last_modified)
            json["last_modified"] = r.last_modified.value();
        if (r.retrieved_at)
            json["retrieved_at"] = r.retrieved_at.value();
    }
    inline void to_json(nlohmann::json &json, const FfiProxyGroupSubscription &r)
    {
        json["format"] = r.format;
        json["url"] = r.url;
        if (r.upload_bytes_used)
            json["upload_bytes_used"] = r.upload_bytes_used.value();
        if (r.download_bytes_used)
            json["download_bytes_used"] = r.download_bytes_used.value();
        if (r.bytes_total)
            json["bytes_total"] = r.bytes_total.value();
        if (r.expires_at)
            json["expires_at"] = r.expires_at.value();
        if (r.retrieved_at)
            json["retrieved_at"] = r.retrieved_at.value();
    }

    inline void to_json(nlohmann::json &json, const FfiResourceGitHubRelease &r)
    {
        json["id"] = r.id;
        json["github_username"] = r.github_username;
        json["github_repo"] = r.github_repo;
        json["asset_name"] = r.asset_name;
        if (r.git_tag)
            json["git_tag"] = r.git_tag.value();
        if (r.release_title)
            json["release_title"] = r.release_title.value();
        if (r.retrieved_at)
            json["retrieved_at"] = r.retrieved_at.value();
    }
    inline void to_json(nlohmann::json &json, const FfiParsedTomlPlugin &r)
    {
        json["plugin"] = r.plugin;
        json["is_entry"] = r.is_entry;
    }
    inline void to_json(nlohmann::json &json, const FfiParsedTomlProfile &r)
    {
        if (r.name)
            json["name"] = r.name.value();
        if (r.locale)
            json["locale"] = r.locale.value();
        if (r.created_at)
            json["created_at"] = r.created_at.value();
        json["plugins"] = r.plugins;
    }

}
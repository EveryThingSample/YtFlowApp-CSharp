using Newtonsoft.Json;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using YtFlowApp2.Classes;
using YtFlowAppBridge.Bridge;

namespace YtFlowApp2.CoreInterop
{
    public static class BridgeExtensions
    {
        public const UInt32 INVALID_DB_ID = 0xFFFFFFFF;
        public static FfiDb FfiDbInstance => FfiDb.Current;
        public static IList<FfiProfile> GetProfiles(this FfiConn ffiConn)
        {
            return FromCBORBytes<IList<FfiProfile>>(ffiConn.GetProfiles().ToArray());
        }
        public static IList<FfiDataProxy> GetProxiesByProxyGroup(this FfiConn ffiConn, uint proxyGroupId)
        {
            return FromCBORBytes<IList<FfiDataProxy>>(ffiConn.GetProxiesByProxyGroup(proxyGroupId).ToArray());
        }
        public static IList<FfiPlugin> GetPluginsByProfile(this FfiConn ffiConn, uint profileId)
        {
            return FromCBORBytes<IList<FfiPlugin>>(ffiConn.GetPluginsByProfile(profileId).ToArray());
        }
        public static IList<FfiPlugin> GetEntryPluginsByProfile(this FfiConn ffiConn, uint profileId)
        {
            return FromCBORBytes<IList<FfiPlugin>>(ffiConn.GetEntryPluginsByProfile(profileId).ToArray());
        }

        public static FfiParsedTomlProfile ParseProfileToml(this FfiConn ffiConn, Windows.Storage.Streams.IBuffer toml)
        {
            return FromCBORBytes<FfiParsedTomlProfile>(ffiConn.ParseProfileToml(toml.ToArray()).ToArray());
        }
        public static T FromCBORBytes<T>(byte[] data)
        {
            return FromCBORBytes(data).ToObject<T>();
        }
        public static List<FfiResource> GetResources(this FfiConn ffiConn)
        {
            return FromCBORBytes<List<FfiResource>>(ffiConn.GetResources().ToArray());
        }

        public static FfiResourceUrl GetResourceUrlByResourceId(this FfiConn ffiConn, uint resourceId)
        {
            var res = ffiConn.GetResourceUrlByResourceId(resourceId);
            return FromCBORBytes<FfiResourceUrl>(res.ToArray());
        }
        public static FfiResourceGitHubRelease GetResourceGitHubReleaseByResourceId(this FfiConn ffiConn, uint resourceId)
        {
            return FromCBORBytes<FfiResourceGitHubRelease>(ffiConn.GetResourceGitHubReleaseByResourceId(resourceId).ToArray());
        }
        public static CBORObject FromCBORBytes(byte[] data)
        {
            var res = CBORObject.DecodeFromBytes(data);
            if (res.Type == CBORType.TextString)
            {
                throw new FfiException(res.ToString());
            }
            return res;
        }
        public static string FromCBORBytesToJson(byte[] data)
        {
            return CBORObject.DecodeFromBytes(data).ToJSONString();
        }
        public static byte[] ToCBORBytes(object obj)
        {
            if (obj == null)
                return CBORObject.NewMap().EncodeToBytes();
            return CBORObject.FromObject(obj).EncodeToBytes();
        }
   /*     public static async Task<IReadOnlyList<RpcPluginInfo>> CollectAllPluginInfoAsync(this CoreRpc coreRpc, IDictionary<uint, uint> keys)
        {
            return FromCBORBytes<IReadOnlyList<RpcPluginInfo>>((await coreRpc.CollectAllPluginInfoAsync(new ReadOnlyDictionary<uint, uint>(keys))).ToArray());
        }
*/
        internal static FfiPluginVerifyResult ytflow_plugin_verify(string plugin, ushort pluginVersion, byte[] param)
        {
            return FromCBORBytes<FfiPluginVerifyResult>(YtflowCore.Verify(plugin, pluginVersion, param).ToArray());
        }

        internal static byte[] ytflow_app_proxy_data_proxy_compose_v1(byte[] proxy)
        {
            return YtflowCore.ytflow_app_proxy_data_proxy_compose_v1(proxy).ToArray();
        }
        internal static FfiProxy ytflow_app_proxy_data_proxy_analyze(string name, byte[] proxy, ushort proxy_versio)
        {
            var res = YtflowCore.ytflow_app_proxy_data_proxy_analyze(name, proxy, proxy_versio).ToArray();
            var ret = FromCBORBytes(res);
            return FfiProxy.FromCbor(ret);
           
        }
        internal static IList<FfiProxyGroup> GetProxyGroups(this FfiConn ffiConn)
        {
            return FromCBORBytes<IList<FfiProxyGroup>>(ffiConn.GetProxyGroups().ToArray());
        }//GetProxySubscriptionByProxyGroup
        internal static FfiProxyGroupSubscription GetProxySubscriptionByProxyGroup(this FfiConn ffiConn, uint proxyGroupId)
        {
            return FromCBORBytes<FfiProxyGroupSubscription>(ffiConn.GetProxySubscriptionByProxyGroup(proxyGroupId).ToArray());
        }//
        internal static Pages.LibraryPage.DecodedSubscriptionUserInfo DecodeSubscriptionUserInfoFromResponseHeaderValue(string str)
        {
            return FromCBORBytes<Pages.LibraryPage.DecodedSubscriptionUserInfo>(YtFlowAppBridge.Bridge.YtflowCore.DecodeSubscriptionUserInfoFromResponseHeaderValue(str).ToArray());
        }
        internal static byte[] ytflow_app_subscription_decode(byte[] data, out string format)
        {
            return YtFlowAppBridge.Bridge.YtflowCore.ytflow_app_subscription_decode(data, out format).ToArray();
        }
        internal static byte[] ytflow_app_subscription_decode_with_format(byte[] data, string format)
        {
            return YtFlowAppBridge.Bridge.YtflowCore.ytflow_app_subscription_decode_with_format(data, format).ToArray();
        }
        internal static byte[] DecodeSubscriptionProxies(string data, ref string decodedFormat)
        {
            try
            {
                FfiSubscription decoded;
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                if (decodedFormat == null)
                {
                    decoded = FromCBORBytes<FfiSubscription>(ytflow_app_subscription_decode(dataBytes, out decodedFormat));
                }
                else
                {
                    decoded = FromCBORBytes<FfiSubscription>(ytflow_app_subscription_decode_with_format(dataBytes, decodedFormat));
                }

                if (decoded.Proxies.Count == 0)
                {
                    decodedFormat = null;
                    return null;
                }

                var dataProxies = decoded.Proxies.Select(proxy =>
                {
                    var name = proxy["name"].AsString();
                    var proxyBuf = ToCBORBytes(proxy);
                    var dataProxyBuf = ytflow_app_proxy_data_proxy_compose_v1(proxyBuf);
                    return new ProxyInput { name = name, proxy = dataProxyBuf };
                }).ToList();
                return ToCBORBytes(dataProxies);
            }
            catch (FfiException)
            {
                decodedFormat = null;
                return null;
            }
        }
    }
    struct ProxyInput
    {
        public string name;
        public byte[] proxy;
        public UInt16 proxy_version;
    }
    public class FfiSubscription
    {
        public List<CBORObject> Proxies { get; set; } = new List<CBORObject>();
    }

    public class FfiException : Exception
    {
        public string Msg { get; }

        public FfiException(string msg) : base(msg)
        {
            Msg = msg;
        }
        public override string Message => Msg;
    }


    public class RpcPluginInfo
    {
        // https://github.com/ReactiveX/RxCpp/issues/420
        public static bool operator ==(RpcPluginInfo left, RpcPluginInfo right)
        {
            if (left.id == right.id && left.hashcode == right.hashcode)
            {
                return true;
            }
            if (left is null || right is null)
            {
                return false;
            }
            return left.Equals(right);
        }
        public static bool operator !=(RpcPluginInfo left, RpcPluginInfo right)
        {
            if (!(left == right))
            {
                return true;
            }
            if (left is null || right is null)
            {
                return false;
            }
            return left.Equals(right);
        }
        public UInt32 id { get; set; }
        public string name { get; set; }
        public string plugin { get; set; }
        public byte[] info { get; set; }
        public UInt32 hashcode { get; set; }
    };
    public class FfiProfile
    {
        public uint id { get; set; }
        public string name { get; set; }
        public string locale { get; set; }

    }

    public class FfiPluginDescriptor
    {
        public string descriptor { get; set; }
    }

    public class FfiPluginVerifyResult
    {
        public IList<FfiPluginDescriptor> required { get; set; }
        public IList<FfiPluginDescriptor> provides { get; set; }
    }

    public class FfiPlugin
    {
        public uint id { get; set; } = 0xFFFFFFFF;
        public string name { get; set; }
        public string desc { get; set; }
        public string plugin { get; set; }
        public ushort plugin_version { get; set; }
        public byte[] param { get; set; }

        public static FfiPluginVerifyResult Verify(string plugin, ushort pluginVersion, byte[] param)
        {
            return BridgeExtensions.ytflow_plugin_verify(plugin, pluginVersion, param);
        }
    }

    public class FfiProxyGroup
    {
        public uint id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class FfiProxyGroupSubscription
    {
        public string format { get; set; }
        public string url { get; set; }
        public ulong? upload_bytes_used { get; set; }
        public ulong? download_bytes_used { get; set; }
        public ulong? bytes_total { get; set; }
        public string expires_at { get; set; }
        public string retrieved_at { get; set; }
    }

    public class FfiDataProxy
    {
        public uint id { get; set; }
        public string name { get; set; }
        public int order_num { get; set; }
        public byte[] proxy { get; set; }
        public ushort proxy_version { get; set; }
    }

    public class FfiResource
    {
        public uint id { get; set; }
        public string key { get; set; }
        public string type { get; set; }
        public string local_file { get; set; }
        public string remote_type { get; set; }
    }

    public class FfiResourceUrl
    {
        public uint id { get; set; }
        public string url { get; set; }
        public string etag { get; set; }
        public string last_modified { get; set; }
        public string retrieved_at { get; set; }
    }

    public class FfiResourceGitHubRelease
    {
        public uint id { get; set; }
        public string github_username { get; set; }
        public string github_repo { get; set; }
        public string asset_name { get; set; }
        public string git_tag { get; set; }
        public string release_title { get; set; }
        public string retrieved_at { get; set; }
    }

    public class FfiParsedTomlPlugin
    {
        public FfiPlugin plugin { get; set; }
        public bool is_entry { get; set; }
    }


    public class FfiParsedTomlProfile
    {
        public string name { get; set; }
        public string locale { get; set; }
        public string created_at { get; set; }
        public List<FfiParsedTomlPlugin> plugins { get; set; }
    }
}

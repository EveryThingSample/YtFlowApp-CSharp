using Newtonsoft.Json;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YtFlowApp2.Classes
{
    internal class CoreProxy
    {
    }

    public class FfiProxyProtocolShadowsocks
    {
        public string cipher { get; set; } = "";
        public byte[] password { get; set; } = new byte[0];
    }

    public class FfiProxyProtocolTrojan
    {
        public byte[] password { get; set; } = new byte[0];
    }

    public class FfiProxyProtocolHttp
    {
        public byte[] username { get; set; } = new byte[0];
        public byte[] password { get; set; } = new byte[0];
    }

    public class FfiProxyProtocolSocks5
    {
        public byte[] username { get; set; } = new byte[0];
        public byte[] password { get; set; } = new byte[0];
    }

    public class FfiProxyProtocolVMess
    {
        public byte[] user_id { get; set; } = new byte[0];
        public ushort alter_id { get; set; }
        public string security { get; set; } = "";
    }

    public class FfiProxyDest
    {
        public string host { get; set; } = "";
        public ushort port { get; set; } = 1;
    }

    public class FfiProxyObfsHttpObfs
    {
        public string host { get; set; } = "";
        public string path { get; set; } = "";
    }

    public class FfiProxyObfsTlsObfs
    {
        public string host { get; set; }
    }

    public class FfiProxyObfsWebSocket
    {
        public string host { get; set; } = "";
        public string path { get; set; } = "";
        public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();
    }

    public class FfiProxyTls
    {
        public List<string> alpn { get; set; } = new List<string>();
        public string sni { get; set; } = "";
        public bool? skip_cert_check { get; set; }
    }

    public class FfiProxyLeg
    {
        public object protocol { get; set; } // Could be one of the protocol types
        public FfiProxyDest dest { get; set; }
        public object obfs { get; set; } // Could be one of the obfuscation types
        public FfiProxyTls tls { get; set; }
        public CBORObject ToCbor2()
        {
            //  CBORObject
            var cbor = CBORObject.NewMap();

            // Convert protocol
            if (protocol != null)
            {
                var protocolCbor = CBORObject.NewMap();
                if (protocol is FfiProxyProtocolShadowsocks)
                {
                    protocolCbor.Add("Shadowsocks", ((FfiProxyProtocolShadowsocks)protocol));
                }
                else if (protocol is FfiProxyProtocolTrojan)
                {
                    protocolCbor.Add("Trojan", ((FfiProxyProtocolTrojan)protocol));
                }
                else if (protocol is FfiProxyProtocolHttp)
                {
                    protocolCbor.Add("Http", ((FfiProxyProtocolHttp)protocol));
                }
                else if (protocol is FfiProxyProtocolSocks5)
                {
                    protocolCbor.Add("Socks5", ((FfiProxyProtocolSocks5)protocol));
                }
                else if (protocol is FfiProxyProtocolVMess)
                {
                    protocolCbor.Add("VMess", ((FfiProxyProtocolVMess)protocol));
                }

                cbor.Add("protocol", protocolCbor);
            }

            // Convert dest
            if (dest != null)
            {
                cbor.Add("dest", CBORObject.FromObject(dest));
            }

            // Convert obfs
            if (obfs != null)
            {
                var obfsCbor = CBORObject.NewMap();
                if (obfs is FfiProxyObfsHttpObfs)
                {
                    obfsCbor.Add("HttpObfs", ((FfiProxyObfsHttpObfs)obfs));
                }
                else if (obfs is FfiProxyObfsTlsObfs)
                {
                    obfsCbor.Add("TlsObfs", ((FfiProxyObfsTlsObfs)obfs));
                }
                else if (obfs is FfiProxyObfsWebSocket)
                {
                    obfsCbor.Add("WebSocket", ((FfiProxyObfsWebSocket)obfs));
                }

                cbor.Add("obfs", obfsCbor);
            }

            // Convert tls
            if (tls != null)
            {
                cbor.Add("tls", CBORObject.FromObject(tls));
            }

            return cbor;
        }
        public CBORObject ToCbor()
        {
            CBORObject cbor = CBORObject.NewMap();
            // Encode protocol
            if (protocol != null)
            {
                var protocolCbor = CBORObject.NewMap();
                switch (protocol)
                {
                    case FfiProxyProtocolShadowsocks shadowsocks:
                        protocolCbor.Add("Shadowsocks", shadowsocks);
                        break;
                    case FfiProxyProtocolTrojan trojan:
                        protocolCbor.Add("Trojan");
                        break;
                    case FfiProxyProtocolHttp http:
                        protocolCbor.Add("Http", http);
                        break;
                    case FfiProxyProtocolSocks5 socks5:
                        protocolCbor.Add("Socks5", socks5);
                        break;
                    case FfiProxyProtocolVMess vmess:
                        protocolCbor.Add("VMess", vmess);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown protocol type.");
                }
                cbor.Add("protocol", protocolCbor);
            }

            // Encode dest
            if (dest != null)
            {
                cbor.Add("dest", CBORObject.FromObject(dest));
            }

            // Encode obfs (optional)
            if (obfs != null)
            {
                var obfsCbor = CBORObject.NewMap();
                switch (obfs)
                {
                    case FfiProxyObfsHttpObfs httpObfs:
                        obfsCbor.Add("HttpObfs", httpObfs);
                        break;
                    case FfiProxyObfsTlsObfs tlsObfs:
                        obfsCbor.Add("TlsObfs", tlsObfs);
                        break;
                    case FfiProxyObfsWebSocket webSocket:
                        obfsCbor.Add("WebSocket", webSocket);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown obfs type.");
                }
                cbor.Add("obfs", obfsCbor);
            }

            // Encode tls (optional)
            if (tls != null)
            {
                cbor.Add("tls", CBORObject.FromObject(tls));
            }
            return cbor;
        }
        public static FfiProxyLeg FromCbor(PeterO.Cbor.CBORObject cbor)
        {

            var ffiProxyLeg = new FfiProxyLeg();


            var protocolObj = cbor["protocol"];

            if (!protocolObj.IsNull && protocolObj.Keys.Count > 0)
            {

                var protocolKey = protocolObj.Keys.First().AsString();
                protocolObj = protocolObj[protocolObj.Keys.First()];
                if (protocolKey == "Shadowsocks")
                {
                    ffiProxyLeg.protocol = protocolObj.ToObject<FfiProxyProtocolShadowsocks>();
                }
                else if (protocolKey == "Trojan")
                {
                    ffiProxyLeg.protocol = protocolObj.ToObject<FfiProxyProtocolTrojan>();
                }
                else if (protocolKey == "Http")
                {
                    ffiProxyLeg.protocol = protocolObj.ToObject<FfiProxyProtocolHttp>();
                }
                else if (protocolKey == "Socks5")
                {
                    ffiProxyLeg.protocol = protocolObj.ToObject<FfiProxyProtocolSocks5>();
                }
                else if (protocolKey == "VMess")
                {
                    ffiProxyLeg.protocol = protocolObj.ToObject<FfiProxyProtocolVMess>();
                }
            }

            // parser dest
            var destObj = cbor["dest"];
            if (!destObj.IsNull)
            {
                ffiProxyLeg.dest = destObj.ToObject<FfiProxyDest>();
            }

            // parser obfs
            var obfsObj = cbor["obfs"];
            if (!obfsObj.IsNull)
            {
                var obfsKey = obfsObj.AsString();
                if (obfsKey == "HttpObfs")
                {
                    ffiProxyLeg.obfs = obfsObj.ToObject<FfiProxyObfsHttpObfs>();
                }
                else if (obfsKey == "TlsObfs")
                {
                    ffiProxyLeg.obfs = obfsObj.ToObject<FfiProxyObfsTlsObfs>();
                }
                else if (obfsKey == "WebSocket")
                {
                    ffiProxyLeg.obfs = obfsObj.ToObject<FfiProxyObfsWebSocket>();
                }
            }

            // parser tls
            var tlsObj = cbor["tls"];
            if (!tlsObj.IsNull)
            {
                ffiProxyLeg.tls = tlsObj.ToObject<FfiProxyTls>();
            }

            return ffiProxyLeg;
        }

    }


    public class FfiProxy
    {
        public string name { get; set; } = "";
        public List<FfiProxyLeg> legs { get; set; }
        public bool udp_supported { get; set; }
        public static PeterO.Cbor.CBORObject c;
        public static FfiProxy FromCbor(PeterO.Cbor.CBORObject cbor)
        {

            var FfiProxy = new FfiProxy();
            FfiProxy.name = cbor["name"].AsString();
            FfiProxy.legs = new List<FfiProxyLeg>();
            FfiProxy.udp_supported = cbor["udp_supported"].AsBoolean();

            for (var i = 0; i < cbor["legs"].Count; i++)
            {
                FfiProxy.legs.Add(FfiProxyLeg.FromCbor(cbor["legs"][i]));
            }

            return FfiProxy;
        }
        public PeterO.Cbor.CBORObject ToCbor()
        {

            var ret = PeterO.Cbor.CBORObject.NewMap();
            ret["name"] = PeterO.Cbor.CBORObject.FromObject(name);
            ret["udp_supported"] = PeterO.Cbor.CBORObject.FromObject(udp_supported);
            ret["legs"] = PeterO.Cbor.CBORObject.NewArray();
            foreach (var leg in legs)
            {
                ret["legs"].Add(leg.ToCbor());
            }
            return ret;

        }
    }
    // Example methods to deserialize
    public static class ProxyUtils
    {
        public static FfiProxyLeg DecodeProxyLeg(string json)
        {
            return JsonConvert.DeserializeObject<FfiProxyLeg>(json);
        }

        public static string EncodeProxyLeg(FfiProxyLeg leg)
        {
            return JsonConvert.SerializeObject(leg);
        }
    }
}

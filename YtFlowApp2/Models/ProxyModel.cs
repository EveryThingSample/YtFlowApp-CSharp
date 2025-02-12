using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YtFlowApp2.Classes;
using YtFlowApp2.CoreInterop;


namespace YtFlowApp2.Models
{
    public sealed class ProxyModel : INotifyPropertyChanged
    {
        private uint _id;
        private string _name;
        private byte[] _proxy;
        private ushort _proxyVersion;
        private FfiProxy _analyzedProxy;
        private event PropertyChangedEventHandler _propertyChanged;

        public ProxyModel() { }

        public ProxyModel(FfiDataProxy proxy)
        {
            _id = proxy.id;
            _name = proxy.name;
            _proxy = proxy.proxy.ToArray();
            _proxyVersion = proxy.proxy_version;
            Analyze(proxy.name);
        }

        public uint Id => _id;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    _analyzedProxy = null;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Summary));
                    OnPropertyChanged(nameof(Tooltip));
                }
            }
        }

        public byte[] Proxy
        {
            get => _proxy;
            set
            {
                if (!_proxy.SequenceEqual(value))
                {
                    _proxy = value;
                    _analyzedProxy = null;
                    OnPropertyChanged(nameof(Proxy));
                    OnPropertyChanged(nameof(Summary));
                    OnPropertyChanged(nameof(Tooltip));
                }
            }
        }

        public ushort ProxyVersion
        {
            get => _proxyVersion;
            set
            {
                if (_proxyVersion != value)
                {
                    _proxyVersion = value;
                    _analyzedProxy = null;
                    OnPropertyChanged(nameof(ProxyVersion));
                    OnPropertyChanged(nameof(Summary));
                    OnPropertyChanged(nameof(Tooltip));
                }
            }
        }
        public string Summary
        {
            get
            {
                var proxy = Analyze();
                if (proxy == null)
                {
                    return string.Empty;
                }

                var legs = proxy.legs;
                if (legs.Count == 0)
                {
                    return "Direct";
                }

                string ret = string.Empty;
                Action<FfiProxyLeg> pushLeg = (FfiProxyLeg leg) =>
                {

                    // Handle protocol
                    switch (leg.protocol.GetType().Name)
                    {
                        case nameof(FfiProxyProtocolShadowsocks):
                            ret += "Shadowsocks(";
                            break;
                        case nameof(FfiProxyProtocolTrojan):
                            ret += "Trojan(";
                            break;
                        case nameof(FfiProxyProtocolSocks5):
                            ret += "SOCKS5(";
                            break;
                        case nameof(FfiProxyProtocolHttp):
                            ret += "HTTP(";
                            break;
                        case nameof(FfiProxyProtocolVMess):
                            ret += "VMess(";
                            break;
                        default:
                            throw new InvalidOperationException("Unknown protocol type.");
                    }

                    ret += $"{leg.dest.host}:{leg.dest.port})";

                    // Handle obfs
                    if (leg.obfs != null)
                    {
                        if (leg.obfs is FfiProxyObfsHttpObfs)
                        {
                            ret += ", simple-obfs";
                        }
                        else if (leg.obfs is FfiProxyObfsTlsObfs)
                        {
                            ret += ", simple-obfs";
                        }
                        else if (leg.obfs is FfiProxyObfsWebSocket)
                        {
                            ret += ", WebSocket";
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown obfs type.");
                        }
                    }

                    // Handle TLS
                    if (leg.tls != null)
                    {
                        ret += ", TLS";
                    }
                };

                pushLeg(legs[0]);
                for (int i = 1; i < legs.Count; i++)
                {
                    ret += " -> ";
                    pushLeg(legs[i]);
                }

                return ret;
            }
        }

        public string Tooltip => $"{Name}\r\n{Summary}";

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private void OnPropertyChanged(string propertyName)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal FfiProxy Analyze(string name = null)
        {
            if (_analyzedProxy != null)
            {
                return _analyzedProxy;
            }

            name =name?? _name;
            try
            {
                _analyzedProxy = BridgeExtensions.ytflow_app_proxy_data_proxy_analyze(name, _proxy, _proxyVersion);
            }
            catch (FfiException)
            {
                // Handle exception if needed
            }

            return _analyzedProxy;
        }

        public void Update()
        {
            var conn = BridgeExtensions.FfiDbInstance.Connect();
            conn.UpdateProxy(_id, _name, _proxy, _proxyVersion);
        }
    }

    
}

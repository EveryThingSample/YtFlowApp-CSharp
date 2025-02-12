using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;
using YtFlowApp2.Classes;

namespace YtFlowApp2.Models
{
    public class ProxyLegModel : INotifyPropertyChanged
    {
        private bool _isReadonly;
        private string _protocolType = "SOCKS5";
        private string _shadowsocksEncryptionMethod = "none";
        private string _vmessEncryptionMethod = "none";
        private ushort _alterId;
        private string _username;
        private string _password;
        private string _host = "localhost";
        private ushort _port = 1;
        private string _obfsType = "none";
        private string _obfsHost;
        private string _obfsPath = "/";
        private string _obfsHeaders;
        private bool _enableTls;
        private string _sni = "auto";
        private string _alpn;
        private bool? _skipCertCheck; // Changed from IReference<bool> to nullable bool (bool?)

        public event PropertyChangedEventHandler PropertyChanged;

        public ProxyLegModel() { }

        public ProxyLegModel(bool isReadonly, FfiProxyLeg leg)
        {
            _isReadonly = isReadonly;

            switch (leg.protocol)
            {
                case FfiProxyProtocolShadowsocks ss:
                    _protocolType = "Shadowsocks";
                    _shadowsocksEncryptionMethod = ss.cipher;
                    ConvertBinaryToString(ss.password, ref _password);
                    break;
                case FfiProxyProtocolTrojan trojan:
                    _protocolType = "Trojan";
                    ConvertBinaryToString(trojan.password, ref _password);
                    break;
                case FfiProxyProtocolHttp http:
                    _protocolType = "HTTP";
                    ConvertBinaryToString(http.username, ref _username);
                    ConvertBinaryToString(http.password, ref _password);
                    break;
                case FfiProxyProtocolSocks5 socks5:
                    _protocolType = "SOCKS5";
                    ConvertBinaryToString(socks5.username, ref _username);
                    ConvertBinaryToString(socks5.password, ref _password);
                    break;
                case FfiProxyProtocolVMess vmess:
                    _protocolType = "VMess";
                    _vmessEncryptionMethod = vmess.security;
                    _alterId = vmess.alter_id;
                    _password = vmess.user_id.ToString();
                    break;
            }

            _host = leg.dest.host;
            _port = leg.dest.port;

            if (leg.obfs != null)
            {
                var obfs = leg.obfs;
                switch (obfs)
                {
                    case FfiProxyObfsHttpObfs httpObfs:
                        _obfsType = "simple-obfs (HTTP)";
                        _obfsHost = httpObfs.host;
                        _obfsPath = httpObfs.path;
                        break;
                    case FfiProxyObfsTlsObfs tlsObfs:
                        _obfsType = "simple-obfs (TLS)";
                        _obfsHost = tlsObfs.host;
                        break;
                    case FfiProxyObfsWebSocket wsObfs:
                        _obfsType = "WebSocket";
                        _obfsHost = wsObfs.host ?? "";
                        _obfsPath = wsObfs.path;
                        _obfsHeaders = string.Join("\r\n", wsObfs.headers.Select(h => $"{h.Key}: {h.Value}"));
                        break;
                }
            }

            _enableTls = leg.tls != null;
            if (_enableTls)
            {
                var tls = leg.tls;
                _sni = tls.sni ?? "auto";
                _skipCertCheck = tls.skip_cert_check;
                _alpn = string.Join(",", tls.alpn);
            }
        }

        public bool IsReadonly => _isReadonly;

        public bool IsWritable => !_isReadonly;

        public string ProtocolType
        {
            get => _protocolType;
            set => SetProperty(ref _protocolType, value);
        }

        public string ShadowsocksEncryptionMethod
        {
            get => _shadowsocksEncryptionMethod;
            set => SetProperty(ref _shadowsocksEncryptionMethod, value);
        }

        public string VMessEncryptionMethod
        {
            get => _vmessEncryptionMethod;
            set => SetProperty(ref _vmessEncryptionMethod, value);
        }

        public ushort AlterId
        {
            get => _alterId;
            set => SetProperty(ref _alterId, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        public ushort Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string ObfsType
        {
            get => _obfsType;
            set => SetProperty(ref _obfsType, value);
        }

        public string ObfsHost
        {
            get => _obfsHost;
            set => SetProperty(ref _obfsHost, value);
        }

        public string ObfsPath
        {
            get => _obfsPath;
            set => SetProperty(ref _obfsPath, value);
        }

        public string ObfsHeaders
        {
            get => _obfsHeaders;
            set => SetProperty(ref _obfsHeaders, value);
        }

        public bool EnableTls
        {
            get => _enableTls;
            set => SetProperty(ref _enableTls, value);
        }

        public string Sni
        {
            get => _sni;
            set => SetProperty(ref _sni, value);
        }

        public string Alpn
        {
            get => _alpn;
            set => SetProperty(ref _alpn, value);
        }

        public bool? SkipCertCheck
        {
            get => _skipCertCheck;
            set => SetProperty(ref _skipCertCheck, value);
        }

        public string Dest => $"{_host}:{_port}";

        public string Summary
        {
            get
            {
                var ret = _protocolType;
                if (_obfsType != "none")
                {
                    ret += $", {_obfsType}";
                }
                if (_enableTls)
                {
                    ret += ", TLS";
                }
                return ret;
            }
        }

        private void SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(storage, value))
            {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                if (propertyName != nameof(Summary))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
                }
                if (propertyName != nameof(Dest))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dest)));
                }
            }
        }

        private static void ConvertBinaryToString(byte[] bytes, ref string target)
        {
            target = Encoding.UTF8.GetString(bytes);
        }

        public FfiProxyLeg Encode()
        {
            object protocol;
            if (_protocolType == "Shadowsocks")
            {
                protocol = new FfiProxyProtocolShadowsocks
                {
                    cipher = _shadowsocksEncryptionMethod,
                    password = Encoding.UTF8.GetBytes(_password)
                };
            }
            else if (_protocolType == "Trojan")
            {
                protocol = new FfiProxyProtocolTrojan
                {
                    password = Encoding.UTF8.GetBytes(_password)
                };
            }
            else if (_protocolType == "HTTP")
            {
                protocol = new FfiProxyProtocolHttp
                {
                    username = Encoding.UTF8.GetBytes(_username),
                    password = Encoding.UTF8.GetBytes(_password)
                };
            }
            else if (_protocolType == "SOCKS5")
            {
                protocol = new FfiProxyProtocolSocks5
                {
                    username = Encoding.UTF8.GetBytes(_username),
                    password = Encoding.UTF8.GetBytes(_password)
                };
            }
            else if (_protocolType == "VMess")
            {
                protocol = new FfiProxyProtocolVMess
                {
                    alter_id = _alterId,
                    security = _vmessEncryptionMethod,
                    user_id =  Guid.Parse(_password).ToByteArray(),
                };
            }
            else
            {
                throw new ArgumentException("Unsupported protocol type", nameof(_protocolType));
            }

            object obfs = null;
            if (_obfsType == "simple-obfs (HTTP)")
            {
                obfs = new FfiProxyObfsHttpObfs
                {
                    host = _obfsHost,
                    path = _obfsPath
                };
            }
            else if (_obfsType == "simple-obfs (TLS)")
            {
                obfs = new FfiProxyObfsTlsObfs
                {
                    host = _obfsHost
                };
            }
            else if (_obfsType == "WebSocket")
            {
                obfs = new FfiProxyObfsWebSocket
                {
                    host = _obfsHost,
                    path = _obfsPath,
                    headers = _obfsHeaders.Split("\r\n").ToDictionary(
                        h => h.Split(':')[0],
                        h => h.Split(':')[1]
                    )
                };
            }

            FfiProxyTls tls = null;
            if (_enableTls)
            {
                tls = new FfiProxyTls
                {
                    sni = (_sni != "auto") ? _sni : null,
                    alpn = _alpn.Split(',').ToList(),
                    skip_cert_check = _skipCertCheck
                };
            }

            return new FfiProxyLeg
            {
                protocol = protocol,
                dest = new FfiProxyDest { host = _host, port = _port },
                obfs = obfs,
                tls = tls
            };
        }
    }
}

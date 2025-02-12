# YtFlowApp-CSharp


YtFlowApp-CSharp is a network debug tool running on the Universal Windows Platform (UWP) that forwards any network traffic through certain types of tunnels, powered by YtFlowCore. This project is a rewrite of the UI pages in C# based on the original [YtFlowApp](https://github.com/YtFlow/YtFlowApp).


## Supported Features

### Protocols
- [Shadowsocks](https://ytflow.github.io/ytflow-book/plugins/shadowsocks-client.html)
  - stream ciphers (rc4-md5, aes-128-cfb, aes-192-cfb, aes-256-cfb, aes-128-ctr, aes-192-ctr, aes-256-ctr, camellia-128-cfb, camellia-192-cfb, camellia-256-cfb, salsa20, chacha20-ietf)
  - AEAD ciphers (aes-128-gcm, aes-192-gcm, aes-256-gcm, chacha20-ietf-poly1305, xchacha20-ietf-poly1305)
- [Trojan](https://ytflow.github.io/ytflow-book/plugins/trojan-client.html)
- [HTTP](https://ytflow.github.io/ytflow-book/plugins/http-proxy-client.html)
  - `CONNECT`-based proxy
- [SOCKS5](https://ytflow.github.io/ytflow-book/plugins/socks5-client.html)
- [VMess](https://ytflow.github.io/ytflow-book/plugins/vmess-client.html)
  - VMess AEAD header format (`alterID`=0)
  - Customizable WebSocket and TLS transport

### Transports

- [TLS](https://ytflow.github.io/ytflow-book/plugins/tls-client.html)
- [WebSocket](https://ytflow.github.io/ytflow-book/plugins/ws-client.html)
  - Based on HTTP/1.1
- `simple-obfs` compatible obfuscation
  - [http](https://ytflow.github.io/ytflow-book/plugins/http-obfs-client.html)
  - [tls](https://ytflow.github.io/ytflow-book/plugins/tls-obfs-client.html)

### Share Links

- Legacy Shadowsocks format
- Shadowsocks SIP002
- SOCKS5
- HTTP Proxy
- Trojan
- VMess (v2rayN flavor)

### Subscriptions

- Base64-encoded share links
- SIP008
- Surge proxy list

## Getting Started

- [Quick Start](https://github.com/YtFlow/YtFlowApp/wiki/Quick-Start)
- [Configuration Guide](https://github.com/YtFlow/YtFlowApp/wiki/Configuration-Guide)
- [YtFlowCore Book](https://ytflow.github.io/ytflow-book)

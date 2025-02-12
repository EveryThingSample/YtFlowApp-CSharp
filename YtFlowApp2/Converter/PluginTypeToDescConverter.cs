using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace YtFlowApp2.Converter
{
    public class PluginTypeToDescConverter : IValueConverter
    {
        internal static readonly Dictionary<string, object> DescMap = new Dictionary<string, object>
        {
            { "reject", "Silently reject any incoming requests." },
            { "null", "Silently drop any outgoing requests." },
            { "ip-stack", "Handle TCP or UDP connections from a TUN." },
            { "socket-listener", "Bind a socket to a specified port and listen for connections or datagrams." },
            { "vpn-tun", "An instance to be instantiated by a VPN system service, such as UWP VPN Plugin on Windows." },
            { "host-resolver", "Resolve real IP addresses by querying DNS servers." },
            { "fake-ip", "Assign a fake IP address for each domain name. This is useful for TUN inbounds where incoming connections carry no information about domain names." },
            { "system-resolver", "Resolve real IP addresses by calling system functions. This is the recommended resolver for simple proxy scenarios for both client and server." },
            { "switch", "Handle incoming connections using runtime-selected handlers from a pre-defined list." },
            { "dns-server", "Respond to DNS request messages using results returned by the specified resolver. Also provides domain name lookup (map_back) for resolved IP addresses." },
            { "socks5-server", "SOCKS5 server." },
            { "http-obfs-server", "simple-obfs HTTP server." },
            { "resolve-dest", "Resolve domain names in flow destinations to IP addresses." },
            { "simple-dispatcher", "Match the source/dest address against a list of simple rules, and use the corresponding handler or fallback handler if there is no match." },
            { "rule-dispatcher", "Match the connection against rules defined in a resource, and use the handler of a corresponding action or fallback if there is no match." },
            { "list-dispatcher", "Match the connection against a list of matchers defined in a resource, and use the handler of the action or fallback handler if there is no match." },
            { "forward", "Establish a new connection for each incoming connection, and forward data between them." },
            { "dyn-outbound", "Select an outbound proxy from the database at runtime." },
            { "shadowsocks-client", "Shadowsocks client." },
            { "socks5-client", "SOCKS5 client." },
            { "http-proxy-client", "HTTP Proxy client. Use HTTP CONNECT to connect to the proxy server." },
            { "tls-client", "TLS client stream." },
            { "trojan-client", "Trojan client. Note that TLS is not included. You will likely need to connect this plugin to a TLS plugin." },
            { "vmess-client", "VMess client." },
            { "http-obfs-client", "simple-obfs HTTP client." },
            { "tls-obfs-client", "simple-obfs TLS client." },
            { "ws-client", "WebSocket client." },
            { "redirect", "Change the destination of connections or datagrams." },
            { "socket", "Represents a system socket connection." },
            { "netif", "A dynamic network interface." }
        };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string pluginType)
            {
                if (DescMap.TryGetValue(pluginType, out var description))
                {
                    return description;
                }
            }
            return value; // Return the original value if no description is found
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

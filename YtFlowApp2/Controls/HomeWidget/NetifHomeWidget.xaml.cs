using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Pages;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace YtFlowApp2.Controls.HomeWidget
{
    public sealed partial class NetifHomeWidget : UserControl, IHomeWidget
    {
        private struct Netif
        {
            [JsonProperty("name")]
            public string name { get; set; }

          /*  [JsonProperty("ipv4_addr")]
            public IPEndPoint ipv4_addr { get; set; }

            [JsonProperty("ipv6_addr")]
            public IPEndPoint ipv6_addr { get; set; }*/

            [JsonProperty("dns_servers")]
            public List<string> dns_servers { get; set; }
        }

        private struct NetifInfo
        {
            [JsonProperty("netif")]
            public Netif netif { get; set; }
        }

        private WidgetHandle _sharedInfo;

        public NetifHomeWidget()
        {
            this.InitializeComponent();
        }

        public NetifHomeWidget(string pluginName, WidgetHandle sharedInfo)
        {
            _sharedInfo = sharedInfo;
            this.InitializeComponent();

            PluginNameText.Text = pluginName;
        }

        public void UpdateInfo()
        {
            try
            {
                var CBOR = BridgeExtensions.FromCBORBytes(_sharedInfo.Info.ToArray());

                var info = CBOR.ToObject<NetifInfo>();

                var netif = CBOR["netif"];

                PreviewInterfaceNameText.Text = info.netif.name;
                InterfaceNameText.Text = info.netif.name;

                if (netif.ContainsKey("ipv4_addr") && !netif["ipv4_addr"].IsNull)
                {
                    // var ipBytes = info.netif.ipv4_addr.GetAddressBytes();
                    // var port = info.netif.ipv4_addr.Item2;
                    // var ipAddress = new IPAddress(ipBytes);
                  //  var ipEndpoint = info.netif.ipv4_addr;
                    Ipv4AddrText.Text = netif["ipv4_addr"][0].ToString();
                }

                if (netif.ContainsKey("ipv6_addr") && !netif["ipv6_addr"].IsNull)
                {
                    /*                    var ipBytes = info.netif.ipv6_addr.Item1;
                                        var port = info.netif.ipv6_addr.Item2;
                                        var ipAddress = new IPAddress(ipBytes);
                                        var ipEndpoint = new IPEndPoint(ipAddress, port);*/
                   // var ipEndpoint = info.netif.ipv6_addr;
                    Ipv6AddrText.Text = netif["ipv6_addr"][0].ToString();
                }

                var dnsServers = string.Join(", ", info.netif.dns_servers);
                DnsText.Text = dnsServers;
                
            }
            catch(PeterO.Cbor.CBORException pex)
            {
                NotifyException("Updating Netif", pex);
            }
            catch (Exception ex)
            {
                NotifyException("Updating Netif", ex);
            }
        }

        private void NotifyException(string message, Exception ex)
        {
            // Implement exception notification logic here
        }
    }
}

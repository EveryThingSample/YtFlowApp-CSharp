using Newtonsoft.Json.Linq;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.Controls;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Models;
using YtFlowApp2.Pages.PluginEditor;
using YtFlowApp2.Utils;



namespace YtFlowApp2.Pages
{
    public class NewProfileConfigRuleset
    {
        public string Name { get; set; } 
        public List<KeyValuePair<string, SplitRoutingRuleDecision>> Rules { get; set; } = new List<KeyValuePair<string, SplitRoutingRuleDecision>>(); 
        public bool IsList { get; set; } 
        public SplitRoutingRuleDecision FallbackRule { get; set; } 
    }
    public class NewProfileConfig
    {
        public string InboundMode { get; set; } 
        public string RuleResolver { get; set; } 
        public string OutboundType { get; set; } 
        public string CustomRules { get; set; } 
        public string FakeIpFilter { get; set; } 
        public List<NewProfileConfigRuleset> Rulesets { get; set; } = new List<NewProfileConfigRuleset>(); 
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewProfilePage : Page
    {


        public NewProfilePage()
        {
            InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            bool isWelcoming = e.Parameter as bool? ?? false;
            HeaderControl.Visibility = isWelcoming ? Visibility.Collapsed : Visibility.Visible;
            WelcomeHeaderControl.Visibility = isWelcoming ? Visibility.Visible : Visibility.Collapsed;

            string suggestedName = "New Profile";
            try
            {
                var conn = BridgeExtensions.FfiDbInstance.Connect();
                suggestedName = $"New Profile {BridgeExtensions.GetProfiles(conn).Count + 1}";
            }
            catch
            {
                // Ignore errors
            }
            NewProfileNameText.Text = suggestedName;
            NewProfileNameText.IsEnabled = true;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if ((!SaveButton.IsEnabled || WelcomeHeaderControl.Visibility == Visibility.Visible) && NewProfileNameText.IsEnabled)
            {
                e.Cancel = true;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newProfileName = NewProfileNameText.Text;
                if (string.IsNullOrEmpty(newProfileName))
                {
                    NewProfileNameText.Focus(FocusState.Programmatic);
                    return;
                }

                SaveButton.IsEnabled = false;
                bool nameDuplicated = false;
                var config = new NewProfileConfig
                {
                    InboundMode = (InboundModeButtons.SelectedItem as RadioButton)?.Tag as string,
                    RuleResolver = (RuleResolverComboBox.SelectedItem as ComboBoxItem)?.Tag as string,
                    OutboundType = _selectedOutboundType,
                    CustomRules = CustomRuleTextBox.Text,
                    FakeIpFilter = FakeIpFilterTextBox.Text,
                    Rulesets = RulesetListView.Items.Select(item =>
                    {
                        var control = (item as SplitRoutingRulesetControl);
                        return new NewProfileConfigRuleset
                        {
                            Name = control.RulesetName,
                            Rules = control.RuleList.Select(ruleModel =>
                            {
                                var ruleObj = ruleModel as SplitRoutingRuleModel;
                                return new KeyValuePair<string, SplitRoutingRuleDecision>(ruleObj.Rule, ruleObj.Decision);
                            }).ToList(),
                            IsList = !control.CanModifyRuleList,
                            FallbackRule = control.FallbackRule.Decision
                        };
                    }).ToList()
                };

                await Task.Run(() =>
                {
                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    foreach (var profile in BridgeExtensions.GetProfiles(conn))
                    {
                        if (profile.name == newProfileName)
                        {
                            nameDuplicated = true;
                            break;
                        }
                    }
                    if (!nameDuplicated)
                    {
                        var id = conn.CreateProfile(newProfileName, "en-US");
                        //brig
                        CreatePresetPlugins(id, config);
                    }
                });

                SaveButton.IsEnabled = true;
                if (nameDuplicated)
                {
                    NewProfileNameText.IsEnabled = true;
                    NewProfileNameText.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                }
                else
                {
                    NewProfileNameText.IsEnabled = false;
                    Frame.GoBack();
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Saving", ex);
            }
        }
        private string _selectedOutboundType = "dyn";
        private void OutboundTypeButton_Checked(object sender, RoutedEventArgs e)
        {
            var btn = sender as FrameworkElement;
            _selectedOutboundType = btn?.Tag as string;
            if (_selectedOutboundType == "dyn")
            {
                DynOutboundDisabledText.Visibility = Visibility.Collapsed;
                DynOutboundEnabledText.Visibility = Visibility.Visible;
                SsButton.IsChecked = false;
                TrojanButton.IsChecked = false;
                VmessWsTlsButton.IsChecked = false;
                HttpButton.IsChecked = false;
                SsButton.IsEnabled = false;
                TrojanButton.IsEnabled = false;
                VmessWsTlsButton.IsEnabled = false;
                HttpButton.IsEnabled = false;
            }
        }

        private async void SplitRoutingModeButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems.FirstOrDefault();
            if (selectedItem == null)
            {
                return;
            }
            var selected = selectedItem as FrameworkElement;
            if (selected == null)
            {
                return;
            }
            var selectedMode = selected.Tag as string;
            int lastSelectedIndex = 0; // Workaround for e.RemovedItems().First() -> nullptr
            var idx = SplitRoutingModeButtons.SelectedIndex;
            if (lastSelectedIndex == idx)
            {
                return;
            }

            bool splitRoutingEnabled = selectedMode != "all";
            if (splitRoutingEnabled)
            {
                if (await DownloadRulesetConsentDialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    SplitRoutingModeButtons.SelectedIndex = lastSelectedIndex;
                    return;
                }
            }

            RulesetListView.Items.Clear();
            if (!splitRoutingEnabled)
            {
                lastSelectedIndex = 0;
                return;
            }

            await RulesetDialog.ShowAsync();
            bool updated = false;
            if (selectedMode == "whitelist")
            {
                updated = await (RulesetDialog as NewProfileRulesetControl).BatchUpdateRulesetsIfNotExistAsync(new List<string>
            {
                "loyalsoldier-country-only-cn-private",
                "loyalsoldier-surge-proxy",
                "loyalsoldier-surge-direct",
                "loyalsoldier-surge-private",
                "loyalsoldier-surge-reject"
            });
                if (updated)
                {
                    AddListRuleset("loyalsoldier-surge-private", SplitRoutingRuleDecision.Direct);
                    AddListRuleset("loyalsoldier-surge-reject", SplitRoutingRuleDecision.Reject);
                    AddListRuleset("loyalsoldier-surge-proxy", SplitRoutingRuleDecision.Proxy);
                    AddListRuleset("loyalsoldier-surge-direct", SplitRoutingRuleDecision.Direct);
                    AddRuleRuleset("loyalsoldier-country-only-cn-private", "cn", SplitRoutingRuleDecision.Direct);
                }
            }
            else if (selectedMode == "blacklist")
            {
                updated = await (RulesetDialog as NewProfileRulesetControl).BatchUpdateRulesetsIfNotExistAsync(new List<string>
            {
                "loyalsoldier-surge-proxy",
                "loyalsoldier-surge-tld-not-cn",
                "loyalsoldier-surge-private",
                "loyalsoldier-surge-reject"
            });
                if (updated)
                {
                    AddListRuleset("loyalsoldier-surge-private", SplitRoutingRuleDecision.Direct);
                    AddListRuleset("loyalsoldier-surge-reject", SplitRoutingRuleDecision.Reject);
                    AddListRuleset("loyalsoldier-surge-proxy", SplitRoutingRuleDecision.Proxy);
                    AddListRuleset("loyalsoldier-surge-tld-not-cn", SplitRoutingRuleDecision.Proxy, SplitRoutingRuleDecision.Direct);
                }
            }
            else if (selectedMode == "overseas")
            {
                updated = await (RulesetDialog as NewProfileRulesetControl).BatchUpdateRulesetsIfNotExistAsync(new List<string>
            {
                "loyalsoldier-country-only-cn-private",
                "loyalsoldier-surge-proxy",
                "loyalsoldier-surge-direct",
                "loyalsoldier-surge-tld-not-cn",
                "loyalsoldier-surge-private",
                "loyalsoldier-surge-reject"
            });
                if (updated)
                {
                    AddListRuleset("loyalsoldier-surge-private", SplitRoutingRuleDecision.Direct);
                    AddListRuleset("loyalsoldier-surge-reject", SplitRoutingRuleDecision.Reject);
                    AddListRuleset("loyalsoldier-surge-proxy", SplitRoutingRuleDecision.Direct);
                    AddListRuleset("loyalsoldier-surge-direct", SplitRoutingRuleDecision.Proxy);
                    AddListRuleset("loyalsoldier-surge-tld-not-cn", SplitRoutingRuleDecision.Direct);
                    AddRuleRuleset("loyalsoldier-country-only-cn-private", "cn", SplitRoutingRuleDecision.Proxy, SplitRoutingRuleDecision.Direct);
                }
            }
            if (updated)
            {
                RulesetDialog.Hide();
                lastSelectedIndex = idx;
            }
            else
            {
                SplitRoutingModeButtons.SelectedIndex = lastSelectedIndex;
            }
        }

        private void DynOutboundButton_Unchecked(object sender, RoutedEventArgs e)
        {
            DynOutboundDisabledText.Visibility = Visibility.Visible;
            DynOutboundEnabledText.Visibility = Visibility.Collapsed;
            SsButton.IsEnabled = true;
            SsButton.IsChecked = true;
            TrojanButton.IsEnabled = true;
            VmessWsTlsButton.IsEnabled = true;
            HttpButton.IsEnabled = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DynOutboundButton.Checked += OutboundTypeButton_Checked;
            SsButton.Checked += OutboundTypeButton_Checked;
            TrojanButton.Checked += OutboundTypeButton_Checked;
            VmessWsTlsButton.Checked += OutboundTypeButton_Checked;
            HttpButton.Checked += OutboundTypeButton_Checked;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DynOutboundButton.Checked -= OutboundTypeButton_Checked;
            SsButton.Checked -= OutboundTypeButton_Checked;
            TrojanButton.Checked -= OutboundTypeButton_Checked;
            VmessWsTlsButton.Checked -= OutboundTypeButton_Checked;
            HttpButton.Checked -= OutboundTypeButton_Checked;
        }

        private void NewProfileNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewProfileNameText.Foreground = null;
        }

        private async void AddRulesetButton_Click(object sender, RoutedEventArgs e)
        {
            await RulesetDialog.ShowAsync();
            if ((RulesetDialog as NewProfileRulesetControl).RulesetSelected)
            {
                var rulesetName = (RulesetDialog as NewProfileRulesetControl).RulesetName;
                if (rulesetName.Contains("country") || rulesetName.Contains("geoip"))
                {
                    AddRuleRuleset(rulesetName, "cn", SplitRoutingRuleDecision.Direct);
                }
                else
                {
                    AddListRuleset(rulesetName, SplitRoutingRuleDecision.Direct);
                }
            }
        }

        private void CreateCustomRuleButton_Click(object sender, RoutedEventArgs e)
        {
            CreateCustomRuleButton.Visibility = Visibility.Collapsed;
            CustomRuleTextBox.Text = "# See https://ytflow.github.io/ytflow-book/plugins/rule-dispatcher.html\r\n" +
                                     "# for quanx-filter-based custom rule syntax.\r\n\r\n" +
                                     "domain, www.example.com, direct\r\n" +
                                     "domain-suffix, ip.sb, proxy\r\n" +
                                     "domain-keyword, google-analytics, reject\r\n" +
                                     "ip-cidr, 114.114.114.114/32, direct, no-resolve\r\n" +
                                     "ip6-cidr, 2001:4860:4860::8800/120, proxy, no-resolve\r\n";
            CustomRuleTextBox.Visibility = Visibility.Visible;
        }

        private void AddListRuleset(string name, SplitRoutingRuleDecision match, SplitRoutingRuleDecision unmatch = SplitRoutingRuleDecision.Next)
        {
            var newControl = new SplitRoutingRulesetControl
            {
                RulesetName = name,
                CanModifyRuleList = false,
                RuleList = new ObservableCollection<SplitRoutingRuleModel>
            {
                new SplitRoutingRuleModel { Rule = "match", Decision = match }
            },
                FallbackRule = new SplitRoutingRuleModel { Rule = "unmatch", Decision = unmatch }
            };
            newControl.RemoveRequested += (control, _) =>
            {
                if (RulesetListView.Items.Contains(control))
                {
                    RulesetListView.Items.Remove(control);
                }
            };
            RulesetListView.Items.Add(newControl);
        }

        private void AddRuleRuleset(string name, string matchRule, SplitRoutingRuleDecision match, SplitRoutingRuleDecision unmatch = SplitRoutingRuleDecision.Next)
        {
            var newControl = new SplitRoutingRulesetControl
            {
                RulesetName = name,
                CanModifyRuleList = true,
                RuleList = new ObservableCollection<SplitRoutingRuleModel>
            {
                new SplitRoutingRuleModel { Rule = matchRule, Decision = match }
            },
                FallbackRule = new SplitRoutingRuleModel { Rule = "unmatch", Decision = unmatch }
            };
            newControl.RemoveRequested += (control, _) =>
            {
                if (RulesetListView.Items.Contains(control))
                {
                    RulesetListView.Items.Remove(control);
                }
            };
            RulesetListView.Items.Add(newControl);
        }



        void CreatePresetPlugins(uint profileId, NewProfileConfig config)
        {
            //try
            {
                var doc = NewProfilePage.GenPresetDoc();

                if (config.InboundMode == "full")
                {
                    doc["uwp-vpn-tun"]["param"]["ipv4_route"] =  new JObject { "0.0.0.0/1", "128.0.0.0/1" };
                    doc["uwp-vpn-tun"]["param"]["ipv6_route"] = new JObject { "::/1", "8000::/1" };
                }

                // Determine resolver for rules
                string ruleResolver = "phy.resolver";
                if (config.RuleResolver == "1111")
                {
                    ruleResolver = "doh-resolver.resolver";
                }
                else if (config.RuleResolver == "ali")
                {
                    doc["doh-resolver"]["param"]["doh"][0]["url"] = "https://223.5.5.5/dns-query";
                    ruleResolver = "doh-resolver.resolver";
                }

                // Compose ruleset chain
                string tcpNext = "proxy-forward.tcp", udpNext = "resolve-proxy.udp";
                uint rulesetIdx = 0;

                Func<SplitRoutingRuleDecision, JObject> makeAction = decision =>
                {
                    JObject result;

                    switch (decision)
                    {
                        case SplitRoutingRuleDecision.Proxy:
                            result = new JObject { { "tcp", "proxy-forward.tcp" }, { "udp", "resolve-proxy.udp" } };
                            break;
                        case SplitRoutingRuleDecision.Direct:
                            result = new JObject { { "tcp", "direct-forward.tcp" }, { "udp", "resolve-local.udp" } };
                            break;
                        case SplitRoutingRuleDecision.Reject:
                            result = new JObject { { "tcp", "reject.tcp" }, { "udp", "reject.udp" } };
                            break;
                        case SplitRoutingRuleDecision.Next:
                            result = new JObject { { "tcp", tcpNext }, { "udp", udpNext } };
                            break;
                        default:
                            throw new ArgumentException("Invalid decision");
                    }

                    return result;

                };

                Func<SplitRoutingRuleDecision, string> decisionName = decision =>
                {
                    string result;

                    switch (decision)
                    {
                        case SplitRoutingRuleDecision.Proxy:
                            result = "proxy";
                            break;
                        case SplitRoutingRuleDecision.Direct:
                            result = "direct";
                            break;
                        case SplitRoutingRuleDecision.Reject:
                            result = "reject";
                            break;
                        case SplitRoutingRuleDecision.Next:
                            result = "next";
                            break;
                        default:
                            throw new ArgumentException("Invalid decision");
                    }

                    return result;
                };

                Func<JObject> availableActions = () =>
                {
                    return new JObject
            {
                { "proxy", makeAction(SplitRoutingRuleDecision.Proxy) },
                { "direct", makeAction(SplitRoutingRuleDecision.Direct) },
                { "reject", makeAction(SplitRoutingRuleDecision.Reject) },
                { "next", makeAction(SplitRoutingRuleDecision.Next) }
            };
                };
                config.Rulesets.Reverse();
                foreach (var ruleset in config.Rulesets)
                {
                    var rulesetDoc = new JObject
            {
                { "desc", $"Rule dispatcher for ruleset {ruleset.Name}." },
                { "plugin", ruleset.IsList ? "list-dispatcher" : "rule-dispatcher" },
                { "plugin_version", 0 },
                { "param", new JObject
                    {
                        { "resolver", ruleResolver },
                        { "source", ruleset.Name },
                        { "fallback", makeAction(ruleset.FallbackRule) }
                    }
                }
            };

                    if (ruleset.IsList)
                    {
                        rulesetDoc["param"]["action"] = makeAction(ruleset.Rules[0].Value);
                    }
                    else
                    {
                        rulesetDoc["param"]["actions"] = availableActions();

                        JObject res = new JObject();
                        foreach( var it  in ruleset.Rules)
                        {
                            res[it.Key] = decisionName(it.Value);
                        }
                        rulesetDoc["param"]["rules"] = res;
                    }

                    string pluginName = $"ruleset-dispatcher-{++rulesetIdx}";
                    doc[pluginName] = rulesetDoc;
                    tcpNext = $"{pluginName}.tcp";
                    udpNext = $"{pluginName}.udp";
                }

                var customRuleDoc = new JObject
        {
            { "desc", "Rule dispatcher for custom rules" },
            { "plugin", "rule-dispatcher" },
            { "plugin_version", 0 },
            { "param", new JObject
                {
                    { "resolver", ruleResolver },
                    { "source", new JObject { { "format", "quanx-filter" }, { "text", new JArray(SplitLines(config.CustomRules)) } } },
                    { "actions", availableActions() },
                    { "rules", new JObject
                        {
                            { "proxy", "proxy" },
                            { "direct", "direct" },
                            { "reject", "reject" },
                            { "next", "next" }
                        }
                    },
                    { "fallback", makeAction(SplitRoutingRuleDecision.Next) }
                }
            }
        };

                var fakeipFilterDoc = new JObject
        {
            { "desc", "Generate real IP addresses instead of Fake IPs for specified domain names." },
            { "plugin", "list-dispatcher" },
            { "plugin_version", 0 },
            { "param", new JObject
                {
                    { "source", new JObject { { "format", "surge-domain-set" }, { "text", new JArray(SplitLines(config.FakeIpFilter)) } } },
                    { "action", new JObject { { "resolver", "phy.resolver" } } },
                    { "fallback", new JObject { { "resolver", "fake-ip.resolver" } } }
                }
            }
        };

                doc["custom-rule-dispatcher"] = customRuleDoc;
                doc["fakeip-filter"] = fakeipFilterDoc;

                // Adjust outbound
                if (config.OutboundType == "ss")
                {
                    doc["proxy-forward"]["param"]["tcp_next"] = "ss-client.tcp";
                    doc["proxy-forward"]["param"]["udp_next"] = "ss-client.udp";
                }
                else if (config.OutboundType == "http")
                {
                    doc["proxy-forward"]["param"]["tcp_next"] = "http-proxy-client.tcp";
                }
                else if (config.OutboundType == "trojan")
                {
                    doc["proxy-forward"]["param"]["tcp_next"] = "trojan-client.tcp";
                    doc["proxy-forward"]["param"]["udp_next"] = "trojan-client.udp";
                    doc["proxy-redir"]["param"]["tcp_next"] = "client-tls.tcp";
                    doc["client-tls"]["param"]["alpn"] = new JObject { "h2", "http/1.1" };
                }
                else if (config.OutboundType == "vmess_ws_tls")
                {
                    doc["proxy-forward"]["param"]["tcp_next"] = "vmess-client.tcp";
                    doc["proxy-redir"]["param"]["tcp_next"] = "ws-client.tcp";
                    doc["client-tls"]["param"]["alpn"] = new JObject { "http/1.1" };
                }

                var conn = BridgeExtensions.FfiDbInstance.Connect();
                foreach (var kvp in doc)
                {
                    var pluginName = kvp.Key;
                    var pluginDoc = kvp.Value;
                    var desc = pluginDoc["desc"].ToString();
                    var plugin = pluginDoc["plugin"].ToString();
                    var pluginVersion = Convert.ToUInt16(pluginDoc["plugin_version"]);
                    var paramDoc = pluginDoc["param"];

                    RawEditorParam.UnescapeCborBuf(paramDoc);
                    if (paramDoc.ToObject<JObject>() == null)
                    {
                        int a = 0;
                    }
                    var param = JObjectToCBOR(paramDoc.ToObject<JObject>());
                    var id = conn.CreatePlugin(profileId, pluginName, desc, plugin, pluginVersion, param.EncodeToBytes());

                    if (pluginName == "entry-ip-stack")
                    {
                        conn.SetPluginAsEntry(id, profileId);
                    }
                }
            }
            //catch(Exception ex)
            {
                //UI.NotifyException("Creating preset plugins",ex);
            }
        }

        public static string[] SplitLines(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new string[0];
            }

            // Split the input string by '\r' and trim trailing '\r' characters
            return input.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.TrimEnd('\r'))
                        .ToArray();
        }

        //-------------------------

        public static JObject GenIpStack()
        {
            return new JObject
            {
                ["desc"] = "Handle TCP or UDP connections from UWP VPN.",
                ["plugin"] = "ip-stack",
                ["plugin_version"] = 0,
                ["param"] = new JObject
                {
                    ["tcp_next"] = "fakeip-dns-server.tcp_map_back.udp-proxy-switch.tcp",
                    ["udp_next"] = "dns-dispatcher.udp",
                    ["tun"] = "uwp-vpn-tun.tun"
                }
            };
        }

        public static JObject GenUwpVpnTun()
        {
            return new JObject
            {
                ["desc"] = "UWP VPN Plugin TUN interface.",
                ["plugin"] = "vpn-tun",
                ["plugin_version"] = 0,
                ["param"] = new JObject
                {
                    ["dns"] = new JArray { "11.16.1.1", "260c:1::1" },
                    ["ipv4"] = "192.168.3.1",
                    ["ipv4_route"] = new JArray { "11.17.0.0/16", "11.16.0.0/16", "149.154.160.0/20", "91.108.56.130/32" },
                    ["ipv6"] = "fd00::2",
                    ["ipv6_route"] = new JArray { "::/16", "260c:2001::/96", "260c:1::/96", "2001:0b28:f23c::/46", "2001:067c:04e8::/48" },
                    ["web_proxy"] = null
                }
            };
        }

        public static JObject GenForward(string desc, string tcpNext, string udpNext = "phy.udp")
        {
            return new JObject
            {
                ["desc"] = desc,
                ["plugin"] = "forward",
                ["plugin_version"] = 0,
                ["param"] = new JObject
                {
                    ["request_timeout"] = 200,
                    ["tcp_next"] = tcpNext,
                    ["udp_next"] = udpNext
                }
            };
        }

        public static JObject GenDnsDispatcher()
        {
            return new JObject
            {
                ["desc"] = "Dispatches DNS requests to our DNS server.",
                ["plugin"] = "simple-dispatcher",
                ["plugin_version"] = 0,
                ["param"] = new JObject
                {
                    ["fallback_tcp"] = "reject.tcp",
                    ["fallback_udp"] = "fakeip-dns-server.udp_map_back.udp-proxy-switch.udp",
                    ["rules"] = new JArray
                {
                    new JObject
                    {
                        ["src"] = new JObject
                        {
                            ["ip_ranges"] = new JArray { "0.0.0.0/0", "::/0" },
                            ["port_ranges"] = new JArray
                            {
                                new JObject { ["start"] = 0, ["end"] = 65535 }
                            }
                        },
                        ["dst"] = new JObject
                        {
                            ["ip_ranges"] = new JArray { "11.16.1.1/32", "260c:1::1/128" },
                            ["port_ranges"] = new JArray
                            {
                                new JObject { ["start"] = 53, ["end"] = 53 }
                            }
                        },
                        ["is_udp"] = true,
                        ["next"] = "fakeip-dns-server.udp"
                    }
                }
                }
            };
        }

        public static JObject GenPresetDoc()
        {
            return new JObject
            {
                ["entry-ip-stack"] = GenIpStack(),
                ["uwp-vpn-tun"] = GenUwpVpnTun(),
                ["proxy-forward"] = GenForward("Forward connections to the proxy outbound.", "outbound.tcp", "outbound.udp"),
                ["direct-forward"] = GenForward("Forward connections to the physical outbound.", "phy.tcp"),
                ["outbound"] = new JObject
                {
                    ["desc"] = "Allows runtime selection of outbound proxies from the Library.",
                    ["plugin"] = "dyn-outbound",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["tcp_next"] = "phy.tcp",
                        ["udp_next"] = "phy.udp"
                    }
                },
                ["ss-client"] = new JObject
                {
                    ["desc"] = "Shadowsocks client.",
                    ["plugin"] = "shadowsocks-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["method"] = "aes-128-gcm",
                        ["password"] = new JObject { ["__byte_repr"] = "utf8", ["data"] = "my_ss_password" },
                        ["tcp_next"] = "proxy-redir.tcp",
                        ["udp_next"] = "null.udp"
                    }
                },
                ["http-proxy-client"] = new JObject
                {
                    ["desc"] = "HTTP Proxy client. Use HTTP CONNECT to connect to the proxy server.",
                    ["plugin"] = "http-proxy-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["tcp_next"] = "proxy-redir.tcp",
                        ["user"] = new JObject { ["__byte_repr"] = "utf8", ["data"] = "" },
                        ["pass"] = new JObject { ["__byte_repr"] = "utf8", ["data"] = "" }
                    }
                },
                ["trojan-client"] = new JObject
                {
                    ["desc"] = "Trojan client. The TLS part is in plugin client-tls.",
                    ["plugin"] = "trojan-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["password"] = new JObject { ["__byte_repr"] = "utf8", ["data"] = "my_trojan_password" },
                        ["tls_next"] = "proxy-redir.tcp"
                    }
                },
                ["client-tls"] = new JObject
                {
                    ["desc"] = "TLS client stream for proxy client.",
                    ["plugin"] = "tls-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["next"] = "phy.tcp",
                        ["skip_cert_check"] = false,
                        ["sni"] = "my.proxy.server.com"
                    }
                },
                ["vmess-client"] = new JObject
                {
                    ["desc"] = "VMess client.",
                    ["plugin"] = "vmess-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["user_id"] = "b831381d-6324-4d53-ad4f-8cda48b30811",
                        ["security"] = "auto",
                        ["alter_id"] = 0,
                        ["tcp_next"] = "proxy-redir.tcp"
                    }
                },
                ["ws-client"] = new JObject
                {
                    ["desc"] = "WebSocket client.",
                    ["plugin"] = "ws-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["host"] = "dl.microsoft.com",
                        ["path"] = "/",
                        ["headers"] = new JObject(),
                        ["next"] = "client-tls.tcp"
                    }
                },
                ["proxy-redir"] = new JObject
                {
                    ["desc"] = "Change the destination to the proxy server.",
                    ["plugin"] = "redirect",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["dest"] = new JObject { ["host"] = "my.proxy.server.com.", ["port"] = 8388 },
                        ["tcp_next"] = "phy.tcp",
                        ["udp_next"] = "phy.udp"
                    }
                },
                ["phy"] = new JObject
                {
                    ["desc"] = "The physical NIC.",
                    ["plugin"] = "netif",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["family_preference"] = "Both",
                        ["type"] = "Auto"
                    }
                },
                ["null"] = new JObject
                {
                    ["desc"] = "Return an error for any incoming requests.",
                    ["plugin"] = "null",
                    ["plugin_version"] = 0,
                    ["param"] = null
                },
                ["reject"] = new JObject
                {
                    ["desc"] = "Silently drop any outgoing requests.",
                    ["plugin"] = "reject",
                    ["plugin_version"] = 0,
                    ["param"] = null
                },
                ["fake-ip"] = new JObject
                {
                    ["desc"] = "Assign a fake IP address for each domain name. This is useful for TUN inbounds where incoming connections carry no information about domain names. By using a Fake IP resolver, destination IP addresses can be mapped back to a domain name that the client is connecting to.",
                    ["plugin"] = "fake-ip",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["fallback"] = "null.resolver",
                        ["prefix_v4"] = new JArray { 11, 17 },
                        ["prefix_v6"] = new JArray { 38, 12, 32, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }
                    }
                },
                ["doh-resolver"] = new JObject
                {
                    ["desc"] = "Resolves real IP addresses from a secure, trusted provider.",
                    ["plugin"] = "host-resolver",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["udp"] = new JArray(),
                        ["tcp"] = new JArray(),
                        ["doh"] = new JArray
                    {
                        new JObject
                        {
                            ["url"] = "https://1.1.1.1/dns-query",
                            ["next"] = "general-tls.tcp"
                        }
                    }
                    }
                },
                ["dns-dispatcher"] = GenDnsDispatcher(),
                ["rule-switch"] = new JObject
                {
                    ["desc"] = "Decide whether rules should take effect at runtime.",
                    ["plugin"] = "switch",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["choices"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = "Rule",
                            ["description"] = "Match connections against rules",
                            ["tcp_next"] = "custom-rule-dispatcher.tcp",
                            ["udp_next"] = "custom-rule-dispatcher.udp"
                        },
                        new JObject
                        {
                            ["name"] = "Proxy",
                            ["description"] = "Proxy all connections unconditionally",
                            ["tcp_next"] = "proxy-forward.tcp",
                            ["udp_next"] = "resolve-proxy.udp"
                        },
                        new JObject
                        {
                            ["name"] = "Direct",
                            ["description"] = "Connections will not go through any proxies",
                            ["tcp_next"] = "direct-forward.tcp",
                            ["udp_next"] = "resolve-local.udp"
                        }
                    }
                    }
                },
                ["udp-proxy-switch"] = new JObject
                {
                    ["desc"] = "Decide whether UDP packets should go through proxies.",
                    ["plugin"] = "switch",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["choices"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = "On",
                            ["description"] = "UDP packets will go through the same routing decisions as TCP connections, possibly via a proxy",
                            ["tcp_next"] = "rule-switch.tcp",
                            ["udp_next"] = "rule-switch.udp"
                        },
                        new JObject
                        {
                            ["name"] = "Off",
                            ["description"] = "UDP packets will not go through any rules or proxies",
                            ["tcp_next"] = "rule-switch.tcp",
                            ["udp_next"] = "resolve-local.udp"
                        }
                    }
                    }
                },
                ["general-tls"] = new JObject
                {
                    ["desc"] = "TLS client stream for h2, DoH etc.",
                    ["plugin"] = "tls-client",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["next"] = "phy.tcp",
                        ["skip_cert_check"] = false
                    }
                },
                ["fakeip-dns-server"] = new JObject
                {
                    ["desc"] = "Respond to DNS request messages using results from the FakeIP resolver.",
                    ["plugin"] = "dns-server",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["concurrency_limit"] = 64,
                        ["resolver"] = "fakeip-filter.resolver",
                        ["tcp_map_back"] = new JArray() { "udp-proxy-switch.tcp" },
                        ["udp_map_back"] = new JArray() { "udp-proxy-switch.udp" },
                        ["ttl"] = 60
                    }
                },
                ["resolve-local"] = new JObject
                {
                    ["desc"] = "Resolve domain names to IP addresses for direct connections and establish a mapping to preserve the original destination address.",
                    ["plugin"] = "resolve-dest",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["resolver"] = "phy.resolver",
                        ["tcp_next"] = "direct-forward.tcp",
                        ["udp_next"] = "direct-forward.udp"
                    }
                },
                ["resolve-proxy"] = new JObject
                {
                    ["desc"] = "Resolve domain names to IP addresses for proxied connections through proxy outbound and establish a mapping to preserve the original destination address.",
                    ["plugin"] = "resolve-dest",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["resolver"] = "proxy-resolver.resolver",
                        ["tcp_next"] = "proxy-forward.tcp",
                        ["udp_next"] = "proxy-forward.udp"
                    }
                },
                ["proxy-resolver"] = new JObject
                {
                    ["desc"] = "Resolve real IP addresses via proxy outbound.",
                    ["plugin"] = "host-resolver",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["tcp"] = new JArray(),
                        ["udp"] = new JArray { "proxy-redir-8888.udp" }
                    }
                },
                ["proxy-redir-8888"] = new JObject
                {
                    ["desc"] = "Rewrite destination address to 8.8.8.8.",
                    ["plugin"] = "redirect",
                    ["plugin_version"] = 0,
                    ["param"] = new JObject
                    {
                        ["dest"] = new JObject { ["host"] = "8.8.8.8", ["port"] = 53 },
                        ["tcp_next"] = "outbound.tcp",
                        ["udp_next"] = "outbound.udp"
                    }
                }
            };
        }


        public static CBORObject JObjectToCBOR(JObject jObject)
        {
            var cborObject = CBORObject.NewMap();
            if (jObject == null) return cborObject;
            Func< JToken, CBORObject> CastCBORObject = ( obj) =>
            {
                if (obj.Type == JTokenType.Null)
                {
                    return CBORObject.Null;
                }
                switch (obj.Type)
                {
                    case JTokenType.String:
                        return CBORObject.FromObject(obj.ToString());
                        break;
                    case JTokenType.Boolean:
                        return CBORObject.FromObject(obj.ToObject<bool>());
                        break;
                    case JTokenType.Integer:
                        return CBORObject.FromObject(obj.ToObject<int>());
                        break;
                    case JTokenType.Float:
                        return CBORObject.FromObject(obj.ToObject<double>());
                        break;

                    default:
                       return CBORObject.FromObject(obj);
                }
            };

            foreach (var property in jObject.Properties())
            {
                // Convert the property value based on its type
                if (property.Value.Type == JTokenType.Object)
                {
                    cborObject.Add(property.Name, JObjectToCBOR((JObject)property.Value)); // Recursive call for nested objects
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    var cborArray = CBORObject.NewArray();
                    foreach (var item in (JArray)property.Value)
                    {
                        if (item.Type == JTokenType.Object)
                            cborArray.Add(JObjectToCBOR(item.ToObject<JObject>())); // Recursive call for nested objects
                        else
                        {
                            cborArray.Add(CastCBORObject(item));
                        }
                    }
                    cborObject.Add(property.Name, cborArray);
                }
                else
                {
                    cborObject.Add(property.Name, CastCBORObject(property.Value));

                   
                }
            }

            return cborObject;
        }

    }
}

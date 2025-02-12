using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using YtFlowApp2.Models;
using YtFlowApp2.Pages;
using YtFlowApp2.Utils;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace YtFlowApp2.Controls.HomeWidget
{
    public class DynOutboundInfo
    {
        [JsonProperty("current_proxy_name")]
        public string current_proxy_name { get; set; }
        [JsonProperty("current_proxy_idx")]
        public uint CurrentProxyIdx { get; set; }
    }

    public class DynOutboundProxy
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("idx")]
        public uint Idx { get; set; }

        [JsonProperty("id")]
        public uint Id { get; set; }

        [JsonProperty("group_id")]
        public uint GroupId { get; set; }

        [JsonProperty("group_name")]
        public string GroupName { get; set; }
    }

    public class DynOutboundListProxiesRes
    {
        [JsonProperty("proxies")]
        public List<DynOutboundProxy> Proxies { get; set; }
    }

    public sealed partial class DynOutboundHomeWidget : UserControl, IHomeWidget
    {
        private string _pluginName;
        private WidgetHandle _sharedInfo;
        private Func<string, List<byte>, Task<List<byte>>> _sendRequest;

        public DynOutboundHomeWidget()
        {
            this.InitializeComponent();
        }

        public DynOutboundHomeWidget(string pluginName, WidgetHandle sharedInfo, Func<string, List<byte>, Task<List<byte>>> sendRequest)
        {
            _pluginName = pluginName;
            _sharedInfo = sharedInfo;
            _sendRequest = sendRequest;
            this.InitializeComponent();
        }

        public void UpdateInfo()
        {
            string proxyName = string.Empty;
            try
            {
             
                var info =CoreInterop.BridgeExtensions.FromCBORBytes<DynOutboundInfo>(_sharedInfo.Info.ToArray());
                
                proxyName = info.current_proxy_name;
            }
            catch (JsonException)
            {
                // Handle JSON parsing errors
            }

            if (ProxyNameText.Text != proxyName)
            {
                PreviewProxyNameText.Text = proxyName;
                ProxyNameText.Text = proxyName;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PluginNameText.Text = _pluginName;
        }

        private async void SelectProxyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectProxyButton.IsEnabled = false;
                // 使用 System.Text.Json 创建一个表示 null 的 JSON 对象
                var jsonObject = JsonConvert.SerializeObject(null);

                // 将 JSON 字符串转换为 CBOR 对象
                var cborObject = CBORObject.FromJSONString(jsonObject);

                // 将 CBOR 对象编码为字节数组
                byte[] cborData = cborObject.EncodeToBytes();

                var res = await _sendRequest("list_proxies", BridgeExtensions.ToCBORBytes(null).ToList());

                string jsonString = BridgeExtensions.FromCBORBytesToJson(res.ToArray());

                var proxies = JsonConvert.DeserializeObject<DynOutboundListProxiesRes>(jsonString).Proxies;

                var models = proxies.Select(p => new DynOutboundProxyModel(p.Idx, p.Name, p.GroupName)).ToList();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ProxyItemGridView.ItemsSource = null;
                    ProxyItemGridView.ItemsSource = new ObservableCollection<DynOutboundProxyModel>(models);
                    VisualStateManager.GoToState(this, "DisplayProxySelectionView", true);
                });
            }
            catch (Exception ex)
            {
                UI.NotifyException("Loading proxies for selection", ex);
            }
            finally
            {
                SelectProxyButton.IsEnabled = true;
            }
        }

        private void ProxySelectionBackButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "DisplayInfoView", true);
            SelectProxyButton.IsEnabled = true;
        }

        private async void ProxyItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var proxyIdx = (sender as FrameworkElement).DataContext as DynOutboundProxyModel;
                var res = await _sendRequest("select", BridgeExtensions.ToCBORBytes(proxyIdx.Idx).ToList());
                var json = BridgeExtensions.FromCBORBytes(res.ToArray());
                
                if (!json.IsNull)
                {
                    UI.NotifyUser(json.ToString(), "Failed to select proxy");
                    return;
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    VisualStateManager.GoToState(this, "DisplayInfoView", true);
                    SelectProxyButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                UI.NotifyException("Selecting proxy", ex);
            }
        }
    }
}

using Newtonsoft.Json;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Vpn;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.Models;
using YtFlowApp2.Pages;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace YtFlowApp2.Controls.HomeWidget
{
    public sealed partial class SwitchHomeWidget : UserControl, IHomeWidget
    {
        private struct SwitchChoice
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }
        }

        private struct SwitchInfo
        {
            [JsonProperty("choices")]
            public List<SwitchChoice> Choices { get; set; }

            [JsonProperty("current")]
            public uint Current { get; set; }
        }

        private WidgetHandle _sharedInfo;
        private Func<string, List<byte>, Task<List<byte>>> _sendRequest;

        public SwitchHomeWidget()
        {
            this.InitializeComponent();
        }
  

        public SwitchHomeWidget(string pluginName, WidgetHandle sharedInfo, Func<string, List<byte>, Task<List<byte>>> sendRequest)
        {
            _sharedInfo = sharedInfo;
            _sendRequest = sendRequest;
            this.InitializeComponent();

            PluginNameText.Text = pluginName;
        }

        public void UpdateInfo()
        {
            try
            {
                var info = CoreInterop.BridgeExtensions.FromCBORBytes<SwitchInfo>(_sharedInfo.Info.ToArray());

                if (info.Current >= info.Choices.Count)
                {
                    return;
                }

                PreviewSelectionNameText.Text = info.Choices[(int)info.Current].Name;

                var items = SwitchList.Items;
                items.Clear();
                foreach (var choice in info.Choices)
                {
                    var item = new SwitchChoiceItem
                    {
                        Name = choice.Name,
                        Description = choice.Description,
                        IsActive = info.Current-- == 0
                    };
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                NotifyException("Updating Switch", ex);
            }
        }

        private async void ChoiceToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var itemObj = (sender as FrameworkElement)?.DataContext as SwitchChoiceItem;
                if (itemObj == null || itemObj.IsActive)
                {
                    return;
                }

                var idx = SwitchList.Items.IndexOf(itemObj);
                if (idx == -1)
                {
                    return;
                }

                foreach (var otherObj in SwitchList.Items)
                {
                    var otherItem = otherObj as SwitchChoiceItem;
                    if (otherItem.IsActive)
                    {
                        otherItem.IsActive = false;
                        break;
                    }
                }
                itemObj.IsActive = true;


                await _sendRequest("s", PeterO.Cbor.CBORObject.FromObject(idx).EncodeToBytes().ToList());
            }
            catch (Exception ex)
            {
                NotifyException("Switch", ex);
            }
        }
        
        private void ChoiceToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            var item = btn?.DataContext as SwitchChoiceItem;
            if (item?.IsActive == true)
            {
                btn.IsChecked = true;
            }
        }

        private void NotifyException(string message, Exception ex)
        {
            // Implement exception notification logic here
        }
    }
}

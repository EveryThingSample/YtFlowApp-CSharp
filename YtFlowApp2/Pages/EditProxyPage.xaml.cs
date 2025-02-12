using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using YtFlowApp2.Classes;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Models;
using YtFlowApp2.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YtFlowApp2.Pages
{


    public sealed partial class EditProxyPage : Page
    {
        public static readonly DependencyProperty IsDirtyProperty =
            DependencyProperty.Register("IsDirty", typeof(bool), typeof(EditProxyPage), new PropertyMetadata(false));

        private ProxyModel m_proxyModel;
        private string m_proxyName;
        private ObservableCollection<ProxyLegModel> m_proxyLegs;
        private bool m_isUdpSupported;
        private bool m_isReadonly;
        private bool m_forceQuit;

        public EditProxyPage()
        {
            this.InitializeComponent();
        }


        public static Visibility IsShadowsocks(string protocolType)
        {
            return protocolType == "Shadowsocks" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsTrojan(string protocolType)
        {
            return protocolType == "Trojan" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsHttp(string protocolType)
        {
            return protocolType == "HTTP" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsSocks5(string protocolType)
        {
            return protocolType == "SOCKS5" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsVMess(string protocolType)
        {
            return protocolType == "VMess" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsHttpObfs(string obfsType)
        {
            return obfsType == "simple-obfs (HTTP)" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsTlsObfs(string obfsType)
        {
            return obfsType == "simple-obfs (TLS)" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility IsWebSocket(string obfsType)
        {
            return obfsType == "WebSocket" ? Visibility.Visible : Visibility.Collapsed;
        }

        public static bool TlsParamsAllowed(bool enableTls, bool isReadonly)
        {
            return enableTls && !isReadonly;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            VisualStateManager.GoToState(this, "MasterState", false);
            bool analyzed = true;
            if (e.Parameter is EditProxyPageParam param)
            {
                m_proxyModel = param.Proxy;
                analyzed = PropagateParamModel(param);
            }
            if (!analyzed)
            {
               UI.NotifyUser("The proxy is too complex to be parsed. While a dyn-outbound plugin may pick up and load this proxy at runtime, it is not possible to edit it. Any changes saved will overwrite the existing proxy configuration.", "Proxy too complex");
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                var currState = AdaptiveWidthVisualStateGroup.CurrentState;
                if (currState != null && currState.Name == "DetailState")
                {
                    VisualStateManager.GoToState(this, "MasterState", true);
                    LegList.SelectedIndex = -1;
                    e.Cancel = true;
                    return;
                }

                if (m_forceQuit)
                {
                    return;
                }

                if (!IsDirty)
                {
                    return;
                }

                e.Cancel = true;
                if (await QuitWithUnsavedDialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }

                m_forceQuit = true;
                switch (e.NavigationMode)
                {
                    case NavigationMode.Back:
                        Frame.GoBack();
                        break;
                    case NavigationMode.Forward:
                        Frame.GoForward();
                        break;
                    default:
                        Frame.Navigate(e.SourcePageType, e.Parameter, e.NavigationTransitionInfo);
                        break;
                }
            }
            catch (Exception ex)
            {
               UI.NotifyException("EditProxyPage OnNavigatingFrom", ex);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            m_proxyModel = null;
            m_proxyLegs = null;
        }

        private void AdaptiveWidth_StateChanged(object sender, VisualStateChangedEventArgs e)
        {
            try
            {
                var newState = e.NewState;
                if (newState == null || newState == MediumWidthState)
                {
                    return;
                }

                if (LegList.SelectedIndex == -1)
                {
                    VisualStateManager.GoToState(this, "MasterState", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "DetailState", true);
                }
            }
            catch (Exception ex)
            {
               UI.NotifyException("EditProxyPage AdaptiveWidth StateChange", ex);
            }
        }

        public string ProxyName
        {
            get => m_proxyName;
            set
            {
                if (m_proxyName != value)
                {
                    m_proxyName = value;
                    IsDirty = true;
                }
            }
        }

        public ObservableCollection<ProxyLegModel> ProxyLegs
        {
            get => m_proxyLegs;
            private set => m_proxyLegs = value;
        }

        public bool IsUdpSupported
        {
            get => m_isUdpSupported;
            set
            {
                if (m_isUdpSupported != value)
                {
                    m_isUdpSupported = value;
                    IsDirty = true;
                }
            }
        }

        public bool IsReadonly => m_isReadonly;

        public bool IsWritable => !m_isReadonly;

        public bool IsDirty
        {
            get => (bool)GetValue(IsDirtyProperty);
            set
            {
                if (IsDirty != value)
                {
                    SetValue(IsDirtyProperty, value);
                }
            }
        }

        private bool PropagateParamModel(EditProxyPageParam param)
        {
            m_isReadonly = param.IsReadonly;
            var analyzeResult = param.Proxy.Analyze();
            var dummyProxy = new FfiProxy { name = "", legs = new List<FfiProxyLeg>(), udp_supported = false };
            var analyzed = analyzeResult != null ? analyzeResult : dummyProxy;

            if (analyzeResult == null)
            {
                dummyProxy.name = param.Proxy.Name;
                dummyProxy.legs.Add(new FfiProxyLeg());
            }

            m_proxyName = analyzed.name;
            m_isUdpSupported = analyzed.udp_supported;


            var weak = new WeakReference(this);
            m_proxyLegs = new ObservableCollection<ProxyLegModel>(
                analyzed.legs.Select(leg =>
                {
                    var model = new ProxyLegModel(m_isReadonly, leg);
                    model.PropertyChanged += (sender, args) =>
                    {
                        if (weak.Target is EditProxyPage self)
                        {
                            self.IsDirty = true;
                        }
                    };
                    return model;
                }).ToList()
            );

            m_proxyLegs.CollectionChanged += (sender, args) =>
            {
                if (weak.Target is EditProxyPage self)
                {
                    self.IsDirty = true;
                }
            };
            IsDirty = false;

            this.Bindings.Update();
            LegList.SelectedIndex = 0;
        
            return analyzeResult != null;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_proxyModel == null)
                {
                    return;
                }

                foreach (var proxyLegModel in m_proxyLegs)
                {
                    if (string.IsNullOrEmpty(proxyLegModel.Host))
                    {
                        LegList.SelectedItem = proxyLegModel;
                       UI.NotifyUser("Host cannot be empty.", "Invalid Proxy");
                        return;
                    }

                    if (proxyLegModel.ProtocolType == "VMess")
                    {
                        if (!Guid.TryParse(proxyLegModel.Password, out _))
                        {
                            LegList.SelectedItem = proxyLegModel;
                           UI.NotifyUser("Invalid VMess UUID.", "Invalid Proxy");
                            return;
                        }
                    }
                }

                var proxy = new FfiProxy
                {
                    name = m_proxyName,
                    legs = m_proxyLegs.Select(leg => leg.Encode()).ToList(),
                    udp_supported = m_isUdpSupported
                };

                var proxyBuf = proxy.ToCbor().EncodeToBytes();
                var proxyDataBuf = BridgeExtensions.ytflow_app_proxy_data_proxy_compose_v1(proxyBuf);
                m_proxyModel.Name = m_proxyName;
                m_proxyModel.Proxy = proxyDataBuf;
                m_proxyModel.ProxyVersion = 0;

                await Task.Run(() => m_proxyModel.Update());
                IsDirty = false;
            }
            catch (Exception ex)
            {
               UI.NotifyException("EditProxyPage Save", ex);
            }
        }

        private void LegList_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                var clickedItem = e.ClickedItem as ProxyLegModel;
                if (clickedItem == null)
                {
                    return;
                }

                var clickedIndex = m_proxyLegs.IndexOf(clickedItem);
                if (clickedIndex == -1)
                {
                    return;
                }

                LegList.SelectedIndex = clickedIndex;
                VisualStateManager.GoToState(this, "DetailState", true);
            }
            catch (Exception ex)
            {
               UI.NotifyException("EditProxyPage LegList_ItemClick", ex);
            }
        }

        private void LegItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (m_proxyLegs.Count == 1)
            {
               UI.NotifyUser("Cannot delete the last proxy leg.", "Invalid Proxy");
                return;
            }

            var legModel = (sender as FrameworkElement).DataContext as ProxyLegModel;
            if (legModel == null)
            {
                return;
            }

            var index = m_proxyLegs.IndexOf(legModel);
            if (index == -1)
            {
                return;
            }

            m_proxyLegs.RemoveAt(index);
            IsDirty = true;
        }

        private void ChainBeforeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = LegList.SelectedIndex;
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
            }

            m_proxyLegs.Insert(selectedIndex, new ProxyLegModel());
            IsDirty = true;
        }

        private void ChainAfterButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = LegList.SelectedIndex;
            if (selectedIndex == -1)
            {
                selectedIndex = LegList.Items.Count - 1;
            }

            m_proxyLegs.Insert(selectedIndex + 1, new ProxyLegModel());
            IsDirty = true;
        }

        private void SniAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            sender.Items.Clear();
            var text = sender.Text;
            if ("auto".StartsWith(text) && text != "auto")
            {
                sender.Items.Add("auto");
            }
        }

        private void AlpnAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            sender.Items.Clear();
            var text = sender.Text;
            if ("auto".StartsWith(text) && text != "auto")
            {
                sender.Items.Add("auto");
            }
            if ("http/1.1".StartsWith(text) && text != "http/1.1")
            {
                sender.Items.Add("http/1.1");
            }
        }

    }
    public sealed class EditProxyPageParam : INotifyPropertyChanged
    {
        public EditProxyPageParam() { }

        public EditProxyPageParam(bool isReadonly, ProxyModel proxy)
        {
            IsReadonly = isReadonly;
            Proxy = proxy;
        }

        private bool _isReadonly;
        public bool IsReadonly
        {
            get => _isReadonly;
            set
            {
                if (_isReadonly != value)
                {
                    _isReadonly = value;
                    OnPropertyChanged(nameof(IsReadonly));
                }
            }
        }

        private ProxyModel _proxy;
        public ProxyModel Proxy
        {
            get => _proxy;
            set
            {
                if (_proxy != value)
                {
                    _proxy = value;
                    OnPropertyChanged(nameof(Proxy));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

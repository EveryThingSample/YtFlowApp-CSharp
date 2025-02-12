using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Vpn;
using Windows.System;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.Pages;
using YtFlowApp2.States;
using YtFlowApp2.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace YtFlowApp2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer _timer;
        public MainPage()
        {
            this.InitializeComponent();
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            Window.Current.SetTitleBar(AppTitleBar);

            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
            Window.Current.Activated += Current_Activated;
            UI.NotifyUser("", "", this.Dispatcher);
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += (sender, args) =>
            {
                if (ConnectionState.Instance != null)
                {
                    _timer.Stop();
                    ConnectionState.Instance.ConnectStatusChanged += OnConnectStatusChanged;
                }
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                ContentFrame.Margin = new Thickness(0, -80, 0, 0);
            }
        }

        private void NavigationViewControl_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            if (sender.PaneDisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top)
            {
                coreTitleBar.ExtendViewIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
            else if (sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
            {
                coreTitleBar.ExtendViewIntoTitleBar = false;
                titleBar.ButtonBackgroundColor = default;
                titleBar.ButtonInactiveBackgroundColor = default;
            }
            else
            {
                coreTitleBar.ExtendViewIntoTitleBar = false;
                titleBar.ButtonBackgroundColor = default;
                titleBar.ButtonInactiveBackgroundColor = default;
            }
        }
        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (sender.IsVisible)
            {
                AppTitleBar.Visibility = Visibility.Visible;
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            var defaultForegroundBrush = Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
            var inactiveForegroundBrush = Application.Current.Resources["TextFillColorDisabledBrush"] as SolidColorBrush;

            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                AppTitle.Foreground = inactiveForegroundBrush;
            }
            else
            {
                AppTitle.Foreground = defaultForegroundBrush;
            }
        }

        private void NavigationViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += CoreDispatcher_AcceleratorKeyActivated;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;

            SubscribeConnectionChanges();
        }

        private void NavigationViewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeConnectionChanges();
        }

        private void NavView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                string navItemTag = args.SelectedItemContainer.Tag?.ToString() ?? "";
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }
        private static readonly List<KeyValuePair<string, Type>> m_pages = new List<KeyValuePair<string, Type>>()
    {
        new KeyValuePair<string, Type>("home", typeof(HomePage)),
        new KeyValuePair<string, Type>("library", typeof(LibraryPage)),
        new KeyValuePair<string, Type>("about", typeof(AboutPage))
    };
        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            Type pageType = null;
            foreach (var page in m_pages)
            {
                if (page.Key == navItemTag)
                {
                    pageType = page.Value;
                    break;
                }
            }

            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            if (preNavPageType == typeof(FirstTimePage))
            {
                return;
            }

            if (pageType != null && preNavPageType != pageType)
            {
                ContentFrame.Navigate(pageType, null, transitionInfo);
            }
        }

        private void NavView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }


        //Input Handling:
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown &&
                args.VirtualKey == VirtualKey.Left && args.KeyStatus.IsMenuKeyDown && !args.Handled)
            {
                args.Handled = TryGoBack();
            }
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                args.Handled = TryGoBack();
            }
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs args)
        {
            if (!args.Handled)
            {
                args.Handled = TryGoBack();
            }
        }

        //Navigation Logic
        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            if (NavigationViewControl.IsPaneOpen &&
                (NavigationViewControl.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Compact ||
                 NavigationViewControl.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal))
                return false;

            Type preNavPageType = ContentFrame.CurrentSourcePageType;
            if (preNavPageType == typeof(FirstTimePage))
            {
                return false;
            }

            ContentFrame.GoBack();
            return true;
        }

        //Connection Status Changes:
        private void SubscribeConnectionChanges()
        {

            // 启动定时器
            _timer.Start();
        }

        private void UnsubscribeConnectionChanges()
        {
            ConnectionState.Instance.ConnectStatusChanged -= OnConnectStatusChanged;
        }

        private void OnConnectStatusChanged(VpnManagementConnectionStatus status)
        {
            TextBlock content = null;

            // 更新UI，根据状态更新占位内容
            switch (status)
            {
                case VpnManagementConnectionStatus.Disconnected:
                    var ha = AppConnectedStatePlaceholder.Dispatcher.HasThreadAccess;
                    AppConnectedStatePlaceholder.Content = null;
                    break;
                case VpnManagementConnectionStatus.Connected:
                    content = new TextBlock
                    {
                        Style = AppConnectedStateStyle
                    };
                    AppConnectedStatePlaceholder.Content = content;
                    break;
                default:
                    break;
            }
        }






        //Content Frame Navigation:
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            string sourcePageName = e.SourcePageType.Name;
            string pageTag = m_pages.FirstOrDefault(p => p.Value == e.SourcePageType).Key;

            if (string.IsNullOrEmpty(pageTag))
            {
                return;
            }

            foreach (var menuObj in NavigationViewControl.MenuItems)
            {
                var menuItem = menuObj as NavigationViewItem;
                if (menuItem != null && menuItem.Tag.ToString() == pageTag)
                {
                    NavigationViewControl.SelectedItem = menuObj;
                    break;
                }
            }
        }

    }
}

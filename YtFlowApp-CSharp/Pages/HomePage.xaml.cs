using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Vpn;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using YtFlowApp2.Controls;
using YtFlowApp2.Controls.HomeWidget;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Models;
using YtFlowApp2.States;
using YtFlowApp2.Utils;
using YtFlowAppBridge.Bridge;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YtFlowApp2.Pages
{

    public interface IHomeWidget
    {
        void UpdateInfo();
    }
    public class WidgetHandle
    {
        public IHomeWidget Widget { get; set; }
        public byte[] Info { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {


        private delegate Task<List<byte>> RequestSender(string func, List<byte> param);


        public static DependencyProperty ProfilesProperty { get; } = DependencyProperty.Register(nameof(Profiles), typeof(ObservableCollection<ProfileModel>), typeof(HomePage), new PropertyMetadata(null));
        public ObservableCollection<ProfileModel> Profiles { get => (ObservableCollection<ProfileModel>)GetValue(ProfilesProperty); set { SetValue(ProfilesProperty, value); } }

        private Dictionary<uint, WidgetHandle> _widgets = new Dictionary<uint, WidgetHandle>();
        private Task _vpnTask;
        private YtFlowApp2.CoreInterop.CoreRpc _rpc;
        private bool _isDialogShown;
        public static FfiDb FfiDbInstance => FfiDb.Current;
        private DispatcherTimer _refreshTimer;

        public HomePage()
        {
            this.InitializeComponent();
            Profiles = new ObservableCollection<ProfileModel>();
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // 1 second interval
            };
            _refreshTimer.Tick += OnRefreshTimerTick;

            _refreshTimer.Stop();
        }
        Task loadDbTask = null;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (loadDbTask == null)
                {
                    loadDbTask = EnsureDatabase();
                }
                if (ConnectionState.Instance == null)
                {
                    if (null == await ConnectionState.InitializeAsync())
                    {
                        await Task.Delay(400);
                        Frame.Navigate(typeof(FirstTimePage));
                        return;
                    }

                }
                if (null == ConnectionState.Instance)
                {
                    // Ensure profile exists
                    var profile = await ConnectionState.GetInstalledVpnProfile();
                    if (profile == null)
                    {
                        await Task.Delay(400);

                        Frame.Navigate(typeof(FirstTimePage));

                        return;
                    }
                    else
                    {
                        ConnectionState.Instance = new ConnectionState(profile);
                    }
                }

                if (loadDbTask.Status != TaskStatus.RanToCompletion)
                {
                    await loadDbTask;
                }


                var conn = FfiDbInstance.Connect();
                var profiles = BridgeExtensions.GetProfiles(conn);

                var profileModels = profiles.Select(p => new ProfileModel(p)).ToList();
                if (profileModels.Count == 0 && Frame.CurrentSourcePageType == typeof(HomePage))
                {
                    Frame.Navigate(typeof(NewProfilePage), true);
                    return;
                }

                Profiles = new ObservableCollection<ProfileModel>(profileModels);
                Instance_ConnectStatusChanged(ConnectionState.Instance.ConnectStatus);
                ConnectionState.Instance.ConnectStatusChanged += Instance_ConnectStatusChanged;

                Application.Current.EnteredBackground -= Current_EnteredBackground;
                Application.Current.LeavingBackground -= Current_LeavingBackground;
            }
            catch (Exception ex)
            {
                NotifyException("HomePage NavigatedTo", ex);
            }
        }



        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            PluginWidgetPanel.Children.Clear();
            ConnectedViewSidePanel.Children.Clear();
            _refreshTimer.Stop();
            _widgets.Clear();
            Application.Current.EnteredBackground -= Current_EnteredBackground;
            Application.Current.LeavingBackground -= Current_LeavingBackground;
            if (_rpc != null)
            {
                _rpc.Dispose();
                _rpc = null;
            }
        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            if (vpnManagementConnectionStatus == VpnManagementConnectionStatus.Connected)
                _refreshTimer.Start();
        }

        private VpnManagementConnectionStatus vpnManagementConnectionStatus;


        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            _refreshTimer.Stop();
        }

        public static async Task EnsureDatabase()
        {
            if (FfiDbInstance.IsOpened)
            {
                return;
            }

            var localFolder = ApplicationData.Current.LocalFolder;
            var dbFolder = await localFolder.CreateFolderAsync("db", CreationCollisionOption.OpenIfExists);
            string dbPath = dbFolder.Path + "\\main.db";
            ApplicationData.Current.LocalSettings.Values["YTFLOW_DB_PATH"] = dbPath;

            FfiDbInstance.Open(dbPath);
        }
        private void Instance_ConnectStatusChanged(VpnManagementConnectionStatus state)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                try
                {

                    var localSettings = ApplicationData.Current.LocalSettings.Values;
                    var coreError = localSettings["YTFLOW_CORE_ERROR_LOAD"] as string;
                    if (coreError != null)
                    {
                        localSettings.Remove("YTFLOW_CORE_ERROR_LOAD");
                        NotifyUser($"YtFlow Core failed to start. {coreError}", "Core Error");
                    }
                    if (state != VpnManagementConnectionStatus.Connected)
                    {
                        _refreshTimer.Stop();
                    }

                    switch (state)
                    {
                        case VpnManagementConnectionStatus.Disconnected:
                            _refreshTimer.Stop();
                            if (_rpc != null)
                            {
                                _rpc.Dispose();
                                _rpc = null;
                            }
                            VisualStateManager.GoToState(this, "Disconnected", true);
                            PluginWidgetPanel.Children.Clear();
                            ConnectedViewSidePanel.Children.Clear();
                            break;
                        case VpnManagementConnectionStatus.Disconnecting:
                            VisualStateManager.GoToState(this, "Disconnecting", true);
                            break;
                        case VpnManagementConnectionStatus.Connected:
                            SubscribeRefreshPluginStatus();
                            CurrentProfileNameRun.Text = localSettings["YTFLOW_PROFILE_ID"] is uint id
                                ? Profiles.FirstOrDefault(p => p.Id == id)?.Name
                                : string.Empty;
                            VisualStateManager.GoToState(this, "Connected", true);
                            break;
                        case VpnManagementConnectionStatus.Connecting:
                            VisualStateManager.GoToState(this, "Connecting", true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    NotifyException("HomePage ConnectStatusChange subscribe", ex);
                }

            });

        }
        private Func<string, List<byte>, Task<List<byte>>> MakeRequestSender(uint pluginId)
        {
            return async (func, param) =>
            {
                var ret = await _rpc.SendRequestToPluginAsync(pluginId, func, param.ToArray());
                return ret.ToList();
            };
        }



        private void OnConnectRequested(object sender, HomeProfileControl control)
        {
            try
            {
                if (_vpnTask != null)
                {
                    _vpnTask.Dispose();
                }
                _vpnTask = ConnectToProfile(control.Profile.Id);
            }
            catch (Exception ex)
            {
                NotifyException("Connect request", ex);
            }
        }

        private void OnEditRequested(object sender, HomeProfileControl control)
        {
            Frame.Navigate(typeof(EditProfilePage), control.Profile, new DrillInNavigationTransitionInfo());
        }

        private async void OnExportRequested(object sender, HomeProfileControl control)
        {
            try
            {
                var profile = control.Profile;
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    SuggestedFileName = profile.Name + ".ytp"
                };
                picker.FileTypeChoices.Add("YtFlow TOML Profile", new List<string> { ".toml" });

                var file = await picker.PickSaveFileAsync();
                if (file == null)
                {
                    return;
                }

                var profileId = profile.Id;
                var data = await Task.Run(() =>
                {
                    var conn = FfiDbInstance.Connect();
                    return conn.ExportProfileToml(profileId);
                });

                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteBytesAsync(file, Encoding.UTF8.GetBytes(data));
                await CachedFileManager.CompleteUpdatesAsync(file);

                NotifyUser("Profile exported successfully. Make sure sensitive information inside is removed before sharing this profile.", "Export Profile");
            }
            catch (Exception ex)
            {
                NotifyException("Exporting profile", ex);
            }
        }
        bool deleting = false;
        private async void OnDeleteRequested(object sender, HomeProfileControl control)
        {

            try
            {
                if (deleting)
                {
                    return;
                }
                deleting = true;

                var profile = control.Profile;
                ConfirmProfileDeleteDialog.Content = profile;
                if (_isDialogShown)
                {
                    deleting = false;
                    return;
                }
                _isDialogShown = true;

                var ret = await ConfirmProfileDeleteDialog.ShowAsync();
                _isDialogShown = false;
                if (ret != ContentDialogResult.Primary)
                {
                    deleting = false;
                    return;
                }

                await Task.Run(() =>
                {
                    var conn = FfiDbInstance.Connect();
                    conn.DeleteProfile(profile.Id);
                });

                deleting = false;
                ConfirmProfileDeleteDialog.Content = null;
                Profiles.Remove(profile);
            }
            catch (Exception ex)
            {
                NotifyException("Deleting profile", ex);
            }
            finally
            {
                deleting = false;
            }
        }

        private async Task ConnectToProfile(uint id)
        {
            var localSettings = ApplicationData.Current.LocalSettings.Values;
            localSettings["YTFLOW_PROFILE_ID"] = id;
            var connectTask = ConnectionState.Instance.Connect();
            var cancelToken = new System.Threading.CancellationTokenSource();

            try
            {
                var ret = await connectTask;
                if (ret == VpnManagementErrorStatus.Other)
                {
                    var coreError = localSettings["YTFLOW_CORE_ERROR_LOAD"] as string;
                    if (string.IsNullOrEmpty(coreError))
                    {
                        NotifyUser("Failed to connect VPN. Please connect YtFlow Auto using system settings for detailed error messages.", "VPN Error");
                    }
                }
                else if (ret != VpnManagementErrorStatus.Ok)
                {
                    NotifyUser($"Failed to connect VPN: error code {(int)ret}", "VPN Error");
                }
            }
            catch (TaskCanceledException)
            {
                // Handle cancellation
            }
            finally
            {
                cancelToken.Dispose();
            }
        }

        private void ConnectCancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vpnTask != null)
            {
                _vpnTask.Dispose();
                _vpnTask = null;
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vpnTask != null)
            {
                _vpnTask.Dispose();
            }
            _vpnTask = ConnectionState.Instance.Disconnect();
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewProfilePage));
        }


        private async void ImportProfileButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                Windows.UI.Xaml.Controls.Button a;

                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                picker.FileTypeFilter.Add(".toml");

                var file = await picker.PickSingleFileAsync();
                if (file == null)
                {
                    return;
                }

                var data = await FileIO.ReadBufferAsync(file);

                FfiParsedTomlProfile profile;
                FfiConn conn;
                try
                {
                    conn = FfiDbInstance.Connect();
                    profile = conn.ParseProfileToml(data);
                }
                catch (FfiException ex)
                {
                    NotifyUser(ex.Message, "Invalid Profile");
                    return;
                }

                if (_isDialogShown)
                {
                    return;
                }
                _isDialogShown = true;

                ConfirmProfileImportDialogPluginCountText.Text = profile.plugins.Count.ToString();
                ConfirmProfileImportDialogProfileNameText.Text = profile.name ?? "Unnamed Profile";
                var ret = await ConfirmProfileImportDialog.ShowAsync();
                _isDialogShown = false;

                if (ret != ContentDialogResult.Primary)
                {
                    return;
                }


                IList<FfiProfile> existingProfiles = BridgeExtensions.GetProfiles(conn);
                var baseProfileName = profile.name ?? "Unnamed Profile";
                var newProfileName = baseProfileName;
                var newProfileSuffix = 0;

                while (existingProfiles.Any(p => p.name == newProfileName))
                {
                    newProfileName = baseProfileName + " " + (++newProfileSuffix);
                }

                var locale = profile.locale ?? "en-US";
                var newProfileId = conn.CreateProfile(newProfileName, locale);

                foreach (var plugin in profile.plugins)
                {

                    var pluginId = conn.CreatePlugin(
                        newProfileId, plugin.plugin.name, plugin.plugin.desc, plugin.plugin.plugin,
                        plugin.plugin.plugin_version, plugin.plugin.param);

                    if (plugin.is_entry)
                    {
                        conn.SetPluginAsEntry(pluginId, newProfileId);
                    }
                }

                Profiles.Add(new ProfileModel()
                {
                    Id = newProfileId,
                    Name = newProfileName,
                    Locale = locale
                });
            }
            catch (Exception ex)
            {
                NotifyException("Importing profile", ex);
            }

        }
        IDictionary<uint, uint> hashcodes = new Dictionary<uint, uint>();
        bool connecting = false;
        private async void OnRefreshTimerTick(object sender, object e)
        {
            if (connecting) return;
            try
            {

                connecting = true;
                // Connect to CoreRpc if not already connected
                if (_rpc == null)
                {
                    _rpc = await CoreInterop.CoreRpc.ConnectAsync();
                }

                var qq = ApplicationData.Current.LocalFolder.Path;
                // Collect plugin info
                var pluginInfos = await _rpc.CollectAllPluginInfoAsync(hashcodes);

                // Update widgets
                foreach (var pluginInfo in pluginInfos)
                {

                    hashcodes[pluginInfo.id] = pluginInfo.hashcode;
                    if (!_widgets.TryGetValue(pluginInfo.id, out var widget))
                    {
                        // Create a new widget if it doesn't exist
                        widget = CreateWidgetHandle(pluginInfo);
                        if (widget == null)
                        {
                            continue;
                        }
                        _widgets[pluginInfo.id] = widget;

                    }
                    widget.Info = pluginInfo.info;
                    // Update widget info
                    widget.Widget.UpdateInfo();
                }
            }
            /*catch (RpcException ex)
            {
                NotifyUser(ex.Message, "RPC Error");
            }*/
            catch (Exception ex)
            {
                NotifyException("RPC", ex);
            }
            finally
            {
                connecting = false;
            }
        }

        public void SubscribeRefreshPluginStatus()
        {
            // Clear existing widgets and subscriptions
            if (_refreshTimer.IsEnabled == false)
            {
                ConnectedView.Visibility = Visibility.Visible;
                _refreshTimer.Stop();
                PluginWidgetPanel.Children.Clear();
                ConnectedViewSidePanel.Children.Clear();
                hashcodes.Clear();
                _widgets.Clear();

                // Start the refresh timer
                _refreshTimer.Start();
                OnRefreshTimerTick(_refreshTimer, EventArgs.Empty);
            }
        }
        private WidgetHandle CreateWidgetHandle(RpcPluginInfo info)
        {
            var handle = new WidgetHandle
            {
                Info = info.info
            };

            switch (info.plugin)
            {
                case "netif":
                    var netifWidget = new NetifHomeWidget(info.name, handle);//info.Name, handle.Info
                    handle.Widget = netifWidget;
                    PluginWidgetPanel.Children.Add(netifWidget);
                    break;

                case "switch":
                    var switchWidget = new SwitchHomeWidget(info.name, handle, MakeRequestSender(info.id));
                    handle.Widget = switchWidget;
                    PluginWidgetPanel.Children.Add(switchWidget);
                    break;

                case "dyn-outbound":
                    var dynOutboundWidget = new DynOutboundHomeWidget(info.name, handle, MakeRequestSender(info.id));
                    handle.Widget = (dynOutboundWidget);
                    PluginWidgetPanel.Children.Add(dynOutboundWidget);
                    break;

                case "forward":
                    var forwardWidget = new ForwardHomeWidget(info.name, handle);
                    handle.Widget = forwardWidget;
                    ConnectedViewSidePanel.Children.Add(forwardWidget);
                    break;

                default:
                    return null; // Return null for unsupported plugins
            }

            return handle;
        }

        private void NotifyException(string message, Exception ex)
        {
            UI.NotifyException(message, ex);
        }

        private void NotifyUser(string message, string title)
        {
            UI.NotifyUser(message, title);
        }


    }
}

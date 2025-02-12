using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Vpn;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.States;
using YtFlowApp2.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YtFlowApp2.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstTimePage : Page
    {
        private long _currentActivatedToken;

        public FirstTimePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
             Window.Current.Activated += Current_Activated;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
           // if (_currentActivatedToken != 0)
            {
                Window.Current.Activated -= Current_Activated;
            }
        }
        public string AppDisplayName => Windows.ApplicationModel.Package.Current.DisplayName;

        public static readonly string PROFILE_NAME = "Ytflow Csharp Auto";
        public string ProfileName => PROFILE_NAME;
        private async void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
                {
                    return;
                }

                var profile = await ConnectionState.GetInstalledVpnProfile();
                if (profile == null)
                {
                    await AddCustomVpnProfileAsync();
                    profile = await ConnectionState.GetInstalledVpnProfile();
                    if (profile == null)
                        return;
                }

                ConnectionState.Instance = new ConnectionState(profile);

                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(typeof(HomePage));
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("FirstTimePage activated", ex);
            }
        }
        public async Task AddCustomVpnProfileAsync()
        {
    
            VpnManagementAgent vpnAgent = new VpnManagementAgent();


            VpnPlugInProfile vpnProfile = new VpnPlugInProfile
            {
                ProfileName = "YtFlow Auto",
                RequireVpnClientAppUI = true, // 如果不需要 UI，设置为 false
                AlwaysOn = false,
                CustomConfiguration = "<CustomConfig><ServerAddress>vpn.example.com</ServerAddress></CustomConfig>", // 确保配置正确
                RememberCredentials = true,
                VpnPluginPackageFamilyName = "56263bdbai.YtFlow_5wvpqmt3a9dj6" // 确保与插件包族名一致
            };

            vpnProfile.ServerUris.Add(new Uri("vpn://vpn.example.com")); // 确保 URI 格式正确

            VpnManagementErrorStatus status = await vpnAgent.AddProfileFromObjectAsync(vpnProfile);
            //var ret = await vpnAgent.DeleteProfileAsync(vpnProfile);
            if (status == VpnManagementErrorStatus.Ok)
            {
                
            }
            else
            {
               
            }
        }

    }


}

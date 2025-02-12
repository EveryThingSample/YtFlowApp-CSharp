using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Vpn;
using Windows.UI.Xaml;

namespace YtFlowApp2.States
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public class ConnectionState
    {
        private static readonly VpnManagementAgent Agent = new VpnManagementAgent();

        private static ConnectionState _instance;
        public static ConnectionState Instance { get => _instance; set { _instance = value; } }

        VpnManagementConnectionStatus? _connectStatus = null;
        public VpnManagementConnectionStatus ConnectStatus
        {
            get => (VpnManagementConnectionStatus)(_connectStatus ?? VpnManagementConnectionStatus.Disconnected);
        }
        // public event EventHandler<VpnManagementConnectionStatus> ConnectStatusChange;
        public event Action<VpnManagementConnectionStatus> ConnectStatusChanged;
        // 
        public void UpdateVpnStatus(VpnManagementConnectionStatus status)
        {
            if (_connectStatus != status)
            {
                _connectStatus = status;
                ConnectStatusChanged?.Invoke(status); // 

            }
        }

        private VpnPlugInProfile m_profile;
        private DispatcherTimer _pollingTimer;
        public ConnectionState(VpnPlugInProfile profile)
        {

            m_profile = profile;

            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollingTimer.Tick += PollingTimer_Tick;

            // Start polling if the app is already resumed
            // if (Application.Current.ApplicationViewState != ApplicationViewState.Suspended)
            {
                _pollingTimer.Start();
            }

            // Subscribe to app lifecycle events
            Application.Current.LeavingBackground += Current_LeavingBackground;

            Application.Current.EnteredBackground += Current_EnteredBackground;
        }
        ~ConnectionState()
        {
            // Subscribe to app lifecycle events
            Application.Current.LeavingBackground -= Current_LeavingBackground;

            Application.Current.EnteredBackground -= Current_EnteredBackground;
            _pollingTimer.Stop();

        }
        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            _pollingTimer.Stop();
        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            _pollingTimer.Start();
        }



        private void PollingTimer_Tick(object sender, object e)
        {
            try
            {
                UpdateVpnStatus(m_profile.ConnectionStatus);
            }
            catch
            {
                UpdateVpnStatus(VpnManagementConnectionStatus.Disconnected);
            }
        }
        public static async Task<ConnectionState> InitializeAsync()
        {
            var profile = await GetInstalledVpnProfile();
            if (profile != null)
            {
                return _instance = new ConnectionState(profile);
            }
            return null;
            //throw new InvalidOperationException("Profile not found.");
        }
        public static string PROFILE_NAME => Pages.FirstTimePage.PROFILE_NAME;
        public static async Task<VpnPlugInProfile> GetInstalledVpnProfile()
        {
            var profiles = await Agent.GetProfilesAsync();
            foreach (var profile in profiles)
            {

                if (string.Equals(profile.ProfileName, PROFILE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    return profile as VpnPlugInProfile;
                }
            }
            return null;
        }

        public async Task<VpnManagementErrorStatus> Connect()
        {
            UpdateVpnStatus(VpnManagementConnectionStatus.Connecting);
            var ret = await Agent.ConnectProfileAsync(m_profile);
            if (ret == VpnManagementErrorStatus.Ok)
            {
                UpdateVpnStatus(VpnManagementConnectionStatus.Connected);
            }
            return ret;
        }

        public async Task<VpnManagementErrorStatus> Disconnect()
        {
            UpdateVpnStatus(VpnManagementConnectionStatus.Disconnecting);
            var ret = await Agent.DisconnectProfileAsync(m_profile);
            if (ret == VpnManagementErrorStatus.Ok)
            {
                UpdateVpnStatus(VpnManagementConnectionStatus.Disconnected);
            }
            return ret;
        }

        private string StatusToString(VpnManagementConnectionStatus status)
        {
            switch (status)
            {
                case VpnManagementConnectionStatus.Connected:
                    return "Connected";
                case VpnManagementConnectionStatus.Disconnected:
                    return "Disconnected";
                case VpnManagementConnectionStatus.Connecting:
                    return "Connecting";
                case VpnManagementConnectionStatus.Disconnecting:
                    return "Disconnecting";
                default:
                    return "Unknown";
            }
        }

    }
}

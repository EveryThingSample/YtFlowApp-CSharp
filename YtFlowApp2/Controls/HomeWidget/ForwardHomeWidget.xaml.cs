using Newtonsoft.Json.Linq;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.Pages;
using YtFlowApp2.Utils;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace YtFlowApp2.Controls.HomeWidget
{
    public sealed partial class ForwardHomeWidget : UserControl, IHomeWidget
    {
        private struct ForwardStatSnapshot
        {
            public ulong uplink_written { get; set; }
            public ulong downlink_written { get; set; }
            public uint tcp_connection_count { get; set; }
            public uint udp_session_count { get; set; }
        }

        private IDisposable _renderStatSubscription;
        private WidgetHandle _sharedInfo;
        private ForwardStatSnapshot _currentStat;
        private ForwardStatSnapshot _lastStat;
        private DispatcherTimer _timer;
        private DateTime _lastTimestamp;

        public ForwardHomeWidget()
        {
            this.InitializeComponent();
            init();

        }

        public ForwardHomeWidget(string pluginName, WidgetHandle sharedInfo)
        {
            _sharedInfo = sharedInfo;
            this.InitializeComponent();
            init();
            PluginNameText.Text = pluginName;

        }
        private void init()
        {
            _lastTimestamp = DateTime.UtcNow;
            _lastStat = new ForwardStatSnapshot();
            _currentStat = new ForwardStatSnapshot();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // 每秒触发一次
            _timer.Tick += Timer_Tick;
            // 监听应用进入和离开后台事件
            Application.Current.EnteredBackground += Current_EnteredBackground;
            Application.Current.LeavingBackground += Current_LeavingBackground;
        }


        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            // 处理应用进入后台时的操作
            _timer.Stop();  // 停止定时器
        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            // 处理应用离开后台时的操作
            _timer.Start();  // 启动定时器
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 启动定时器
            _timer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 卸载时停止定时器
            _timer.Stop();
            _renderStatSubscription?.Dispose();
        }

        private void Timer_Tick(object sender, object e)
        {
            var now = DateTime.UtcNow;

            if (now == _lastTimestamp)
            {
                return;
            }

            try
            {
                var stat = _currentStat;
                TcpCountText.Text = stat.tcp_connection_count.ToString();
                UdpCountText.Text = stat.udp_session_count.ToString();

                var timespan = now - _lastTimestamp;
                string uplinkText, downlinkText;

                if (timespan > TimeSpan.FromSeconds(3))
                {
                    uplinkText = HumanizeByteSpeed(0);
                    downlinkText = HumanizeByteSpeed(0);
                }
                else if (timespan < TimeSpan.FromMilliseconds(1005))
                {
                    uplinkText = HumanizeByteSpeed(stat.uplink_written - _lastStat.uplink_written);
                    downlinkText = HumanizeByteSpeed(stat.downlink_written - _lastStat.downlink_written);
                }
                else
                {
                    var scale = timespan.TotalMilliseconds / 1000.0;
                    uplinkText = HumanizeByteSpeed((ulong)((stat.uplink_written - _lastStat.uplink_written) / scale));
                    downlinkText = HumanizeByteSpeed((ulong)((stat.downlink_written - _lastStat.downlink_written) / scale));
                }

                UplinkText.Text = uplinkText;
                DownlinkText.Text = downlinkText;

                _lastStat = stat;
                _lastTimestamp = now;
            }
            catch (Exception ex)
            {
                NotifyException($"ForwardHomeWidget Stat subscribe: {ex.Message}", ex);
            }
        }

        public void UpdateInfo()
        {
            try
            {
                var snapshot = CoreInterop.BridgeExtensions.FromCBORBytes<ForwardStatSnapshot>(_sharedInfo.Info.ToArray());

              
                _currentStat = snapshot;
            }
            catch (Exception ex)
            {
                NotifyException("Updating Forward", ex);
            }
        }

        private void NotifyException(string message, Exception ex)
        {
            UI.NotifyException(message, ex);
        }

        public static string HumanizeByteSpeed(ulong num)
        {
            return UI.HumanizeByteSpeed(num);
        }
    }
}

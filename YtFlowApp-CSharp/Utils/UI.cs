using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace YtFlowApp2.Utils
{
    public static class UI
    {
        private static CoreDispatcher _dispatcher;
        private static ConcurrentQueue<(string Message, string Title)> _messages = new ConcurrentQueue<(string, string)>();
        private static bool _isQueueRunning;

        public static void NotifyUser(string msg, string title, CoreDispatcher inputDispatcher = null)
        {
            if (inputDispatcher != null)
            {
                _dispatcher = inputDispatcher;
                return;
            }
            
            _messages.Enqueue((msg, title));

            if (_dispatcher != null && !_isQueueRunning)
            {
                _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    _isQueueRunning = true;
                    while (_messages.TryDequeue(out var message))
                    {
                        await ShowDialogAsync(message.Message, message.Title);
                    }
                    _isQueueRunning = false;
                });
            }
        }

        private static async Task ShowDialogAsync(string message, string title)
        {
            ContentDialog dialog = new ContentDialog
            {
                Content = message,
                FullSizeDesired = false
            };

            if (!string.IsNullOrEmpty(title))
            {
                dialog.Title = title;
            }

            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Input.StandardUICommand"))
            {
                dialog.CloseButtonCommand = new StandardUICommand( StandardUICommandKind.Close);
            }
            else if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.ContentDialog", "CloseButtonText"))
            {
                dialog.CloseButtonText = "Close";
            }
            else
            {
                dialog.SecondaryButtonText = "Close";
            }

            try
            {
                await dialog.ShowAsync();
            }
            catch
            {
                // Handle exceptions if needed
            }
        }
        public static void NotifyException(string errorMessage)
        {
            

                // string title = $"Unexpected exception";
                UI.NotifyUser(errorMessage, "Error");
            
        }
        public static void NotifyException(string title,Exception ex)
        {
            {
                string errorMessage = ex.ToString();
               // string title = $"Unexpected exception";
                UI.NotifyUser(errorMessage, title);
            }
        }
        public static string HumanizeByte(ulong num)
        {
            if (num == 0)
                return "0 B";
            if (num < 1024)
                return $"{num} B";
            if (num < 1024 * 1000)
                return $"{(double)num * 10 / 1024 / 10:F1} KB";
            if (num < 1024 * 1024 * 1000)
                return $"{(double)num * 10 / 1024 / 1024 / 10:F1} MB";
            if (num < 1024L * 1024 * 1024000)
                return $"{(double)num * 10 / 1024 / 1024 / 1024 / 10:F1} GB";
            return "∞";
        }

        public static string HumanizeByteSpeed(ulong num)
        {
            if (num == 0)
                return "0 B/s";
            if (num < 1024)
                return $"{num} B/s";
            if (num < 1024 * 1000)
                return $"{(double)num * 10 / 1024 / 10:F1} KB/s";
            if (num < 1024 * 1024 * 1000)
                return $"{(double)num * 10 / 1024 / 1024 / 10:F1} MB/s";
            if (num < 1024L * 1024 * 1024 * 1000)
                return $"{(double)num * 10 / 1024 / 1024 / 1024 / 10:F1} GB/s";
            return "∞";
        }
        public static string FormatNaiveDateTime(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return "";

            DateTime tp;
            if (DateTime.TryParse(dateStr, out tp))
            {
                TimeZoneInfo tz = TimeZoneInfo.Local;
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(tp, tz);
                return localTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return "";
        }
    }
}

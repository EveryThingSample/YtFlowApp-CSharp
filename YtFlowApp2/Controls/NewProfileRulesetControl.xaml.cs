using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Utils;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YtFlowApp2.Controls
{
    public sealed partial class NewProfileRulesetControl : ContentDialog, INotifyPropertyChanged
    {
        private bool _rulesetSelected;
        private string _rulesetName;
        private HttpClient _client;
        private bool _updating;
        private bool _updateCancelled;
        private List<FfiResource> _resources = new List<FfiResource>();
        private StorageFolder _resourceFolder;

        public event PropertyChangedEventHandler PropertyChanged;

        public NewProfileRulesetControl()
        {
            InitializeComponent();
        }

        public bool RulesetSelected => _rulesetSelected;
        public string RulesetName => _rulesetName;

        public string GetResourceKeyFromSelectedRuleset()
        {
            var selectedItemControl = SelectionComboBox.SelectedItem as ComboBoxItem;
            if (selectedItemControl == null)
            {
                return string.Empty;
            }

            var tag = selectedItemControl.Tag as string;
            switch (tag)
            {
                case "dreamacro-geoip":
                    return "dreamacro-geoip";
                case "loyalsoldier-country-only-cn-private":
                    return "loyalsoldier-country-only-cn-private";
                case "loyalsoldier-surge-proxy":
                    return "loyalsoldier-surge-proxy";
                case "loyalsoldier-surge-direct":
                    return "loyalsoldier-surge-direct";
                case "loyalsoldier-surge-private":
                    return "loyalsoldier-surge-private";
                case "loyalsoldier-surge-reject":
                    return "loyalsoldier-surge-reject";
                case "loyalsoldier-surge-tld-not-cn":
                    return "loyalsoldier-surge-tld-not-cn";
                default:
                    return string.Empty;
            }
        }

        public async Task<bool> BatchUpdateRulesetsIfNotExistAsync(List<string> rulesetKeys)
        {
            foreach (var rulesetKey in rulesetKeys)
            {
                var item = SelectionComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Tag as string) == rulesetKey);
                if (item == null)
                {
                    continue;
                }

                SelectionComboBox.SelectedItem = item;
                if (!await InitSelectedRuleset())
                {
                    return false;
                }

                if (IsPrimaryButtonEnabled)
                {
                    continue;
                }

                if (!await UpdateAsync())
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> InitSelectedRuleset()
        {
            try
            {
                var resourceKey = GetResourceKeyFromSelectedRuleset();
                if (string.IsNullOrEmpty(resourceKey))
                {
                    return false;
                }

                SelectionComboBox.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                UpdateErrorText.Text = string.Empty;
                IsPrimaryButtonEnabled = false;

                var shouldLoadResources = _resources.Count == 0;
                FfiResource resource = null;
                if (!shouldLoadResources)
                {
                    resource = _resources.FirstOrDefault(r => r.key == resourceKey);
                }

                if (_resourceFolder == null)
                {
                    _resourceFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                        "resource", CreationCollisionOption.OpenIfExists);
                }

                if (shouldLoadResources)
                {
                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    _resources = BridgeExtensions.GetResources(conn);
                    resource = _resources.FirstOrDefault(r => r.key == resourceKey);
                }

                string lastUpdated;
                bool fileExists = false;
                if (resource != null)
                {
                    if (resource.remote_type == "url")
                    {
                        var resourceUrl = BridgeExtensions.GetResourceUrlByResourceId(BridgeExtensions.FfiDbInstance.Connect(),resource.id);
                        lastUpdated = resourceUrl.retrieved_at ?? "never";
                    }
                    else if (resource.remote_type == "github_release")
                    {
                        var resourceUrl = BridgeExtensions.GetResourceGitHubReleaseByResourceId(BridgeExtensions.FfiDbInstance.Connect(), resource.id);
                        lastUpdated = resourceUrl.retrieved_at ?? "never";
                    }
                    else
                    {
                        lastUpdated = "unknown";
                    }

                    if (!string.IsNullOrEmpty(resource.local_file))
                    {
                        var item = await _resourceFolder.TryGetItemAsync(resource.local_file);
                        fileExists = item != null && item.IsOfType(StorageItemTypes.File);
                    }
                }
                else
                {
                    lastUpdated = "never";
                }

                IsPrimaryButtonEnabled = fileExists;
                LastUpdatedText.Text = lastUpdated;
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Hide();
                    NotifyException("Initializing resource", ex);
                });
                return false;
            }
            finally
            {
                SelectionComboBox.IsEnabled = true;
                UpdateButton.IsEnabled = true;
            }
            return true;
        }

        private async void SelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await InitSelectedRuleset();
        }

        private async void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            _rulesetSelected = false;
            SelectionComboBox.SelectionChanged += SelectionComboBox_SelectionChanged;
            await InitSelectedRuleset();
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (_updating)
            {
                args.Cancel = true;
            }
            else
            {
                SelectionComboBox.SelectionChanged -= SelectionComboBox_SelectionChanged;
            }
        }

        private void CancelUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_updating)
            {
                _updateCancelled = true;
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateAsync();
        }

        private async Task<bool> UpdateAsync()
        {
            StorageFile file = null;
            bool finishedUpdate = false;

            try
            {
                _updateCancelled = false;
                IsPrimaryButtonEnabled = false;
                SelectionComboBox.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                CancelUpdateButton.Visibility = Visibility.Visible;
                UpdateProgressBar.Value = 0;

                var resourceKey = GetResourceKeyFromSelectedRuleset();
                var resource = _resources.FirstOrDefault(r => r.key == resourceKey);

                if (_client == null)
                {
                    _client = new HttpClient();
                }

                var newFileName = Guid.NewGuid().ToString();
                var newFileNameW = newFileName;

                string url, oldFileName;
                uint resourceId;
                if (resource != null)
                {
                    if (resource.remote_type != "url")
                    {
                        throw new ArgumentException("Unknown remote type");
                    }

                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    var resourceUrl = BridgeExtensions.GetResourceUrlByResourceId(conn,resource.id);
                    resourceId = resource.id;
                    url = resourceUrl.url;
                    oldFileName = resource.local_file;
                }
                else
                {
                    string type;
                    switch (resourceKey)
                    {
                        case "dreamacro-geoip":
                            type = "geoip-country";
                            url = "https://cdn.jsdelivr.net/gh/Dreamacro/maxmind-geoip@release/Country.mmdb";
                            break;
                        case "loyalsoldier-country-only-cn-private":
                            type = "geoip-country";
                            url = "https://cdn.jsdelivr.net/gh/Loyalsoldier/geoip@release/Country-only-cn-private.mmdb";
                            break;
                        case "loyalsoldier-surge-proxy":
                            type = "surge-domain-set";
                            url = "https://cdn.jsdelivr.net/gh/Loyalsoldier/surge-rules@release/proxy.txt";
                            break;
                        case "loyalsoldier-surge-direct":
                            type = "surge-domain-set";
                            url = "https://cdn.jsdelivr.net/gh/Loyalsoldier/surge-rules@release/direct.txt";
                            break;
                        case "loyalsoldier-surge-private":
                            type = "surge-domain-set";
                            url = "https://cdn.jsdelivr.net/gh/Loyalsoldier/surge-rules@release/private.txt";
                            break;
                        case "loyalsoldier-surge-reject":
                            type = "surge-domain-set";
                            url = "https://cdn.jsdelivr.net/gh/Loyalsoldier/surge-rules@release/reject.txt";
                            break;
                        case "loyalsoldier-surge-tld-not-cn":
                            type = "surge-domain-set";
                            url = "https://cdn.jsdelivr.net/gh/Loyalsoldier/surge-rules@release/tld-not-cn.txt";
                            break;
                        default:
                            throw new ArgumentException("Unknown resource key for URL");
                    }
                    var q =  Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    resourceId = conn.CreateResourceWithUrl(resourceKey, type, newFileName, url);
                    oldFileName = newFileName;
                }

                string errMsg = "";
                try
                {
                    _client.DefaultRequestHeaders.UserAgent.Clear();
                    _client.DefaultRequestHeaders.UserAgent.ParseAdd("YtFlowApp/0.0 ResourceUpdater/0.0");

                    var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                    var res = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                    res.EnsureSuccessStatusCode();

                    var headers = res.Headers;
                    var newEtag = headers.TryGetValues("etag", out var etag) ? etag.FirstOrDefault() : null;
                    var newLastModified = headers.TryGetValues("last-modified", out var lastModified) ? lastModified.FirstOrDefault() : DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                    var content = res.Content;
                    var resLen = content.Headers.ContentLength ?? 0;

                    file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(newFileNameW);
                    var fstream = (await file.OpenAsync(FileAccessMode.ReadWrite)).AsStream();

                    UpdateProgressBar.Visibility = Visibility.Visible;
                    UpdateProgressBar.IsIndeterminate = resLen == 0;
                    _updating = true;

                    var resStream = await content.ReadAsStreamAsync();
                    var buf = new byte[32768];
                    ulong totalWritten = 0;

                    while (true)
                    {
                        var readBuf = await resStream.ReadAsync(buf,0, buf.Length);
                        if (readBuf == 0)
                        {
                            break;
                        }

                        await fstream.WriteAsync(buf,0, readBuf);
                        totalWritten += (ulong)readBuf;

                        if (_updateCancelled)
                        {
                            throw new TaskCanceledException();
                        }

                        if (resLen > 0)
                        {
                            var percentage = (double)totalWritten / resLen * 100;
                            UpdateProgressBar.Value = percentage;
                        }
                    }

                    await fstream.FlushAsync();
                    fstream.Dispose();
                    content.Dispose();

                    await file.MoveAsync(_resourceFolder, oldFileName, NameCollisionOption.ReplaceExisting);
                    file = null;

                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    //convert
                    conn.UpdateResourceUrlRetrievedByResourceId(
                        resourceId, newEtag, newLastModified);
                    finishedUpdate = true;
                }
                catch (TaskCanceledException)
                {
                    // Update was cancelled
                }
                catch (Exception ex)
                {
                    errMsg = ex.Message;
                }

                _resources.Clear();
                _updating = false;
                await InitSelectedRuleset();
                UpdateErrorText.Text = errMsg;
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _updating = false; // Allow the dialog to close
                    Hide();
                    NotifyException("Updating", ex);
                });
                await InitSelectedRuleset();
            }
            finally
            {
                if (file != null)
                {
                    await file.DeleteAsync();
                }

                UpdateButton.IsEnabled = true;
                CancelUpdateButton.Visibility = Visibility.Collapsed;
                UpdateProgressBar.Visibility = Visibility.Collapsed;
            }

            return finishedUpdate;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _rulesetSelected = true;
            _rulesetName = GetResourceKeyFromSelectedRuleset();
        }

        private void NotifyException(string context, Exception ex)
        {
           UI.NotifyException(context, ex);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

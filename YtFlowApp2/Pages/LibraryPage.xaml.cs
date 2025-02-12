using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using YtFlowApp2.Classes;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Models;
using YtFlowApp2.Pages;
using YtFlowApp2.Utils;

namespace YtFlowApp2.Pages
{
    public sealed partial class LibraryPage : Page
    {
        public LibraryPage()
        {
            this.InitializeComponent();
            LoadModel();
        }

        public static readonly DependencyProperty ProxyGroupProxySelectedCountProperty =
            DependencyProperty.Register(nameof(ProxyGroupProxySelectedCount), typeof(uint), typeof(LibraryPage), new PropertyMetadata(0u));

        public static readonly DependencyProperty IsProxyGroupLockedProperty =
            DependencyProperty.Register(nameof(IsProxyGroupLocked), typeof(bool), typeof(LibraryPage), new PropertyMetadata(true));
       
        public static bool IsProxyGroupProxyShareEnabled(uint proxySelectedCount) => proxySelectedCount > 0;

        public static bool IsProxyGroupProxyEditEnabled(uint proxySelectedCount) => proxySelectedCount == 1;

        public static bool IsProxyGroupProxyAddEnabled(bool isSubscription, bool isProxyGroupLocked) => !(isSubscription && isProxyGroupLocked);

        public static bool IsProxyGroupProxyDeleteEnabled(bool isSubscription, bool isProxyGroupLocked, uint proxySelectedCount) =>
            !(isSubscription && isProxyGroupLocked) && proxySelectedCount > 0;

        public AssetModel Model { get; private set; } = new AssetModel();


       

        private async Task LoadModel()
        {
            try
            {
                await Task.Run(async () =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    var proxyGroups = CoreInterop.BridgeExtensions.GetProxyGroups(conn);
                    var proxyGroupModels = new List<ProxyGroupModel>();
                    var subscriptionInfoToAttach = new List<(ProxyGroupModel, FfiProxyGroupSubscription)>();

                    foreach (var group in proxyGroups)
                    {
                        var model = new ProxyGroupModel(group);
                        if (group.type == "subscription")
                        {
                            var subscriptionInfo = CoreInterop.BridgeExtensions.GetProxySubscriptionByProxyGroup(conn,group.id);
                            subscriptionInfoToAttach.Add((model, subscriptionInfo));
                        }
                        proxyGroupModels.Add(model);
                    }

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        foreach (var (model, subscriptionInfo) in subscriptionInfoToAttach)
                        {
                            model.AttachSubscriptionInfo(subscriptionInfo);
                        }
                        Model.ProxyGroups = new System.Collections.ObjectModel.ObservableCollection<ProxyGroupModel>(proxyGroupModels);
                        PopulateProxyGroupItemsForMenu();
                    });
                });
            }
            catch (Exception ex)
            {
                UI.NotifyException("Loading Library page", ex);
            }
        }

        bool isDetailedViewShown;
        bool isDialogShown;


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.Cancel) return;
            if (e.NavigationMode == NavigationMode.Back && isDetailedViewShown)
            {
                VisualStateManager.GoToState(this, "DisplayAssetView", true);
                e.Cancel = true;
                isDetailedViewShown = false;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Implement Rx subscription logic here if needed
            //ProxySubscriptionUpdatesRunningChanged.
        }
        bool _ProxySubscriptionUpdatesRunning;
        public bool ProxySubscriptionUpdatesRunning
        {
            get => _ProxySubscriptionUpdatesRunning; set
            {
                if (value != _ProxySubscriptionUpdatesRunning)
                {
                    _ProxySubscriptionUpdatesRunning = value;
                    if (_ProxySubscriptionUpdatesRunning)
                    {
                        SyncSubscriptionButtonRunStoryboard.Begin();
                    }
                    else{
                        SyncSubscriptionButtonRunStoryboard.Stop();
                    }
                }
            }
        }
        private async void ProxyGroupItemDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (sender as FrameworkElement)?.DataContext as ProxyGroupModel;
                if (item == null) return;

                if (isDialogShown) return;
                isDialogShown = true;

                var dialogResult = await ProxyGroupDeleteDialog.ShowAsync();
                isDialogShown = false;
                if (dialogResult != ContentDialogResult.Primary) return;

                await Task.Run(() =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    conn.DeleteProxyGroup(item.Id);
                });

                Model.ProxyGroups.Remove(item);
            }
            catch (Exception ex)
            {
                UI.NotifyException("Deleting", ex);
            }
        }

        private void ProxyGroupItemRename_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as ProxyGroupModel;
            if (item == null) return;
            RenameProxyGroupItem(item);
        }

        private async Task RenameProxyGroupItem(ProxyGroupModel model)
        {
            try
            {
                if (isDialogShown) return;
                isDialogShown = true;

                ProxyGroupRenameDialogText.Text = model.Name;
                var dialogResult = await ProxyGroupRenameDialog.ShowAsync();
                isDialogShown = false;
                if (dialogResult != ContentDialogResult.Primary) return;

                var newName = ProxyGroupRenameDialogText.Text;

                await Task.Run(() =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    conn.RenameProxyGroup(model.Id, newName);
                });

                model.Name = newName;
            }
            catch (Exception ex)
            {
                UI.NotifyException("Renaming", ex);
            }
        }

        private async void CreateProxyGroupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newGroupName = $"New Group {Model.ProxyGroups.Count + 1}";
                uint newGroupId = 0;

                await Task.Run(() =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    newGroupId = conn.CreateProxyGroup(newGroupName, "manual");
                });

                var newGroupModel = new ProxyGroupModel { Id = newGroupId, Name = newGroupName };
                Model.ProxyGroups.Add(newGroupModel);
                await RenameProxyGroupItem(newGroupModel);
            }
            catch (Exception ex)
            {
                UI.NotifyException("Creating", ex);
            }
        }

        private static HttpClient GetHttpClientForSubscription()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("YtFlowApp/0.0 SubscriptionUpdater/0.0");
            return client;
        }

        private async Task DownloadSubscriptionProxies(HttpClient client, Uri uri, string format, SubscriptionDownloadDecodeResult result)
        {
            
            var res = await client.GetAsync(uri);
            res.EnsureSuccessStatusCode();

            var userinfoHeader = res.Headers.TryGetValue("subscription-userinfo", out var values) ? values : null;
            if (userinfoHeader != null)
            {
                result.userinfo = BridgeExtensions.DecodeSubscriptionUserInfoFromResponseHeaderValue(userinfoHeader);
            }

            var resStr = await res.Content.ReadAsStringAsync();
            
            result.proxies = YtFlowApp2.CoreInterop.BridgeExtensions.DecodeSubscriptionProxies(resStr, ref format);
            if (result.proxies == null || format == null)
            {
                throw new ArgumentException("The subscription data contains no valid proxy.");
            }
            else
            {
                result.format = format;
                result.expiresAt = result.userinfo.expires_at??"";
            }
        }

        private async void CreateSubscriptionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = GetHttpClientForSubscription();
                while (await ProxyGroupAddSubscriptionDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    var url = ProxyGroupAddSubscriptionUrlText.Text;
                    var result = new SubscriptionDownloadDecodeResult();
                    ProxySubscriptionUpdatesRunning = true;
                    try
                    {
                        var uri = new Uri(url);

                        await DownloadSubscriptionProxies(client, uri, null, result);

                        await Task.Run(() =>
                        {
                            var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                            var newGroupId = conn.CreateProxySubscriptionGroup(uri.Host, result.format, url);
                            conn.BatchUpdateProxyInGroup(newGroupId, result.proxies);
                            conn.UpdateProxySubscriptionRetrievedByProxyGroup(newGroupId, result.userinfo.upload_bytes_used,
                                result.userinfo.download_bytes_used, result.userinfo.bytes_total, result.expiresAt);


                            var q = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                var newGroupModel = new ProxyGroupModel { Id = newGroupId, Name = uri.Host };
                                newGroupModel.AttachSubscriptionInfo(BridgeExtensions.GetProxySubscriptionByProxyGroup(conn, newGroupId));
                                Model.ProxyGroups.Add(newGroupModel);
                            });

                        });
                        break;
                    }
                    catch (Exception ex)
                    {
                        ProxyGroupAddSubscriptionError.Text = ex.Message;
                        ProxyGroupAddSubscriptionError.Visibility = Visibility.Visible;
                    }
                    finally
                    {
                        ProxySubscriptionUpdatesRunning = false;
                    }
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Import Subscription", ex);
            }
        }

        private void SyncSubscriptionButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateSubscription(null);
        }
        public struct DecodedSubscriptionUserInfo
        {
            public UInt64? upload_bytes_used;
            public UInt64? download_bytes_used;
            public UInt64? bytes_total;
            public string expires_at;
        }
        class SubscriptionDownloadDecodeResult
        {
            public byte[] proxies;
            public string format;
            public DecodedSubscriptionUserInfo userinfo;
            public string expiresAt = "";
        };

        
private async Task UpdateSubscription(uint? id)
        {
            var proxyGroups = Model.ProxyGroups
                .Where(model => !model.IsManualGroup && !model.IsUpdating && (!id.HasValue || model.Id == id.Value))
                .ToList();

            try
            {
                foreach (var model in proxyGroups)
                {
                    model.IsUpdating = true;
                }
                ProxySubscriptionUpdatesRunning = true;
                var client = GetHttpClientForSubscription();
                var errors = new List<string>();
                var subscriptionInfoToAttach = new List<(ProxyGroupModel, FfiProxyGroupSubscription)>();
                await Task.Run(async () =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();

                    foreach (var model in proxyGroups)
                    {
                        try
                        {

                            var subscription = BridgeExtensions.GetProxySubscriptionByProxyGroup(conn,model.Id);
                            var result = new SubscriptionDownloadDecodeResult();
                            await DownloadSubscriptionProxies(client, new Uri(subscription.url), subscription.format, result);

                            conn.BatchUpdateProxyInGroup(model.Id, result.proxies);
                            conn.UpdateProxySubscriptionRetrievedByProxyGroup(model.Id, result.userinfo.upload_bytes_used,
                                result.userinfo.download_bytes_used, result.userinfo.bytes_total, result.expiresAt);

                            subscription = BridgeExtensions.GetProxySubscriptionByProxyGroup(conn, model.Id);
                            subscriptionInfoToAttach.Add((model, subscription));

//                          
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{model.Name}: {ex.Message}");
                        }
                    }
                });
                
                foreach (var (model, subscriptionInfo) in subscriptionInfoToAttach)
                {
                    model.AttachSubscriptionInfo(subscriptionInfo);
                    if (model.Proxies != null)
                    {
                        model.Proxies = null;
                        LoadProxiesForProxyGroup(model);
                    }
                }
                if (errors.Any())
                {
                    UI.NotifyUser(string.Join(Environment.NewLine, errors), "Update errors");
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Updating Subscription", ex);
            }
            finally
            {
                foreach (var model in proxyGroups)
                {
                    model.IsUpdating = false;
                }
            }
            ProxySubscriptionUpdatesRunning = false;
        }

        private void EditProxyInCurrentProxyGroup(ProxyModel proxyModel)
        {
            var isSubscription = Model.CurrentProxyGroupModel.IsSubscription;
            var isReadonly = isSubscription && IsProxyGroupLocked;
            Frame.Navigate(typeof(EditProxyPage), new EditProxyPageParam(isReadonly, proxyModel));
        }

        private void PopulateProxyGroupItemsForMenu()
        {
            ProxyGroupProxyDuplicateFlyout.Items.Clear();
            foreach (var proxyGroupModel in Model.ProxyGroups)
            {
                if (proxyGroupModel.IsSubscription) continue;

                var menuItemForDuplicate = new MenuFlyoutItem
                {
                    Text = proxyGroupModel.Name,
                    DataContext = proxyGroupModel
                };
                menuItemForDuplicate.Click += ProxyGroupItemDuplicate_Click;
                ProxyGroupProxyDuplicateFlyout.Items.Add(menuItemForDuplicate);
            }
        }

      
        private void ProxyGroupItem_Click(object sender, RoutedEventArgs e)
        {
            var model = (sender as FrameworkElement)?.DataContext as ProxyGroupModel;
            if (model == null) return;

            LoadProxiesForProxyGroup(model);
            SetValue(IsProxyGroupLockedProperty, true);
            Model.CurrentProxyGroupModel = model;
            VisualStateManager.GoToState(this, "DisplayProxyGroupView", true);
            isDetailedViewShown = true;
            Bindings.Update();
        }

        private async Task LoadProxiesForProxyGroup(ProxyGroupModel model)
        {
            try
            {
                if (model.Proxies != null) return;
                IList<ProxyModel> result = null;
                await Task.Run(() =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    var ffiProxies = BridgeExtensions.GetProxiesByProxyGroup(conn,model.Id);
                    result = ffiProxies.Select(ffiProxy => new ProxyModel(ffiProxy)).ToList();
                  
                });
                model.Proxies = new System.Collections.ObjectModel.ObservableCollection<ProxyModel>(result);
            }
            catch (Exception ex)
            {
                UI.NotifyException("Loading proxies for proxy group", ex);
            }
        }

        private void ProxyGroupProxyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCount = (sender as ListView)?.SelectedItems.Count ?? 0;
            ProxyGroupProxySelectedCount = (uint)selectedCount;
        }

        private async void ProxyGroupDeleteProxyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isDialogShown) return;
                isDialogShown = true;

                var selected = ProxyGroupProxyList.SelectedItems;
                var proxyGroup = Model.CurrentProxyGroupModel;
                if (proxyGroup == null) return;

                switch (selected.Count)
                {
                    case 0:
                        return;
                    case 1:
                        var proxy = selected[0] as ProxyModel;
                        if (proxy == null) return;
                        ProxyGroupDeleteProxyPlaceholder.Text = proxy.Name;
                        break;
                    default:
                        ProxyGroupDeleteProxyPlaceholder.Text = $"{selected.Count} proxies";
                        break;
                }

                var dialogResult = await ProxyGroupProxyDeleteDialog.ShowAsync();
                isDialogShown = false;
                if (dialogResult != ContentDialogResult.Primary) return;

                var proxyIds = selected.Cast<ProxyModel>().Select(p => p.Id).ToHashSet();

                await Task.Run(() =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    foreach (var id in proxyIds)
                    {
                        conn.DeleteProxy(id);
                    }
                });

                var existingProxies = proxyGroup.Proxies;
                for (int i = 0; i < existingProxies.Count;)
                {
                    if (proxyIds.Contains(existingProxies[i].Id))
                    {
                        existingProxies.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Deleting proxies", ex);
            }
        }
        public static Tuple<string, byte[]> ConvertShareLinkToDataProxyV1(string link)
        {
            KeyValuePair<string, byte[]> pair;
            try
            {
                // 调用 FFI 函数解码 ShareLink
                var proxyBuffer = YtFlowAppBridge.Bridge.YtflowCore.ytflow_app_share_link_decode(link);
              
                // 提取代理名称
                var name = CoreInterop.BridgeExtensions.FromCBORBytes<string>(proxyBuffer.ToArray());

                // 调用 FFI 函数组合 DataProxy
                var dataBufferPtr = YtFlowAppBridge.Bridge.YtflowCore.ytflow_app_proxy_data_proxy_compose_v1(proxyBuffer);
                var dataBuffer = (dataBufferPtr);

                // 返回结果
                return Tuple.Create(name, dataBuffer.ToArray());
            }
            catch (Exception)
            {
                // 发生异常时返回 null
                return null;
            }
        }
        private async void ProxyGroupAddProxyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isDialogShown) return;
                isDialogShown = true;

                ProxyGroupProxyImportText.Text = string.Empty;
                var dialogResult = await ProxyGroupProxyImportDialog.ShowAsync();
                isDialogShown = false;
                if (dialogResult != ContentDialogResult.Primary) return;

                var input = ProxyGroupProxyImportText.Text;
                if (string.IsNullOrEmpty(input)) return;

                var currentGroup = Model.CurrentProxyGroupModel;
                var groupId = currentGroup.Id;

                await Task.Run(() =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    int unrecognized = 0;
                    var newProxyIds = new List<uint>();

                    foreach (var line in input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedLink = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLink)) continue;

                        var proxy = ConvertShareLinkToDataProxyV1(trimmedLink);
                        if (null!=proxy)
                        {
                            unrecognized++;
                            continue;
                        }

                        var (proxyName, proxyParam) = proxy;
                        var newProxyId = conn.CreateProxy(groupId, proxyName, proxyParam, 0);
                        newProxyIds.Add(newProxyId);
                    }

                    var ffiProxies = BridgeExtensions.GetProxiesByProxyGroup(conn,groupId);
                    var ffiProxySet = ffiProxies.ToDictionary(p => p.id);
                    var newProxyModels = newProxyIds.Select(id => new ProxyModel(ffiProxySet[id])).ToList();

                    currentGroup.Proxies = new System.Collections.ObjectModel.ObservableCollection<ProxyModel>(newProxyModels);

                    if (newProxyIds.Any() || unrecognized > 0)
                    {
                        var unrecognizedMsg = unrecognized > 0 ? $" (skipped {unrecognized} unrecognized link)" : string.Empty;
                        UI.NotifyUser($"Imported {newProxyIds.Count} proxies.{unrecognizedMsg}", "Import proxy");
                    }
                });
            }
            catch (Exception ex)
            {
                UI.NotifyException("Importing proxies", ex);
            }
        }

        private async void ProxyGroupUnlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDialogShown) return;
            isDialogShown = true;

            var dialogResult = await ProxyGroupUnlockDialog.ShowAsync();
            isDialogShown = false;
            if (dialogResult != ContentDialogResult.Primary) return;

            SetValue(IsProxyGroupLockedProperty, false);
        }

        private void ProxyGroupShareProxyButton_Click(object sender, RoutedEventArgs e)
        {
            var linkRange = ProxyGroupProxyList.SelectedItems
                .Cast<ProxyModel>()
                .Where(model => model != null)
                .Select(model =>
                {
                    var proxy = model.Proxy;
                    IReadOnlyList<byte> proxyView = proxy;
                    return YtFlowAppBridge.Bridge.YtflowCore.ConvertDataProxyToShareLink(model.Name, proxyView, model.ProxyVersion);
                })
                .Where(link => !string.IsNullOrEmpty(link));

            var text = string.Join(Environment.NewLine, linkRange);
            ProxyGroupProxyExportText.Text = text;
            _ = ProxyGroupProxyExportDialog.ShowAsync();
        }

        private void ProxyGroupEditProxyButton_Click(object sender, RoutedEventArgs e)
        {
            var proxy = ProxyGroupProxyList.SelectedItem as ProxyModel;
            if (proxy == null) return;
            EditProxyInCurrentProxyGroup(proxy);
        }
        public async void ProxyGroupItemDuplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var targetGroup = (sender as FrameworkElement)?.DataContext as ProxyGroupModel;
                if (targetGroup == null)
                    return;

                var targetGroupId = targetGroup.Id;
                var allProxies = ProxyGroupProxyList.Items;
                var selectedItems = ProxyGroupProxyList.SelectedItems.Cast<object>().ToList();

                // Sort selected items based on their index in the allProxies list
                selectedItems.Sort((lhs, rhs) =>
                {
                    var lhsIndex = allProxies.IndexOf(lhs);
                    var rhsIndex = allProxies.IndexOf(rhs);
                    return lhsIndex.CompareTo(rhsIndex);
                });

                // Transform selected items into new proxies
                var newProxies = selectedItems
                    .Select(item => item as ProxyModel)
                    .Where(item => item != null)
                    .Select(proxy => new FfiDataProxy
                    {
                        id = BridgeExtensions.INVALID_DB_ID,
                        name = proxy.Name,
                        proxy = proxy.Proxy
                    })
                    .ToList();

                await Task.Run(async () =>
                {
                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    foreach (var proxy in newProxies)
                    {
                        try
                        {
                            proxy.id = conn.CreateProxy(targetGroupId, proxy.name, proxy.proxy, 0);
                            // TODO: order_num
                        }
                        catch (Exception)
                        {
                            UI.NotifyException("Duplicating a proxy");
                        }
                    }
                });

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var targetGroupProxies = targetGroup.Proxies;
                    var newProxyModelSize = 0;

                    foreach (var newProxy in newProxies.Where(proxy => proxy.id != BridgeExtensions.INVALID_DB_ID))
                    {
                        var newProxyModel = new ProxyModel(newProxy);
                        if (targetGroupProxies != null)
                        {
                            targetGroupProxies.Append(newProxyModel);
                        }
                        newProxyModelSize++;
                    }

                    UI.NotifyUser($"Duplicated {newProxyModelSize} proxies to {targetGroup.Name}", "Duplicate Proxies");
                });
            }
            catch (Exception)
            {
                UI.NotifyException("Duplicating proxies");
            }
        }

        public uint ProxyGroupProxySelectedCount
        {
            get => (uint)GetValue(ProxyGroupProxySelectedCountProperty);
            set => SetValue(ProxyGroupProxySelectedCountProperty, value);
        }

        public bool IsProxyGroupLocked
        {
            get => (bool)(GetValue(IsProxyGroupLockedProperty) ?? true);
            set => SetValue(IsProxyGroupLockedProperty, value);
        }
  
        public async void ProxyGroupNewProxyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isDialogShown)
                    return;

                var proxyGroup = Model.CurrentProxyGroupModel as ProxyGroupModel;
                if (proxyGroup == null)
                    return;

                var proxies = proxyGroup.Proxies;
                var existingProxyCount = proxies.Count;
                var proxyGroupId = proxyGroup.Id;

                bool isNameValid = false;
                string newProxyName = string.Empty;

                while (!isNameValid)
                {
                    isDialogShown = true;
                    ProxyGroupProxyCreateNameTextBox.Text = $"New Proxy {existingProxyCount + 1}";
                    var dialogResult = await ProxyGroupProxyCreateDialog.ShowAsync();
                    isDialogShown = false;

                    if (dialogResult != ContentDialogResult.Primary)
                        return;

                    newProxyName = ProxyGroupProxyCreateNameTextBox.Text;
                    isNameValid = !string.IsNullOrEmpty(newProxyName);
                }

                var selectedTemplate = ProxyGroupProxyCreateTemplateComboBox.SelectedItem as string ?? string.Empty;

                await Task.Run(async () =>
                {
                    FfiProxy newProxy = new FfiProxy();
                    FfiProxyDest dest = new FfiProxyDest { host = "example.com", port = 1080 };

                    switch (selectedTemplate)
                    {
                        case "SOCKS5":
                            newProxy.legs = new List<FfiProxyLeg>
                        {
                            new FfiProxyLeg { protocol = new FfiProxyProtocolSocks5(), dest = dest }
                        };
                            break;
                        case "HTTP":
                            newProxy.legs = new List<FfiProxyLeg>
                        {
                            new FfiProxyLeg { protocol = new FfiProxyProtocolHttp(), dest = dest }
                        };
                            break;
                        case "Shadowsocks":
                            var password = new byte[] { 0x70, 0x61, 0x73, 0x73, 0x77, 0x6f, 0x72, 0x64 };
                            newProxy.legs = new List<FfiProxyLeg>
                        {
                            new FfiProxyLeg
                            {
                                protocol = new FfiProxyProtocolShadowsocks { cipher = "aes-128-gcm", password = password },
                                dest = dest
                            }
                        };
                            newProxy.udp_supported = true;
                            break;
                        case "Trojan-GFW":
                            var trojanPassword = new byte[] { 0x70, 0x61, 0x73, 0x73, 0x77, 0x6f, 0x72, 0x64 };
                            newProxy.legs = new List<FfiProxyLeg>
                        {
                            new FfiProxyLeg
                            {
                                protocol = new FfiProxyProtocolTrojan { password = trojanPassword },
                                dest = dest,
                                tls = new FfiProxyTls()
                            }
                        };
                            break;
                        case "Trojan-Go":
                            var trojanGoPassword = new byte[] { 0x70, 0x61, 0x73, 0x73, 0x77, 0x6f, 0x72, 0x64 };
                            newProxy.legs = new List<FfiProxyLeg>
                        {
                            new FfiProxyLeg
                            {
                                protocol = new FfiProxyProtocolTrojan { password = trojanGoPassword },
                                dest = dest,
                                obfs = new FfiProxyObfsWebSocket { path = "/" },
                                tls = new FfiProxyTls()
                            }
                        };
                            break;
                        case "VMess + WebSocket + TLS":
                            var vmessPassword = new byte[] { 0xab, 0x94, 0xe0, 0xfc, 0x56, 0x21, 0x8e, 0xeb, 0x08, 0xb4, 0x4c, 0xce, 0xd6, 0xb3, 0xd2, 0x66 };
                            newProxy.legs = new List<FfiProxyLeg>
                        {
                            new FfiProxyLeg
                            {
                                protocol = new FfiProxyProtocolVMess { user_id = vmessPassword, alter_id = 0, security = "auto" },
                                dest = dest,
                                obfs = new FfiProxyObfsWebSocket { path = "/" },
                                tls = new FfiProxyTls()
                            }
                        };
                            break;
                        default:
                            return;
                    }
                    newProxy.name = newProxyName;
                    var proxyBuf = (newProxy).ToCbor().EncodeToBytes();
                    var dataProxyBuf = YtFlowAppBridge.Bridge.YtflowCore.ytflow_app_proxy_data_proxy_compose_v1(proxyBuf);

                    var conn = CoreInterop.BridgeExtensions.FfiDbInstance.Connect();
                    var newProxyId = conn.CreateProxy(proxyGroupId, newProxyName, dataProxyBuf, 0);

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var newProxyModel = new ProxyModel(new FfiDataProxy
                        {
                            id = newProxyId,
                            name = newProxyName,
                            order_num = 0, // TODO: order_num
                            proxy = dataProxyBuf.ToArray(),
                            proxy_version = 0
                        });

                        proxies.Append(newProxyModel);
                        EditProxyInCurrentProxyGroup(newProxyModel);
                    });
                });
            }
            catch (Exception ex)
            {
                UI.NotifyException("Creating new proxy",ex);
            }
        }

        
    }
}
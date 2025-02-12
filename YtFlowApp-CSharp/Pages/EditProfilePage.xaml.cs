using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using YtFlowApp2.Models;
using YtFlowApp2.Utils;
using TreeViewNode = Microsoft.UI.Xaml.Controls.TreeViewNode;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using TreeViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs;
using TreeViewExpandingEventArgs = Microsoft.UI.Xaml.Controls.TreeViewExpandingEventArgs;
using TreeViewCollapsedEventArgs = Microsoft.UI.Xaml.Controls.TreeViewCollapsedEventArgs;
using YtFlowApp2.Pages.PluginEditor;

using YtFlowApp2.CoreInterop;
using YtFlowApp2.Converter;

namespace YtFlowApp2.Pages
{
    public sealed partial class EditProfilePage : Page
    {
        private enum SortType
        {
            ByName,
            ByDependency,
            ByCategory,
        }

        private bool m_forceQuit = false;
        private ProfileModel m_profile = null;
        private List<EditPluginModel> m_pluginModels = new List<EditPluginModel>();
        private SortType m_sortType;

        private DispatcherTimer _depChangeTimer;

        public EditProfilePage()
        {
            this.InitializeComponent();

            m_sortType = (SortType)(ApplicationData.Current.LocalSettings.Values["YTFLOW_APP_PROFILE_EDIT_PLUGIN_SORT_TYPE"] ?? SortType.ByName);


            _depChangeTimer = new DispatcherTimer();
            _depChangeTimer.Interval = TimeSpan.FromSeconds(3); // 设置间隔为 3 秒
            _depChangeTimer.Tick += _depChangeTimer_Tick;

        }

        private void _depChangeTimer_Tick(object sender, object e)
        {

            {
                try
                {
                    RefreshTreeView();
                }
                catch (Exception ex)
                {
                    UI.NotifyException("dep change subscribe", ex);
                }
            };
        }

        private static FontWeight PluginNameFontWeight(bool isDirty)
        {
            return isDirty ? FontWeights.SemiBold : FontWeights.Normal;
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                VisualStateManager.GoToState(this, "MasterState", false);
                m_profile = e.Parameter as ProfileModel;
                ProfileNameBox.Text = m_profile.Name;
                EditorFrame.Content = null;

                await Task.Run(async () =>
                {
                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    var plugins = BridgeExtensions.GetPluginsByProfile(conn, m_profile.Id);
                    var entryPlugins = BridgeExtensions.GetEntryPluginsByProfile(conn, m_profile.Id);

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        m_pluginModels.Clear();
                        m_pluginModels.Capacity = plugins.Count;
                        m_pluginModels.AddRange(plugins.Select(p =>
                        {
                            var isEntry = entryPlugins.Any(ep => ep.id == p.id);
                            return CreateEditPluginModel(p, isEntry);
                        }));
                        RefreshTreeView();
                    });
                });
            }
            catch (Exception ex)
            {
                UI.NotifyException("EditProfilePage OnNavigateTo", ex);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            try
            {
                PluginTreeView.RootNodes.Clear();
                m_profile = null;
                m_pluginModels.Clear();
                PluginTreeView.Expanding -= PluginTreeView_ExpandingDeps;
            }
            catch (Exception ex)
            {
                UI.NotifyException("EditProfilePage OnNavigatedFrom", ex);
            }
        }


        private void CheckRenamingPlugin(EditPluginModel editPluginModel)
        {
            var name = editPluginModel.Plugin.Name;
            if (string.IsNullOrEmpty(name))
            {
                editPluginModel.HasNamingConflict = true;
                return;
            }
            foreach (var model in m_pluginModels)
            {
                if (model.Plugin.Name == name && editPluginModel != model)
                {
                    editPluginModel.HasNamingConflict = true;
                    return;
                }
            }
            editPluginModel.HasNamingConflict = false;
        }

        private EditPluginModel CreateEditPluginModel(FfiPlugin plugin, bool isEntry)
        {
            var pluginModel = new PluginModel(plugin, m_profile.Id);
            var editPluginModel = new EditPluginModel(pluginModel, isEntry);
            var editPluginModelWeak = new WeakReference<EditPluginModel>(editPluginModel);
            var weakThis = new WeakReference<EditProfilePage>(this);

            pluginModel.PropertyChanged += (sender, e) =>
            {
                try
                {
                    if (!weakThis.TryGetTarget(out var self) || !editPluginModelWeak.TryGetTarget(out var _editPluginModel))
                    {
                        return;
                    }
                    _editPluginModel.IsDirty = true;
                    if (e.PropertyName == "Name" || e.PropertyName == "Param")
                    {
                        _depChangeTimer_Tick(null, null);
                    }
                    if (e.PropertyName != "Name")
                    {
                        return;
                    }
                    self.CheckRenamingPlugin(editPluginModel);
                }
                catch (Exception ex)
                {
                    UI.NotifyException("pluginModel PropertyChanged", ex);
                }
            };
            return editPluginModel;
        }

        private async Task OnNavigatingFrom(NavigatingCancelEventArgs args)
        {
            try
            {
                var currState = AdaptiveWidthVisualStateGroup.CurrentState;
                if (currState != null && currState.Name == "DetailState")
                {
                    VisualStateManager.GoToState(this, "MasterState", true);
                    args.Cancel = true;
                    return;
                }

                await SaveProfileName();

                if (m_forceQuit)
                {
                    return;
                }

                var unsavedPluginNames = string.Join("\r\n", m_pluginModels.Where(model => model.IsDirty).Select(model => model.Plugin.Name));
                if (string.IsNullOrEmpty(unsavedPluginNames))
                {
                    return;
                }

                UnsavedPluginDialogText.Text = unsavedPluginNames;
                args.Cancel = true;
                if (await QuitWithUnsavedDialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }

                m_forceQuit = true;
                switch (args.NavigationMode)
                {
                    case NavigationMode.Back:
                        Frame.GoBack();
                        break;
                    case NavigationMode.Forward:
                        Frame.GoForward();
                        break;
                    default:
                        Frame.Navigate(args.SourcePageType, args.Parameter, args.NavigationTransitionInfo);
                        break;
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("EditProfilePage OnNavigatingFrom", ex);
            }
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

                if (EditorFrame.Content == null)
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
                UI.NotifyException("EditProfilePage AdaptiveWidth StateChange", ex);
            }
        }

        private void ProfileNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    SaveProfileName();
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Change profile name", ex);
            }
        }

        private void ProfileNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveProfileName();
            }
            catch (Exception ex)
            {
                UI.NotifyException("Submit profile name", ex);
            }
        }

        private async Task SaveProfileName()
        {
            try
            {
                var profile = m_profile;
                var newProfileName = ProfileNameBox.Text;
                if (profile == null || profile.Name == newProfileName)
                {
                    return;
                }

                await Task.Run(() =>
                {
                    var conn = BridgeExtensions.FfiDbInstance.Connect();
                    conn.UpdateProfile(profile.Id, newProfileName, profile.Locale);
                });

                profile.Name = newProfileName;
            }
            catch (Exception ex)
            {
                UI.NotifyException("Save profile name", ex);
            }
        }

        private void RefreshTreeView()
        {
            PluginTreeView.RootNodes.Clear();


            PluginTreeView.Expanding -= PluginTreeView_ExpandingDeps;

            SortByNameItem.Icon.Visibility = Visibility.Collapsed;
            SortByDependencyItem.Icon.Visibility = Visibility.Collapsed;
            SortByCategoryItem.Icon.Visibility = Visibility.Collapsed;
            switch (m_sortType)
            {
                case SortType.ByName:
                    SortByNameItem.Icon.Visibility = Visibility.Visible;
                    LoadTreeNodesByName();
                    break;
                case SortType.ByDependency:
                    SortByDependencyItem.Icon.Visibility = Visibility.Visible;
                    LoadTreeNodesByDependency();
                    break;
                case SortType.ByCategory:
                    SortByCategoryItem.Icon.Visibility = Visibility.Visible;
                    break;
            }
            this.Bindings.Update();
        }

        private void LoadTreeNodesByName()
        {
            var root = PluginTreeView.RootNodes;
            var models = m_pluginModels.OrderBy(model => model.Plugin.Name).ToList();
            foreach (var model in models)
            {
                var node = new TreeViewNode
                {
                    Content = model
                };
                root.Add(node);
            }
        }

        private void LoadTreeNodesByDependency()
        {
            var tv = PluginTreeView;

            tv.Expanding += PluginTreeView_ExpandingDeps;

            var root = tv.RootNodes;
            var models = m_pluginModels.ToList();
            var errorModels = new List<EditPluginModel>();
            var usedPlugins = new List<string>();
            int idx = 0;

            while (idx < models.Count)
            {
                var model = models[idx];
                bool hasUnrealizedChildren = false;
                try
                {
                    foreach (var dep in model.Plugin.GetDependencyPlugins())
                    {
                        hasUnrealizedChildren = true;
                        usedPlugins.Add(dep);
                    }
                }
                catch (Exception)
                {
                    errorModels.Add(model);
                    models.RemoveAt(idx);
                    continue;
                }
                if (model.IsEntry)
                {
                    var node = new TreeViewNode
                    {
                        Content = model,
                        HasUnrealizedChildren = hasUnrealizedChildren
                    };
                    root.Add(node);
                    models.RemoveAt(idx);
                }
                else
                {
                    idx++;
                }
            }

            while (usedPlugins.Count > 0)
            {
                var desiredPluginName = usedPlugins[usedPlugins.Count - 1];
                usedPlugins.RemoveAt(usedPlugins.Count - 1);
                var model = models.FirstOrDefault(m => m.Plugin.Name == desiredPluginName);
                if (model == null)
                {
                    continue;
                }
                models.Remove(model);

                try
                {
                    foreach (var dep in model.Plugin.GetDependencyPlugins())
                    {
                        usedPlugins.Add(dep);
                    }
                }
                catch (Exception ex)
                {
                    errorModels.Add(model);
                    continue;
                }
            }

            if (models.Count > 0)
            {
                var tb = new TextBlock
                {
                    Text = "Inactive Plugins"
                };
                var unusedNode = new TreeViewNode
                {
                    IsExpanded = true,
                    Content = tb
                };
                foreach (var model in models)
                {
                    var node = new TreeViewNode
                    {
                        Content = model,
                        HasUnrealizedChildren = model.Plugin.Verify().required.Count > 0
                    };
                    unusedNode.Children.Add(node);
                }
                root.Add(unusedNode);
            }
            if (errorModels.Count > 0)
            {
                var tb = new TextBlock
                {
                    Text = "Error Plugins"
                };
                var errorNode = new TreeViewNode
                {
                    IsExpanded = true,
                    Content = tb
                };
                foreach (var model in errorModels)
                {
                    var node = new TreeViewNode
                    {
                        Content = model
                    };
                    errorNode.Children.Add(node);
                }
                root.Add(errorNode);
            }
        }

        private void LoadTreeNodesByCategory()
        {
            // TODO:
        }

        private void PluginTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItem;
            var tvNode = item as TreeViewNode;
            if (tvNode == null)
            {
                return;
            }

            var editPluginModel = tvNode.Content as EditPluginModel;
            if (editPluginModel == null)
            {
                tvNode.IsExpanded = !tvNode.IsExpanded;
                return;
            }

            EditorFrame.BackStack.Clear();
            EditorFrame.Navigate(typeof(RawEditorPage), editPluginModel, new EntranceNavigationTransitionInfo());
            var currState = AdaptiveWidthVisualStateGroup.CurrentState;
            if (currState != null && currState.Name == "MasterState")
            {
                VisualStateManager.GoToState(this, "DetailState", true);
            }
        }

        private void SortByItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = sender as MenuFlyoutItem;
                SortType targetSortType;
                if (item == SortByNameItem)
                {
                    targetSortType = SortType.ByName;
                }
                else if (item == SortByDependencyItem)
                {
                    targetSortType = SortType.ByDependency;
                }
                else if (item == SortByCategoryItem)
                {
                    targetSortType = SortType.ByCategory;
                }
                else
                {
                    return;
                }
                if (targetSortType == m_sortType)
                {
                    return;
                }
                m_sortType = targetSortType;
                ApplicationData.Current.LocalSettings.Values["YTFLOW_APP_PROFILE_EDIT_PLUGIN_SORT_TYPE"] = (int)targetSortType;
                RefreshTreeView();
            }
            catch (Exception ex)
            {
                UI.NotifyException("Change sorting", ex);
            }
        }

        private void PluginTreeView_ExpandingDeps(TreeView sender, TreeViewExpandingEventArgs args)
        {
            try
            {
                var node = args.Node;
                if (!node.HasUnrealizedChildren)
                {
                    return;
                }
                var model = args.Item as EditPluginModel;
                try
                {
                    node.HasUnrealizedChildren = false;
                    foreach (var dep in model.Plugin.GetDependencyPlugins())
                    {
                        var subModel = m_pluginModels.FirstOrDefault(m => m.Plugin.Name == dep);
                        if (subModel == null)
                        {
                            continue;
                        }
                        var subNode = new TreeViewNode
                        {
                            Content = subModel,
                            HasUnrealizedChildren = subModel.Plugin.Verify().required.Count > 0
                        };
                        node.Children.Add(subNode);
                    }
                }
                catch (Exception ex)
                {
                    node.IsExpanded = false;
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Expanding", ex);
            }
        }

        private async void SetAsEntryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = (sender as FrameworkElement).DataContext as EditPluginModel;
                await Task.Run(() => model.Plugin.SetAsEntry());
                model.IsEntry = true;
                if (m_sortType == SortType.ByDependency)
                {
                    RefreshTreeView();
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Set as entry", ex);
            }
        }

        private async void DeactivateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = (sender as FrameworkElement).DataContext as EditPluginModel;
                await Task.Run(() => model.Plugin.UnsetAsEntry());
                model.IsEntry = false;
                if (m_sortType == SortType.ByDependency)
                {
                    RefreshTreeView();
                }
            }
            catch (Exception ex)
            {
                UI.NotifyException("Deactivate", ex);
            }
        }

        private void PluginTreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            try
            {
                var model = args.Item as EditPluginModel;
                if (model == null)
                {
                    return;
                }

                var node = args.Node;
                node.Children.Clear();
                node.HasUnrealizedChildren = true;
            }
            catch (Exception ex)
            {
                UI.NotifyException("Collapsed", ex);
            }
        }
        bool deleting = false;
        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

            if (deleting)
            {
                return;
            }
            deleting = true;
            try
            {
                var model = (sender as FrameworkElement).DataContext as EditPluginModel;
                ConfirmPluginDeleteDialog.Content = model;
                var ret = await ConfirmPluginDeleteDialog.ShowAsync();
                if (ret != ContentDialogResult.Primary)
                {
                    deleting = false;
                    return;
                }
                await Task.Run(() => BridgeExtensions.FfiDbInstance.Connect().DeletePlugin(model.Plugin.Id));
                deleting = false;
                ConfirmPluginDeleteDialog.Content = null;
                m_pluginModels.Remove(model);
                RefreshTreeView();
            }
            catch (Exception ex)
            {
                UI.NotifyException("Deleting", ex);
            }
        }

        private async void AddPluginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = await AddPluginDialog.ShowAsync();
                if (res != ContentDialogResult.Primary)
                {
                    return;
                }

                var pluginType = NewPluginTypeBox.SelectedValue as string;
                var pluginName = NewPluginNameText.Text;
                var ffiPlugin = new FfiPlugin
                {
                    name = pluginName,
                    desc = PluginTypeToDescConverter.DescMap[pluginType].ToString(),
                    plugin = pluginType,
                    plugin_version = 0,
                    param = new byte[] { 0xf6 } // TODO: param?
                };

                await Task.Run(() =>
                {
                    var conn = HomePage.FfiDbInstance.Connect();
                    ffiPlugin.id = conn.CreatePlugin(m_profile.Id, ffiPlugin.name, ffiPlugin.desc, ffiPlugin.plugin, ffiPlugin.plugin_version, ffiPlugin.param);
                });

                foreach (var model in m_pluginModels)
                {
                    if (model.Plugin.Name == pluginName)
                    {
                        model.HasNamingConflict = true;
                    }
                }

                var editPluginModel = CreateEditPluginModel(ffiPlugin, false);
                m_pluginModels.Add(editPluginModel);

                RefreshTreeView();
            }
            catch (Exception ex)
            {
                NotifyException("Adding", ex);
            }
        }

        private void NewPluginNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewPluginNameText.Foreground = null;
        }

        private void AddPluginDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            try
            {
                if (args.Result != ContentDialogResult.Primary)
                {
                    return;
                }
                var pluginName = NewPluginNameText.Text;
                if (string.IsNullOrEmpty(pluginName))
                {
                    NewPluginNameText.Focus(FocusState.Programmatic);
                    args.Cancel = true;
                    return;
                }
                if (m_pluginModels.Any(m => m.Plugin.Name == pluginName))
                {
                    NewPluginNameText.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                    args.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                NotifyException("Validating new plugin", ex);
            }
        }

        private void NotifyException(string message, Exception ex = null)
        {
            // Handle exception notification
        }
    }
}
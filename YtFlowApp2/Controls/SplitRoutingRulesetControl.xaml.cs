using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using YtFlowApp2.Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace YtFlowApp2.Controls
{
    public sealed partial class SplitRoutingRulesetControl : UserControl
    {
        public SplitRoutingRulesetControl()
        {
            // XAML objects should not call InitializeComponent during construction.
            // See https://github.com/microsoft/cppwinrt/tree/master/nuget#initializecomponent
            this.InitializeComponent();
        }

        // Dependency Properties
        public static readonly DependencyProperty RulesetNameProperty =
            DependencyProperty.Register(
                nameof(RulesetName),
                typeof(string),
                typeof(SplitRoutingRulesetControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CanModifyRuleListProperty =
            DependencyProperty.Register(
                nameof(CanModifyRuleList),
                typeof(bool),
                typeof(SplitRoutingRulesetControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty RuleListProperty =
            DependencyProperty.Register(
                nameof(RuleList),
                typeof(ObservableCollection<SplitRoutingRuleModel>),
                typeof(SplitRoutingRulesetControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FallbackRuleProperty =
            DependencyProperty.Register(
                nameof(FallbackRule),
                typeof(SplitRoutingRuleModel),
                typeof(SplitRoutingRulesetControl),
                new PropertyMetadata(null));

        // Properties
        public string RulesetName
        {
            get => (string)GetValue(RulesetNameProperty);
            set => SetValue(RulesetNameProperty, value);
        }

        public bool CanModifyRuleList
        {
            get => (bool)GetValue(CanModifyRuleListProperty);
            set => SetValue(CanModifyRuleListProperty, value);
        }

        public ObservableCollection<SplitRoutingRuleModel> RuleList
        {
            get => (ObservableCollection<SplitRoutingRuleModel>)GetValue(RuleListProperty);
            set => SetValue(RuleListProperty, value);
        }

        public SplitRoutingRuleModel FallbackRule
        {
            get => (SplitRoutingRuleModel)GetValue(FallbackRuleProperty);
            set => SetValue(FallbackRuleProperty, value);
        }

        // Event
        public event TypedEventHandler<SplitRoutingRulesetControl, object> RemoveRequested;

        // Event Handlers
        private void DeleteRulesetButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveRequested?.Invoke(this, null);
        }

        private void AddRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var rule = "new";
            var decision = SplitRoutingRuleDecision.Direct;
            var model = new SplitRoutingRuleModel(rule, decision);

            RuleList?.Add(model);
        }

        private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = RuleListView.SelectedItems;
            var itemsToRemove = new List<object>(selectedItems);
            foreach (var item in itemsToRemove)
            {
                if (item is SplitRoutingRuleModel model)
                {
                    RuleList?.Remove(model);
                }
            }
        }

        // Helper Method
        private ListViewSelectionMode CanModifyToListSelectionMode(bool canModify)
        {
            return canModify ? ListViewSelectionMode.Single : ListViewSelectionMode.None;
        }
    }
}

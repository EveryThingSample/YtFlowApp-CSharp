using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
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
    public sealed partial class HomeProfileControl : UserControl
    {
       
        private event EventHandler<HomeProfileControl> _connectRequested;
        private event EventHandler<HomeProfileControl> _editRequested;
        private event EventHandler<HomeProfileControl> _exportRequested;
        private event EventHandler<HomeProfileControl> _deleteRequested;



        public HomeProfileControl()
        {
            InitializeComponent();
          
        }


        public DependencyProperty ProfileProperty { get; } = DependencyProperty.Register("Profile", typeof(ProfileModel), typeof(HomeProfileControl), new PropertyMetadata(null));
        public ProfileModel Profile
        {
            get => (ProfileModel)GetValue(ProfileProperty);
            set { SetValue(ProfileProperty, value); }
        }
     

        public event EventHandler<HomeProfileControl> ConnectRequested
        {
            add => _connectRequested += value;
            remove => _connectRequested -= value;
        }

        public event EventHandler<HomeProfileControl> EditRequested
        {
            add => _editRequested += value;
            remove => _editRequested -= value;
        }

        public event EventHandler<HomeProfileControl> ExportRequested
        {
            add => _exportRequested += value;
            remove => _exportRequested -= value;
        }

        public event EventHandler<HomeProfileControl> DeleteRequested
        {
            add => _deleteRequested += value;
            remove => _deleteRequested -= value;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Bindings.Update();
            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.Symbol", "Share"))
            {
                ExportButton.Icon = new SymbolIcon(Symbol.Share);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            _connectRequested?.Invoke(this, this);
        }

        private void EditButton_Click(object sender, object e)
        {
            _editRequested?.Invoke(this, this);
        }


        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _exportRequested?.Invoke(this, this);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _deleteRequested?.Invoke(this, this);
        }


    }
}

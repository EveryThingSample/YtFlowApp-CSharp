using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YtFlowApp2.Models
{
    public class AssetModel : INotifyPropertyChanged
    {
        // Event to notify property changes
        public event PropertyChangedEventHandler PropertyChanged;

        // Private fields to store the asset model data
        private ObservableCollection<ProxyGroupModel> _proxyGroups;
        private ProxyGroupModel _currentProxyGroupModel;

        // Constructor to initialize the AssetModel
        public AssetModel()
        {
            _proxyGroups = new ObservableCollection<ProxyGroupModel>();
        }

        // Public property to get or set the collection of ProxyGroups
        public ObservableCollection<ProxyGroupModel> ProxyGroups
        {
            get => _proxyGroups;
            set
            {
                if (_proxyGroups != value)
                {
                    if (_proxyGroups != null)
                    {
                        // Remove event handler from the previous collection (if any)
                        _proxyGroups.CollectionChanged -= OnProxyGroupsChanged;
                    }

                    _proxyGroups = value;

                    // Add event handler for collection changes
                    _proxyGroups.CollectionChanged += OnProxyGroupsChanged;

                    OnPropertyChanged(nameof(ProxyGroups));
                    OnPropertyChanged(nameof(IsProxyGroupsEmpty));
                }
            }
        }

        // Method to check if ProxyGroups is empty
        public bool IsProxyGroupsEmpty => _proxyGroups == null || _proxyGroups.Count == 0;

        // Public property to get or set the current ProxyGroupModel
        public ProxyGroupModel CurrentProxyGroupModel
        {
            get => _currentProxyGroupModel;
            set
            {
                if (_currentProxyGroupModel != value)
                {
                    _currentProxyGroupModel = value;
                    OnPropertyChanged(nameof(CurrentProxyGroupModel));
                }
            }
        }

        // Method to trigger the PropertyChanged event for a given property name
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Event handler for ProxyGroups collection changes
        private void OnProxyGroupsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsProxyGroupsEmpty)); // Notify when the collection changes
        }
    }

}

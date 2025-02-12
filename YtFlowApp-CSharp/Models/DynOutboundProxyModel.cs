using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YtFlowApp2.Models
{
    public class DynOutboundProxyModel : INotifyPropertyChanged
    {
        // Event to notify property changes
        public event PropertyChangedEventHandler PropertyChanged;

        // Private fields to store the proxy model data
        private uint _idx;
        private string _name;
        private string _groupName;

        // Constructor to initialize the DynOutboundProxyModel
        public DynOutboundProxyModel(uint idx, string name, string groupName)
        {
            _idx = idx;
            _name = name;
            _groupName = groupName; 
        }

        // Public properties to get the values of the model
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        public uint Idx
        {
            get => _idx;
            set
            {
                if (_idx != value)
                {
                    _idx = value;
                    OnPropertyChanged(nameof(Idx));
                }
            }
        }

        // Method to trigger the PropertyChanged event for a given property name
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

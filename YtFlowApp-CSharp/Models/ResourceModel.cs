using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YtFlowApp2.CoreInterop;

namespace YtFlowApp2.Models
{
    public sealed class ResourceModel : INotifyPropertyChanged
    {
        private uint _id;
        private string _key;
        private string _type;
        private string _localFile;
        private string _remoteType;

        public ResourceModel()
        {
        }

        public ResourceModel(FfiResource resource)
        {
            _id = resource.id;
            _key = resource.key;
            _type = resource.type;
            _localFile = resource.local_file;
            _remoteType = resource.remote_type;
        }

        public uint Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public string Key
        {
            get => _key;
            set
            {
                if (_key != value)
                {
                    _key = value;
                    OnPropertyChanged(nameof(Key));
                }
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public string LocalFile
        {
            get => _localFile;
            set
            {
                if (_localFile != value)
                {
                    _localFile = value;
                    OnPropertyChanged(nameof(LocalFile));
                }
            }
        }

        public string RemoteType
        {
            get => _remoteType;
            set
            {
                if (_remoteType != value)
                {
                    _remoteType = value;
                    OnPropertyChanged(nameof(RemoteType));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Utils;

namespace YtFlowApp2.Models

{
    public class ProxyGroupModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private uint _id;
        public uint Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(TooltipText));
                }
            }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                    OnPropertyChanged(nameof(IsManualGroup));
                    OnPropertyChanged(nameof(IsSubscription));
                    OnPropertyChanged(nameof(DisplayType));
                    OnPropertyChanged(nameof(DisplayTypeIcon));
                    OnPropertyChanged(nameof(TooltipText));
                }
            }
        }

        public bool IsManualGroup => Type == "manual";

        public bool IsSubscription => Type == "subscription";

        public string DisplayType
        {
            get
            {
                if (IsManualGroup)
                    return "Local";
                if (IsSubscription)
                    return "Subscription";
                return Type;
            }
        }

        public string DisplayTypeIcon
        {
            get
            {
                if (Type == "subscription")
                    return "\uE8F7";
                return "\uE8B7";
            }
        }

        public string TooltipText
        {
            get
            {
                if (IsManualGroup)
                    return $"{Name} (Local)";
                else if (IsSubscription)
                    return $"{Name} (Subscription)";
                return $"{Name} ({Type})";
            }
        }

        private string _subscriptionUrl;
        public string SubscriptionUrl
        {
            get { return _subscriptionUrl; }
            set
            {
                if (_subscriptionUrl != value)
                {
                    _subscriptionUrl = value;
                    OnPropertyChanged(nameof(SubscriptionUrl));
                }
            }
        }

        private string _subscriptionUploadUsed;
        public string SubscriptionUploadUsed
        {
            get { return _subscriptionUploadUsed; }
            set
            {
                if (_subscriptionUploadUsed != value)
                {
                    _subscriptionUploadUsed = value;
                    OnPropertyChanged(nameof(SubscriptionUploadUsed));
                }
            }
        }

        private string _subscriptionDownloadUsed;
        public string SubscriptionDownloadUsed
        {
            get { return _subscriptionDownloadUsed; }
            set
            {
                if (_subscriptionDownloadUsed != value)
                {
                    _subscriptionDownloadUsed = value;
                    OnPropertyChanged(nameof(SubscriptionDownloadUsed));
                }
            }
        }

        private string _subscriptionTotalUsed;
        public string SubscriptionTotalUsed
        {
            get { return _subscriptionTotalUsed; }
            set
            {
                if (_subscriptionTotalUsed != value)
                {
                    _subscriptionTotalUsed = value;
                    OnPropertyChanged(nameof(SubscriptionTotalUsed));
                }
            }
        }

        private double _subscriptionPercentUsed;
        public double SubscriptionPercentUsed
        {
            get { return _subscriptionPercentUsed; }
            set
            {
                if (_subscriptionPercentUsed != value)
                {
                    _subscriptionPercentUsed = value;
                    OnPropertyChanged(nameof(SubscriptionPercentUsed));
                }
            }
        }

        private bool _subscriptionHasDataUsage;
        public bool SubscriptionHasDataUsage
        {
            get { return _subscriptionHasDataUsage; }
            set
            {
                if (_subscriptionHasDataUsage != value)
                {
                    _subscriptionHasDataUsage = value;
                    OnPropertyChanged(nameof(SubscriptionHasDataUsage));
                }
            }
        }

        private string _subscriptionBytesTotal;
        public string SubscriptionBytesTotal
        {
            get { return _subscriptionBytesTotal; }
            set
            {
                if (_subscriptionBytesTotal != value)
                {
                    _subscriptionBytesTotal = value;
                    OnPropertyChanged(nameof(SubscriptionBytesTotal));
                }
            }
        }

        private string _subscriptionRetrievedAt;
        public string SubscriptionRetrievedAt
        {
            get { return _subscriptionRetrievedAt; }
            set
            {
                if (_subscriptionRetrievedAt != value)
                {
                    _subscriptionRetrievedAt = value;
                    OnPropertyChanged(nameof(SubscriptionRetrievedAt));
                }
            }
        }

        private string _subscriptionExpireAt;
        public string SubscriptionExpireAt
        {
            get { return _subscriptionExpireAt; }
            set
            {
                if (_subscriptionExpireAt != value)
                {
                    _subscriptionExpireAt = value;
                    OnPropertyChanged(nameof(SubscriptionExpireAt));
                }
            }
        }

        private ObservableCollection<ProxyModel> _proxies;
        public ObservableCollection<ProxyModel> Proxies
        {
            get { return _proxies; }
            set
            {
                if (_proxies != value)
                {
                    _proxies = value;
                    OnPropertyChanged(nameof(Proxies));
                }
            }
        }

        public bool IsUpdating { get; set; }

        public ProxyGroupModel()
        {
           // Proxies = new ObservableCollection<ProxyModel>();
        }

        public ProxyGroupModel(FfiProxyGroup proxyGroup)
        {
            Id = proxyGroup.id;
            Name = proxyGroup.name;
            Type = proxyGroup.type;
          //  Proxies = new ObservableCollection<ProxyModel>();
        }

        public void AttachSubscriptionInfo(FfiProxyGroupSubscription subscription)
        {
            SubscriptionUrl = subscription.url;
            SubscriptionUploadUsed = subscription.upload_bytes_used.HasValue ? UI.HumanizeByte((ulong)subscription.upload_bytes_used) : "";
            SubscriptionDownloadUsed = subscription.download_bytes_used.HasValue ? UI.HumanizeByte(subscription.download_bytes_used.Value) : "";
            SubscriptionBytesTotal = subscription.bytes_total.HasValue ? UI.HumanizeByte(subscription.bytes_total.Value) : "";
            SubscriptionRetrievedAt = subscription.retrieved_at != null ? UI.FormatNaiveDateTime(subscription.retrieved_at) : "Never";
            SubscriptionExpireAt = subscription.expires_at != null ? UI.FormatNaiveDateTime(subscription.expires_at) : "";

            if (subscription.upload_bytes_used.HasValue && subscription.download_bytes_used.HasValue && subscription.bytes_total.HasValue)
            {
                SubscriptionHasDataUsage = true;
                SubscriptionTotalUsed = UI.HumanizeByte(subscription.upload_bytes_used.Value + subscription.download_bytes_used.Value);
                SubscriptionPercentUsed = (subscription.upload_bytes_used.Value + subscription.download_bytes_used.Value) / (double)subscription.bytes_total.Value * 100;
            }
            else
            {
                SubscriptionHasDataUsage = false;
                SubscriptionTotalUsed = "";
                SubscriptionPercentUsed = 0;
            }

            OnPropertyChanged(nameof(SubscriptionUrl));
            OnPropertyChanged(nameof(SubscriptionUploadUsed));
            OnPropertyChanged(nameof(SubscriptionDownloadUsed));
            OnPropertyChanged(nameof(SubscriptionTotalUsed));
            OnPropertyChanged(nameof(SubscriptionPercentUsed));
            OnPropertyChanged(nameof(SubscriptionHasDataUsage));
            OnPropertyChanged(nameof(SubscriptionBytesTotal));
            OnPropertyChanged(nameof(SubscriptionRetrievedAt));
            OnPropertyChanged(nameof(SubscriptionExpireAt));
        }





        protected virtual void OnPropertyChanged( string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


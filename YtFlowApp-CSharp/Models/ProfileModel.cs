using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YtFlowApp2.CoreInterop;

namespace YtFlowApp2.Models
{
    public class ProfileModel : INotifyPropertyChanged
    {
        // Event to notify property changes
        public event PropertyChangedEventHandler PropertyChanged;

        // Private fields for storing profile data
        private uint _id;
        private string _name;
        private string _locale;

        // Default constructor
        public ProfileModel() { }

        // Constructor that initializes the profile with data
        public ProfileModel(uint id, string name, string locale)
        {
            _id = id;
            _name = name;
            _locale = locale;
        }
        public ProfileModel(FfiProfile ffiProfile)
        {
            _id = ffiProfile.id;
            _name = ffiProfile.name;
            _locale = ffiProfile.locale;
        }

        // Property to get and set the ID
        public uint Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id)); // Notify that the ID has changed
                }
            }
        }

        // Property to get and set the Name
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name)); // Notify that the Name has changed
                }
            }
        }

        // Property to get and set the Locale
        public string Locale
        {
            get => _locale;
            set
            {
                if (_locale != value)
                {
                    _locale = value;
                    OnPropertyChanged(nameof(Locale)); // Notify that the Locale has changed
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YtFlowApp2.CoreInterop;

namespace YtFlowApp2.Models
{
    public class PluginModel : INotifyPropertyChanged
    {
        // Event for property change notification (used by data binding in the UI).
        public event PropertyChangedEventHandler PropertyChanged;

        // Private fields to store the plugin data.
        private uint _id;
        private uint _profileId;
        private string _name;
        private string _desc;
        private string _plugin;
        private ushort _pluginVersion;
        private List<byte> _param;

        // Constructor that initializes a PluginModel instance using an FfiPlugin and a profile ID.
        public PluginModel(FfiPlugin plugin, uint profileId)
        {
            // Ensure the plugin is not null.
            OriginalPlugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

            // Initialize the properties with the values from the FfiPlugin.
            _id = plugin.id;
            _profileId = profileId;
            _name = plugin.name;
            _desc = plugin.desc;
            _plugin = plugin.plugin;
            _pluginVersion = plugin.plugin_version;
            _param = plugin.param.ToList(); // Convert the parameter array to a list.
        }

        // Public properties for the plugin data.

        // The original FfiPlugin that the PluginModel is based on.
        public FfiPlugin OriginalPlugin;

        // The unique ID of the plugin.
        public uint Id => _id;

        // The profile ID associated with the plugin.
        public uint ProfileId => _profileId;

        // The name of the plugin.
        public string Name
        {
            get => _name;
            set
            {
                // If the name changes, update the property and notify subscribers.
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name)); // Trigger PropertyChanged event for Name.
                }
            }
        }

        // The description of the plugin.
        public string Desc
        {
            get => _desc;
            set
            {
                // If the description changes, update the property and notify subscribers.
                if (_desc != value)
                {
                    _desc = value;
                    OnPropertyChanged(nameof(Desc)); // Trigger PropertyChanged event for Desc.
                }
            }
        }

        // The plugin identifier string.
        public string Plugin
        {
            get => _plugin;
            set
            {
                // If the plugin identifier changes, update the property and notify subscribers.
                if (_plugin != value)
                {
                    _plugin = value;
                    OnPropertyChanged(nameof(Plugin)); // Trigger PropertyChanged event for Plugin.
                }
            }
        }

        // The version of the plugin.
        public ushort PluginVersion
        {
            get => _pluginVersion;
            set
            {
                // If the plugin version changes, update the property and notify subscribers.
                if (_pluginVersion != value)
                {
                    _pluginVersion = value;
                    OnPropertyChanged(nameof(PluginVersion)); // Trigger PropertyChanged event for PluginVersion.
                }
            }
        }

        // The plugin's parameters as an array of bytes.
        public byte[] Param
        {
            get => _param.ToArray(); // Return a copy of the parameter array.
            set
            {
                // If the parameters change, update the list and notify subscribers.
                if (!_param.SequenceEqual(value))
                {
                    _param = value.ToList(); // Convert the array to a list.
                    OnPropertyChanged(nameof(Param)); // Trigger PropertyChanged event for Param.
                }
            }
        }

        // Protected method to trigger the PropertyChanged event.
        protected virtual void OnPropertyChanged(string propertyName)
        {
            // Notify the subscribers (usually the UI) that a property has changed.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Method to manually trigger property change notification.
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName); // Trigger the event for the given property.
        }

        // Method to verify the plugin using its parameters.
        public FfiPluginVerifyResult Verify()
        {
            // Call the Verify method from FfiPlugin to perform plugin verification.
            return FfiPlugin.Verify(_plugin, _pluginVersion, _param.ToArray());
        }

        // Method to retrieve a list of dependent plugins.
        public HashSet<string> GetDependencyPlugins()
        {
            var ret = new HashSet<string>(); // Set to store the dependency plugin names.

            // Get the verification results for the plugin.
            var verifyRes = Verify();

            // Iterate through the required plugins.
            foreach (var req in verifyRes.required)
            {
                // Extract the plugin name (before the first dot).
                var dotPos = req.descriptor.IndexOf('.');
                if (dotPos == -1)
                {
                    dotPos = req.descriptor.Length; // If no dot is found, use the full descriptor.
                }
                ret.Add(req.descriptor.Substring(0, dotPos)); // Add the dependency plugin to the set.
            }

            return ret; // Return the set of dependent plugins.
        }

        // Method to set the plugin as an entry in the database.
        public void SetAsEntry()
        {
            try
            {
                // Connect to the database and set the plugin as an entry.
                var conn = BridgeExtensions.FfiDbInstance.Connect();
            
                conn.SetPluginAsEntry(_id, _profileId);
            }
            catch (FfiException)
            {
                // Ignore any exception that occurs if the plugin is already marked as an entry.
            }
        }

        // Method to unset the plugin as an entry in the database.
        public void UnsetAsEntry()
        {
            try
            {
                // Connect to the database and unset the plugin as an entry.
                var conn = BridgeExtensions.FfiDbInstance.Connect();
                conn.UnsetPluginAsEntry(_id, _profileId);
            }
            catch (FfiException)
            {
                // Ignore any exception that occurs if the plugin is not marked as an entry.
            }
        }

        // Method to update the plugin in the database.
        public void Update()
        {
            // Connect to the database and update the plugin's details.
            var conn = BridgeExtensions.FfiDbInstance.Connect();
            conn.UpdatePlugin(_id, _profileId, _name, _desc, _plugin, _pluginVersion, _param.ToArray());
        }
    }
}

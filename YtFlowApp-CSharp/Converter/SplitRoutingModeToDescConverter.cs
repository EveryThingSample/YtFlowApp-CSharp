using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace YtFlowApp2.Converter
{
    public class SplitRoutingModeToDescConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string mode)
            {
                switch (mode)
                {
                    case "all":
                        return "Connections to all destinations are handled by proxy forwarder.";
                    case "whitelist":
                        return "Connection requests to Chinese websites, determined by rulesets, are handled by direct forwarder. All remaining connections are handled by proxy forwarder. Also called whitelist mode.";
                    case "blacklist":
                        return "Connection requests to known, overseas websites, determined by rulesets, are handled by proxy forwarder. All remaining connections are handled by direct forwarder. Also called blacklist mode.";
                    case "overseas":
                        return "Connection requests to Chinese websites, determined by rulesets, are handled by proxy forwarder. All remaining connections are handled by direct forwarder. Suitable for overseas users.";
                    default:
                        return string.Empty;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

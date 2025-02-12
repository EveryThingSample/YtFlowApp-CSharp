using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace YtFlowApp2.Converter
{
    public class BoolToObjectConverter : IValueConverter
    {
        public object TrueObject { get; set; }
        public object FalseObject { get; set; }
        public object DefaultObject { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueObject : FalseObject;
            }
     
            return DefaultObject ?? FalseObject;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        public static Visibility ToVisibility(bool input)
        {
            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility NullabilityToVisibility(object input)
        {
            if (input is bool boolValue)
            {
                return ToVisibility(boolValue);
            }
            return ToVisibility(input != null);
        }
    }
}
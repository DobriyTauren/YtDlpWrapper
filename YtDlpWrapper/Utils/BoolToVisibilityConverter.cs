using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace YtDlpWrapper.Utils
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var show = value is bool b && (b ^ Invert);
            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value is Visibility v) ? ((v == Visibility.Visible) ^ Invert) : false;
        }
    }
}

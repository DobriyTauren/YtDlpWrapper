using Microsoft.UI.Xaml.Data;
using System;
using System.ComponentModel;
using System.Linq;
using YtDlpWrapper.Services;

namespace YtDlpWrapper.Utils
{
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";

            var resourceKey = $"{value.GetType().Name}_{value}";
            var localized = LocalizationService.GetOptionalString(resourceKey);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            var field = value.GetType().GetField(value.ToString());
            var attr = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

            return attr?.Description ?? value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}

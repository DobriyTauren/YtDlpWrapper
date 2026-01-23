using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace YtDlpWrapper.Utils
{
    public class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = new SolidColorBrush(Colors.DodgerBlue);
        public Brush FalseBrush { get; set; } = new SolidColorBrush(Colors.Gray);

        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return (b ^ Invert) ? TrueBrush : FalseBrush;

            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
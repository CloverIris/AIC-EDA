using Microsoft.UI.Xaml.Data;
using System;

namespace AIC_EDA.Converters
{
    /// <summary>
    /// 通用字符串格式化转换器（替代WPF的StringFormat）
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string format && value != null)
            {
                return string.Format("{0:" + format + "}", value);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 带前缀的字符串格式化转换器
    /// </summary>
    public class PrefixFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string format && value != null)
            {
                return string.Format(format, value);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

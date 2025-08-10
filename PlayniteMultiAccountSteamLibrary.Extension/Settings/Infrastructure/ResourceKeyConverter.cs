using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PlayniteMultiAccountSteamLibrary.Extension.Infrastructure
{
    public class ResourceKeyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var result = string.Empty;

            if (value is string resourceKey)
            {
                var resource = Application.Current.TryFindResource(resourceKey);
                if (resource is string localizedString)
                {
                    result = localizedString;
                }
            }
            else if (value != null)
            {
                result = value.ToString();
            }

            return result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
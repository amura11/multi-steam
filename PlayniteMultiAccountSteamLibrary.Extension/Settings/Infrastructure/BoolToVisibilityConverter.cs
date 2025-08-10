using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PlayniteMultiAccountSteamLibrary.Extension.Infrastructure;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool boolean && boolean) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is Visibility visibility && visibility == Visibility.Visible);
}
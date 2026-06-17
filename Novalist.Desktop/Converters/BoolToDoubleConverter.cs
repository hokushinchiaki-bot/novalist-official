using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Novalist.Desktop.Converters;

public sealed class BoolToDoubleConverter : IValueConverter
{
    public static readonly BoolToDoubleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isTrue = value is bool b && b;
        if (!isTrue)
        {
            return 0.0;
        }

        if (parameter is string paramStr)
        {
            if (string.Equals(paramStr, "Infinity", StringComparison.OrdinalIgnoreCase))
            {
                return double.PositiveInfinity;
            }
            if (double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
        }

        return double.PositiveInfinity;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

namespace ReceiptWise.App.Converters;

using System.Globalization;

public class TrendColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncrease)
            return isIncrease ? Color.FromArgb("#F44336") : Color.FromArgb("#4CAF50");

        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
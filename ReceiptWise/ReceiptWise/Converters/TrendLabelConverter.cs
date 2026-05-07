namespace ReceiptWise.App.Converters;

using System.Globalization;

public class TrendLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncrease)
            return isIncrease ? "↑ Increase" : "↓ Decrease";

        return "No change";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
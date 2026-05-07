namespace ReceiptWise.App.Converters;

using System.Globalization;

public class WarrantyStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpired)
        {
            return isExpired ? "⚠️ Warranty Expired" : "✅ Warranty Active";
        }

        return "Unknown Status";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
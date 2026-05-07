namespace ReceiptWise.App.Converters;

using System.Globalization;

public class WarrantyStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpired)
        {
            return isExpired
                ? Color.FromArgb("#F44336") // Red for expired
                : Color.FromArgb("#4CAF50"); // Green for active
        }

        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
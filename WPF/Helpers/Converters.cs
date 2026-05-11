using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Application = System.Windows.Application;

namespace EasySave.WPF.Helpers;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool flag && flag;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverse bool pour lier la 2ᵉ RadioButton (ex. langue EN, type différentiel).</summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : System.Windows.DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : System.Windows.DependencyProperty.UnsetValue;
}

public class CountToVisibilityConverter : IValueConverter
{
    public bool ShowWhenZero { get; set; }

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        int count = value is int i ? i : 0;
        bool isZero = count == 0;
        return (ShowWhenZero ? isZero : !isZero) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value as string ?? "Idle";
        return status switch
        {
            "Active" => Application.Current.Resources["AccentBrush"],
            "Done" => Application.Current.Resources["SuccessBrush"],
            "Inactive" => Application.Current.Resources["TextMutedBrush"],
            "Idle" => Application.Current.Resources["TextMutedBrush"],
            "Paused" => Application.Current.Resources["WarningBrush"],
            "Cancelled" => Application.Current.Resources["TextMutedBrush"],
            "Error" => Application.Current.Resources["ErrorBrush"],
            _ => Application.Current.Resources["TextMutedBrush"]
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BannerKindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        string kind = value as string ?? "info";
        return kind switch
        {
            "success" => Application.Current.Resources["SuccessBrush"],
            "error" => Application.Current.Resources["ErrorBrush"],
            "warning" => Application.Current.Resources["WarningBrush"],
            _ => Application.Current.Resources["AccentBrush"]
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>"1" / "2" -> chip color : full=accent, diff=secondary accent.</summary>
public class BackupTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isFull = value is bool b && b;
        return isFull
            ? Application.Current.Resources["AccentBrush"]
            : Application.Current.Resources["SecondaryAccentBrush"];
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

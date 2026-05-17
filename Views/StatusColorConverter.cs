using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1.Views
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Оплачен":
                        return new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)); // Зеленый
                    case "Просрочен":
                        return new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)); // Красный
                    case "В ожидании":
                        return new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)); // Оранжевый
                    case "Отменен":
                        return new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6)); // Серый
                    case "Ожидает":
                        return new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)); // Оранжевый
                    default:
                        return new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6)); // Серый
                }
            }
            return new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

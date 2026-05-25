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
                        return Brushes.Green;
                    case "Просрочен":
                        return Brushes.Red;
                    case "В ожидании":
                        return Brushes.Orange;
                    default:
                        return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imageName = value as string;

            // Если имя файла пустое или null, возвращаем заглушку
            if (string.IsNullOrWhiteSpace(imageName))
            {
                return new BitmapImage(new Uri("pack://application:,,,/Resources/logo.png"));
            }

            // Формируем полный Pack URI к ресурсу внутри приложения
            string fullPath = $"pack://application:,,,/Resources/{imageName}";
            
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Делаем объект неизменяемым для лучшей производительности
                return bitmap;
            }
            catch
            {
                // Если файл не найден, возвращаем заглушку
                return new BitmapImage(new Uri("pack://application:,,,/Resources/logo.png"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

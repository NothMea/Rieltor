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
                return new BitmapImage(new Uri("/Resources/logo.png", UriKind.Relative));
            }

            // Формируем полный путь к ресурсу внутри приложения
            // Предполагается, что картинки лежат в папке Resources и имеют свойство "Build Action" = Resource
            string fullPath = $"/Resources/{imageName}";
            
            try
            {
                return new BitmapImage(new Uri(fullPath, UriKind.Relative));
            }
            catch
            {
                // Если файл не найден, возвращаем заглушку
                return new BitmapImage(new Uri("/Resources/logo.png", UriKind.Relative));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PropertyEditView.xaml
    /// </summary>
    public partial class PropertyEditView : UserControl
    {
        private readonly int? _propertyId;
        private RieltorEntities _db = new RieltorEntities();
        private event Action OnDataSaved;

        public PropertyEditView(int propertyId, Action onDataSaved = null)
        {
            InitializeComponent();
            _propertyId = propertyId;
            OnDataSaved = onDataSaved;
            LoadPropertyData();
        }

        public PropertyEditView(Action onDataSaved = null)
        {
            InitializeComponent();
            _propertyId = null;
            OnDataSaved = onDataSaved;
            InitializeForNewProperty();
        }

        private void InitializeForNewProperty()
        {
            // Режим добавления нового объекта
            CmbPropertyType.SelectedIndex = 0;
            CmbStatus.SelectedIndex = 0;
        }

        private void LoadPropertyData()
        {
            var property = _db.Property.Find(_propertyId);
            if (property == null)
            {
                MessageBox.Show("Объект не найден.");
                return;
            }

            TxtAddress.Text = property.Address;
            
            // Установка типа объекта
            string[] types = { "Офис", "Склад", "Торговое помещение", "Производственное помещение" };
            int typeIndex = Array.IndexOf(types, property.PropertyType);
            if (typeIndex >= 0)
                CmbPropertyType.SelectedIndex = typeIndex;
            else
                CmbPropertyType.SelectedIndex = 0;

            TxtArea.Text = property.Area.ToString();
            TxtMonthlyRent.Text = property.MonthlyRent.ToString();

            // Установка статуса
            string[] statuses = { "Свободен", "Занят", "На обслуживании" };
            int statusIndex = Array.IndexOf(statuses, property.Status);
            if (statusIndex >= 0)
                CmbStatus.SelectedIndex = statusIndex;
            else
                CmbStatus.SelectedIndex = 0;

            TxtImagePath.Text = property.ImagePath;
            if (!string.IsNullOrEmpty(property.ImagePath))
            {
                ImgPreview.Source = new BitmapImage(new Uri($"/Resources/{property.ImagePath}", UriKind.Relative));
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp|Все файлы|*.*",
                Title = "Выберите изображение объекта"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                TxtImagePath.Text = fileName;
                
                try
                {
                    ImgPreview.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            {
                MessageBox.Show("Введите адрес объекта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtArea.Text, out decimal area) || area <= 0)
            {
                MessageBox.Show("Введите корректную площадь.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtMonthlyRent.Text, out decimal rent) || rent < 0)
            {
                MessageBox.Show("Введите корректную стоимость аренды.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Property property;
            if (_propertyId.HasValue)
            {
                // Редактирование существующего объекта
                property = _db.Property.Find(_propertyId);
                if (property == null)
                {
                    MessageBox.Show("Объект не найден.");
                    return;
                }
            }
            else
            {
                // Создание нового объекта
                property = new Property();
                _db.Property.Add(property);
            }

            property.Address = TxtAddress.Text.Trim();
            property.PropertyType = ((ComboBoxItem)CmbPropertyType.SelectedItem)?.Content?.ToString() ?? "Офис";
            property.Area = area;
            property.MonthlyRent = rent;
            property.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "Свободен";
            property.ImagePath = TxtImagePath.Text;

            _db.SaveChanges();

            MessageBox.Show($"Объект успешно {(_propertyId.HasValue ? "обновлён" : "добавлен")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            
            OnDataSaved?.Invoke();
            
            // Если это UserControl в окне, закрываем окно
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}

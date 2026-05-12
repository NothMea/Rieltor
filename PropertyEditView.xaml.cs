using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Data.Entity;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для PropertyEditView.xaml
    /// </summary>
    public partial class PropertyEditView : Window
    {
        private readonly int? _propertyId;
        private RieltorEntities _db = new RieltorEntities();
        private Property _currentProperty;

        // Конструктор для редактирования существующего объекта
        public PropertyEditView(int propertyId)
        {
            InitializeComponent();
            _propertyId = propertyId;
            LoadPropertyData();
        }

        // Конструктор для создания нового объекта
        public PropertyEditView()
        {
            InitializeComponent();
            _propertyId = null;
            _currentProperty = new Property();
        }

        private void LoadPropertyData()
        {
            _currentProperty = _db.Property.Find(_propertyId);
            if (_currentProperty == null)
            {
                MessageBox.Show("Объект недвижимости не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            TxtAddress.Text = _currentProperty.Address;
            TxtArea.Text = _currentProperty.Area.ToString();
            TxtMonthlyRent.Text = _currentProperty.MonthlyRent.ToString();


            // Установка типа объекта
            var typeItems = CmbPropertyType.Items.Cast<ComboBoxItem>().ToList();
            var typeItem = typeItems.FirstOrDefault(x => x.Content.ToString() == _currentProperty.PropertyType);
            if (typeItem != null)
                CmbPropertyType.SelectedItem = typeItem;

            // Установка статуса
            var statusItems = CmbStatus.Items.Cast<ComboBoxItem>().ToList();
            var statusItem = statusItems.FirstOrDefault(x => x.Content.ToString() == _currentProperty.Status);
            if (statusItem != null)
                CmbStatus.SelectedItem = statusItem;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            {
                MessageBox.Show("Введите адрес объекта.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbPropertyType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип объекта.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal area;
            if (!decimal.TryParse(TxtArea.Text, out area) || area <= 0)
            {
                MessageBox.Show("Введите корректную площадь.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус объекта.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal monthlyRent;
            if (!decimal.TryParse(TxtMonthlyRent.Text, out monthlyRent) || monthlyRent < 0)
            {
                MessageBox.Show("Введите корректную сумму арендной платы.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Сохранение данных
                _currentProperty.Address = TxtAddress.Text.Trim();
                _currentProperty.PropertyType = ((ComboBoxItem)CmbPropertyType.SelectedItem).Content.ToString();
                _currentProperty.Area = area;
                _currentProperty.Status = ((ComboBoxItem)CmbStatus.SelectedItem).Content.ToString();
                _currentProperty.MonthlyRent = monthlyRent;


                if (_propertyId.HasValue)
                {
                    // Обновление существующего
                    _db.Entry(_currentProperty).State = EntityState.Modified;
                }
                else
                {
                    // Добавление нового
                    _db.Property.Add(_currentProperty);
                }

                _db.SaveChanges();
                MessageBox.Show(_propertyId.HasValue ? "Объект успешно обновлён!" : "Объект успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TxtNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры и запятую/точку для десятичных чисел
            Regex regex = new Regex("[^0-9,.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        
        }
    }
}

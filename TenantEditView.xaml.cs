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

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для TenantEditView.xaml
    /// </summary>
    public partial class TenantEditView : Window
    {
        private readonly int? _tenantId;
        private RieltorEntities _db = new RieltorEntities();
        private Tenants _currentTenant;

        // Конструктор для редактирования существующего арендатора
        public TenantEditView(int tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
            LoadTenantData();
        }

        // Конструктор для создания нового арендатора
        public TenantEditView()
        {
            InitializeComponent();
            _tenantId = null;
            _currentTenant = new Tenants();
        }

        private void LoadTenantData()
        {
            _currentTenant = _db.Tenants.Find(_tenantId);
            if (_currentTenant == null)
            {
                MessageBox.Show("Арендатор не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            TxtName.Text = _currentTenant.Name;
            TxtPhone.Text = _currentTenant.Phone;
            TxtINN.Text = _currentTenant.INN;
            TxtEmail.Text = _currentTenant.Email;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите ФИО или название организации.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                MessageBox.Show("Введите телефон.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                // Простая валидация email
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(TxtEmail.Text))
                {
                    MessageBox.Show("Введите корректный email адрес.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                // Сохранение данных
                _currentTenant.Name = TxtName.Text.Trim();
                _currentTenant.Phone = TxtPhone.Text.Trim();
                _currentTenant.INN = TxtINN.Text.Trim();
                _currentTenant.Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim();

                if (_tenantId.HasValue)
                {
                    // Обновление существующего
                    _db.Entry(_currentTenant).State = System.Data.Entity.EntityState.Modified;
                }
                else
                {
                    // Добавление нового
                    _db.Tenants.Add(_currentTenant);
                }

                _db.SaveChanges();
                MessageBox.Show(_tenantId.HasValue ? "Арендатор успешно обновлён!" : "Арендатор успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void TxtPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, плюс, скобки, дефис и пробелы для телефона
            Regex regex = new Regex("[^0-9+\\-\\(\\) ]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtINN_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры для ИНН
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

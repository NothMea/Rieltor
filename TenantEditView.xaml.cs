using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для TenantEditView.xaml
    /// </summary>
    public partial class TenantEditView : Window
    {
        private readonly int? _tenantId;
        private event Action OnDataSaved;

        public TenantEditView(int tenantId, Action onDataSaved = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            OnDataSaved = onDataSaved;
            using (var db = new RieltorEntities())
            {
                LoadTenantData(db);
            }
        }

        public TenantEditView(Action onDataSaved = null)
        {
            InitializeComponent();
            _tenantId = null;
            OnDataSaved = onDataSaved;
        }

        private void LoadTenantData(RieltorEntities db)
        {
            var tenant = db.Tenants.Find(_tenantId);
            if (tenant == null)
            {
                MessageBox.Show("Арендатор не найден.");
                return;
            }

            TxtName.Text = tenant.Name;
            TxtPhone.Text = tenant.Phone;
            TxtINN.Text = tenant.INN;
            TxtEmail.Text = tenant.Email;
        }

        /// <summary>
        /// Валидация номера телефона
        /// </summary>
        private bool ValidatePhone(string phone, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(phone))
            {
                // Телефон не обязателен
                return true;
            }

            // Удаляем все допустимые символы форматирования и проверяем, остались ли только цифры
            string cleanedPhone = Regex.Replace(phone, @"[\s\+\-\(\)]", string.Empty);

            if (cleanedPhone.Length == 0)
            {
                errorMessage = "Введите корректный номер телефона.";
                return false;
            }

            // Проверяем, что после очистки остались только цифры
            if (!Regex.IsMatch(cleanedPhone, @"^\d+$"))
            {
                errorMessage = "Номер телефона должен содержать только цифры, пробелы, +, -, скобки.";
                return false;
            }

            // Проверяем минимальную и максимальную длину (учитывая разные форматы)
            if (cleanedPhone.Length < 10 || cleanedPhone.Length > 15)
            {
                errorMessage = "Номер телефона должен содержать от 10 до 15 цифр.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Валидация ИНН
        /// </summary>
        private bool ValidateINN(string inn, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(inn))
            {
                // ИНН не обязателен
                return true;
            }

            // Проверяем, что ИНН содержит только цифры
            if (!Regex.IsMatch(inn, @"^\d+$"))
            {
                errorMessage = "ИНН должен содержать только цифры.";
                return false;
            }

            // ИНН может быть 10 цифр (юридические лица) или 12 цифр (физические лица)
            if (inn.Length != 10 && inn.Length != 12)
            {
                errorMessage = "ИНН должен содержать 10 цифр (для организаций) или 12 цифр (для физических лиц).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Валидация Email
        /// </summary>
        private bool ValidateEmail(string email, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                // Email не обязателен
                return true;
            }

            // Простая проверка формата email
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                errorMessage = "Введите корректный адрес электронной почты.";
                return false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация имени
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите ФИО/Название арендатора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация телефона
            string phoneError;
            if (!ValidatePhone(TxtPhone.Text, out phoneError))
            {
                MessageBox.Show(phoneError, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPhone.Focus();
                return;
            }

            // Валидация ИНН
            string innError;
            if (!ValidateINN(TxtINN.Text, out innError))
            {
                MessageBox.Show(innError, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtINN.Focus();
                return;
            }

            // Валидация Email
            string emailError;
            if (!ValidateEmail(TxtEmail.Text, out emailError))
            {
                MessageBox.Show(emailError, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            using (var db = new RieltorEntities())
            {
                Tenants tenant;
                if (_tenantId.HasValue)
                {
                    // Редактирование существующего арендатора
                    tenant = db.Tenants.Find(_tenantId);
                    if (tenant == null)
                    {
                        MessageBox.Show("Арендатор не найден.");
                        return;
                    }
                }
                else
                {
                    // Создание нового арендатора
                    tenant = new Tenants();
                    db.Tenants.Add(tenant);
                }

                tenant.Name = TxtName.Text.Trim();
                tenant.Phone = TxtPhone.Text.Trim();
                tenant.INN = TxtINN.Text.Trim();
                tenant.Email = TxtEmail.Text.Trim();

                db.SaveChanges();

                MessageBox.Show($"Арендатор успешно {(_tenantId.HasValue ? "обновлён" : "добавлен")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                OnDataSaved?.Invoke();

                // Если это UserControl в окне, закрываем окно
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
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

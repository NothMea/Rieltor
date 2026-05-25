using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для PaymentEditWindow.xaml
    /// </summary>
    public partial class PaymentEditWindow : Window
    {
        private readonly int? _paymentId;
        private readonly Action _onSaveCallback;

        public PaymentEditWindow(int? paymentId, Action onSaveCallback = null)
        {
            InitializeComponent();
            _paymentId = paymentId;
            _onSaveCallback = onSaveCallback;
            LoadComboBoxes();
            SetDefaultValues();
            
            if (_paymentId.HasValue)
            {
                LoadPaymentData();
            }
        }

        private void LoadComboBoxes()
        {
            using (var db = new RieltorEntities())
            {
                // Загружаем активные договоры с дополнительной информацией
                // Сначала получаем данные из БД, затем формируем DisplayText в памяти
                var leasesData = db.Leases
                    .Where(l => l.Status == "Активен")
                    .OrderBy(l => l.LeaseNumber)
                    .Select(l => new 
                    {
                        LeaseID = l.LeaseID,
                        LeaseNumber = l.LeaseNumber,
                        TenantName = l.Tenants.Name,
                        PropertyAddress = l.Property.Address
                    })
                    .ToList();

                var leases = leasesData.Select(l => new LeaseDisplayItem
                {
                    LeaseID = l.LeaseID,
                    LeaseNumber = l.LeaseNumber,
                    TenantName = l.TenantName,
                    PropertyAddress = l.PropertyAddress,
                    DisplayText = $"{l.LeaseNumber} | {l.TenantName} | {l.PropertyAddress}"
                }).ToList();

                CmbLease.ItemsSource = leases;
            }
        }

        private void SetDefaultValues()
        {
            // Дата платежа по умолчанию - сегодня
            DpPaymentDate.SelectedDate = DateTime.Today;
            
            // Статус по умолчанию - "В ожидании"
            CmbStatus.SelectedIndex = 0;
        }

        private void LoadPaymentData()
        {
            if (!_paymentId.HasValue) return;

            using (var db = new RieltorEntities())
            {
                var payment = db.Payments.Find(_paymentId.Value);
                if (payment == null) return;

                CmbLease.SelectedValue = payment.LeaseID;
                DpPaymentDate.SelectedDate = payment.PaymentDate;
                TxtAmount.Text = payment.Amount.ToString();
                
                // Устанавливаем статус
                switch (payment.Status)
                {
                    case "Оплачен":
                        CmbStatus.SelectedIndex = 1;
                        break;
                    case "Просрочен":
                        CmbStatus.SelectedIndex = 2;
                        break;
                    default:
                        CmbStatus.SelectedIndex = 0;
                        break;
                }
                
                TxtNotes.Text = payment.Notes;
            }
        }

        private bool ValidateForm()
        {
            // Проверка выбора договора
            if (CmbLease.SelectedItem == null)
            {
                MessageBox.Show("Выберите договор аренды", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка даты
            if (!DpPaymentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату платежа", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка суммы
            if (string.IsNullOrWhiteSpace(TxtAmount.Text))
            {
                MessageBox.Show("Введите сумму платежа", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            decimal amount;
            if (!decimal.TryParse(TxtAmount.Text, out amount) || amount <= 0)
            {
                MessageBox.Show("Сумма должна быть положительным числом", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка максимального значения суммы (защита от случайного ввода огромных чисел)
            if (amount > 1000000000) // 1 млрд
            {
                MessageBox.Show("Сумма слишком большая. Проверьте корректность ввода.", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SavePayment()
        {
            if (!ValidateForm()) return;

            try
            {
                using (var db = new RieltorEntities())
                {
                    Payments payment;

                    if (_paymentId.HasValue)
                    {
                        // Редактирование существующего платежа
                        payment = db.Payments.Find(_paymentId.Value);
                        if (payment == null)
                        {
                            MessageBox.Show("Платеж не найден", "Ошибка", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        payment.LeaseID = (int)CmbLease.SelectedValue;
                        payment.PaymentDate = DpPaymentDate.SelectedDate.Value;
                        payment.Amount = decimal.Parse(TxtAmount.Text);
                        payment.Status = ((ComboBoxItem)CmbStatus.SelectedItem).Content.ToString();
                        payment.Notes = TxtNotes.Text.Trim();
                        
                        // Если статус изменился на "Оплачен", обновляем связанные данные
                        if (payment.Status == "Оплачен")
                        {
                            var lease = db.Leases.Find(payment.LeaseID);
                            if (lease != null)
                            {
                                var property = db.Property.Find(lease.PropertyID);
                                if (property != null)
                                {
                                    property.Status = "Сдан";
                                }
                            }
                        }
                    }
                    else
                    {
                        // Создание нового платежа
                        payment = new Payments
                        {
                            LeaseID = (int)CmbLease.SelectedValue,
                            PaymentDate = DpPaymentDate.SelectedDate.Value,
                            Amount = decimal.Parse(TxtAmount.Text),
                            Status = ((ComboBoxItem)CmbStatus.SelectedItem).Content.ToString(),
                            Notes = TxtNotes.Text.Trim()
                        };

                        db.Payments.Add(payment);
                        
                        // Если статус "Оплачен", обновляем статус объекта
                        if (payment.Status == "Оплачен")
                        {
                            var lease = db.Leases.Find(payment.LeaseID);
                            if (lease != null)
                            {
                                var property = db.Property.Find(lease.PropertyID);
                                if (property != null)
                                {
                                    property.Status = "Сдан";
                                }
                            }
                        }
                    }

                    db.SaveChanges();

                    MessageBox.Show(
                        _paymentId.HasValue ? "Платеж обновлен" : "Платеж успешно добавлен",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _onSaveCallback?.Invoke();
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SavePayment();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TxtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры и запятую/точку для дробных чисел
            Regex regex = new Regex("[^0-9,.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }

    // Класс для отображения договора в ComboBox с дополнительной информацией
    public class LeaseDisplayItem
    {
        public int LeaseID { get; set; }
        public string LeaseNumber { get; set; }
        public string TenantName { get; set; }
        public string PropertyAddress { get; set; }
        public string DisplayText { get; set; }
    }
}

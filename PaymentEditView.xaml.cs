using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для PaymentEditView.xaml
    /// </summary>
    public partial class PaymentEditView : Window
    {
        private readonly int? _paymentId;
        private readonly Action _onSaveCallback;

        public PaymentEditView(int? paymentId, Action onSaveCallback)
        {
            InitializeComponent();
            _paymentId = paymentId;
            _onSaveCallback = onSaveCallback;

            using (var db = new RieltorEntities())
            {
                LoadLeases(db);

                if (_paymentId.HasValue)
                {
                    LoadPaymentData(db);
                    Title = "Редактирование платежа";
                    LblTitle.Text = "Редактирование платежа";
                }
                else
                {
                    Title = "Добавление платежа";
                    LblTitle.Text = "Добавление платежа";
                    DpPaymentDate.SelectedDate = DateTime.Today;
                    
                    // Показываем панель информации о договоре только при редактировании
                    LeaseInfoPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadLeases(RieltorEntities db)
        {
            // Загружаем активные договоры с информацией
            var leases = db.Leases
                .Include("Tenants")
                .Include("Property")
                .Where(l => l.Status == "Активен")
                .ToList();

            // Создаем анонимный тип с отображаемой информацией
            var leaseItems = leases.Select(l => new
            {
                LeaseID = l.LeaseID,
                LeaseInfo = $"{l.LeaseNumber} - {l.Tenants.Name} ({l.Property.Address})"
            }).ToList();

            CmbLease.ItemsSource = leaseItems;
        }

        private void LoadPaymentData(RieltorEntities db)
        {
            var payment = db.Payments.Find(_paymentId);
            if (payment == null)
            {
                MessageBox.Show("Платеж не найден.");
                this.Close();
                return;
            }

            // Загрузка договора в ComboBox
            LoadLeases(db);
            
            // Выбираем текущий договор
            var leaseItems = CmbLease.ItemsSource.Cast<dynamic>().ToList();
            var selectedLease = leaseItems.FirstOrDefault(l => l.LeaseID == payment.LeaseID);
            if (selectedLease != null)
            {
                CmbLease.SelectedValue = selectedLease.LeaseID;
            }

            DpPaymentDate.SelectedDate = payment.PaymentDate;
            TxtAmount.Text = payment.Amount.ToString();
            
            // Установка статуса
            string[] statuses = { "Ожидает", "Оплачен", "Просрочен" };
            int statusIndex = Array.IndexOf(statuses, payment.Status);
            if (statusIndex >= 0)
                CmbStatus.SelectedIndex = statusIndex;

            TxtNotes.Text = payment.Notes;

            // Отображение информации о договоре
            var lease = db.Leases
                .Include("Tenants")
                .Include("Property")
                .FirstOrDefault(l => l.LeaseID == payment.LeaseID);

            if (lease != null)
            {
                TxtLeaseNumber.Text = $"№ договора: {lease.LeaseNumber}";
                TxtTenantName.Text = $"Арендатор: {lease.Tenants.Name}";
                TxtPropertyAddress.Text = $"Объект: {lease.Property.Address}";
                TxtMonthlyAmount.Text = $"Ежемесячный платёж: {lease.MonthlyAmount:N0} ₽";
            }
        }

        private void TxtAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Валидация ввода суммы - только цифры
            if (!string.IsNullOrEmpty(TxtAmount.Text))
            {
                string cleanText = new string(TxtAmount.Text.Where(c => char.IsDigit(c) || c == ',').ToArray());
                if (cleanText != TxtAmount.Text)
                {
                    TxtAmount.Text = cleanText;
                    TxtAmount.CaretIndex = cleanText.Length;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (CmbLease.SelectedValue == null)
            {
                MessageBox.Show("Выберите договор аренды.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DpPaymentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату платежа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму платежа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new RieltorEntities())
            {
                Payments payment;
                if (_paymentId.HasValue)
                {
                    payment = db.Payments.Find(_paymentId);
                    if (payment == null)
                    {
                        MessageBox.Show("Платеж не найден.");
                        return;
                    }
                }
                else
                {
                    payment = new Payments();
                    db.Payments.Add(payment);
                }

                // Получаем LeaseID из выбранного элемента
                var selectedItem = CmbLease.SelectedItem as dynamic;
                if (selectedItem != null)
                {
                    payment.LeaseID = selectedItem.LeaseID;
                }
                else
                {
                    payment.LeaseID = (int)CmbLease.SelectedValue;
                }

                payment.PaymentDate = DpPaymentDate.SelectedDate.Value;
                payment.Amount = amount;
                payment.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "Ожидает";
                payment.Notes = TxtNotes.Text.Trim();

                db.SaveChanges();

                MessageBox.Show($"Платеж успешно {(_paymentId.HasValue ? "обновлён" : "создан")}!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                _onSaveCallback?.Invoke();
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

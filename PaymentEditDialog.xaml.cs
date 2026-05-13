using System;
using System.Linq;
using System.Windows;
using WpfApp1.Views;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для PaymentEditDialog.xaml
    /// </summary>
    public partial class PaymentEditDialog : Window
    {
        private readonly int? _paymentId;

        public PaymentEditDialog()
        {
            InitializeComponent();
            _paymentId = null;
            InitializeDialog();
        }

        public PaymentEditDialog(PaymentsView.PaymentDisplay payment)
        {
            InitializeComponent();
            _paymentId = payment.PaymentID;
            InitializeDialog();
            LoadPaymentData(payment);
        }

        private void InitializeDialog()
        {
            using (var db = new RieltorEntities())
            {
                // Загрузка договоров с информацией для отображения
                var leases = db.Leases
                    .Select(l => new
                    {
                        l.LeaseID,
                        DisplayText = $"№{l.LeaseNumber} - {l.Property.Address} ({l.Tenants.Name})"
                    })
                    .ToList();

                CmbLease.ItemsSource = leases;

                // Установить дату по умолчанию
                if (!_paymentId.HasValue)
                {
                    DpPaymentDate.SelectedDate = DateTime.Today;
                    CmbStatus.SelectedIndex = 2; // "Ожидает" по умолчанию
                    Title = "Добавление платежа";
                }
                else
                {
                    Title = "Редактирование платежа";
                }
            }
        }

        private void LoadPaymentData(PaymentsView.PaymentDisplay payment)
        {
            using (var db = new RieltorEntities())
            {
                var paymentEntity = db.Payments.Find(_paymentId);
                if (paymentEntity == null)
                {
                    MessageBox.Show("Платёж не найден.");
                    DialogResult = false;
                    Close();
                    return;
                }

                CmbLease.SelectedValue = paymentEntity.LeaseID;
                DpPaymentDate.SelectedDate = paymentEntity.PaymentDate;
                TxtAmount.Text = paymentEntity.Amount.ToString();

                string[] statuses = { "Оплачен", "Просрочен", "Ожидает" };
                int statusIndex = Array.IndexOf(statuses, paymentEntity.Status);
                if (statusIndex >= 0)
                    CmbStatus.SelectedIndex = statusIndex;

                TxtNotes.Text = paymentEntity.Notes;
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

            if (!decimal.TryParse(TxtAmount.Text, out decimal amount))
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
                        MessageBox.Show("Платёж не найден.");
                        return;
                    }
                }
                else
                {
                    payment = new Payments();
                    db.Payments.Add(payment);
                }

                payment.LeaseID = (int)CmbLease.SelectedValue;
                payment.PaymentDate = DpPaymentDate.SelectedDate.Value;
                payment.Amount = amount;
                payment.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "Ожидает";
                payment.Notes = TxtNotes.Text.Trim();

                db.SaveChanges();

                MessageBox.Show($"Платёж успешно {(_paymentId.HasValue ? "обновлён" : "создан")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PaymentEditDialog.xaml
    /// </summary>
    public partial class PaymentEditDialog : UserControl
    {
        private readonly int? _paymentId;
        private event Action OnDataSaved;

        public PaymentEditDialog(Action onDataSaved = null)
        {
            InitializeComponent();
            _paymentId = null;
            OnDataSaved = onDataSaved;
            InitializeForNewPayment();
        }

        public PaymentEditDialog(int paymentId, Action onDataSaved = null)
        {
            InitializeComponent();
            _paymentId = paymentId;
            OnDataSaved = onDataSaved;
            using (var db = new RieltorEntities())
            {
                LoadPaymentData(db);
            }
        }

        private void InitializeForNewPayment()
        {
            // Загрузка списка договоров для ComboBox
            using (var db = new RieltorEntities())
            {
                var leases = db.Leases.Where(l => l.Status == "Активен").ToList();
                CmbLease.ItemsSource = leases;
            }

            DpPaymentDate.SelectedDate = DateTime.Today;
            CmbStatus.SelectedIndex = 1; // По умолчанию "В ожидании"
        }

        private void LoadPaymentData(RieltorEntities db)
        {
            var payment = db.Payments.Find(_paymentId);
            if (payment == null)
            {
                MessageBox.Show("Платеж не найден.");
                return;
            }

            // Загрузка списка всех договоров
            var leases = db.Leases.ToList();
            CmbLease.ItemsSource = leases;
            CmbLease.SelectedValue = payment.LeaseID;

            DpPaymentDate.SelectedDate = payment.PaymentDate;
            TxtAmount.Text = payment.Amount.ToString();

            // Установка статуса
            string[] statuses = { "Оплачен", "В ожидании", "Просрочен" };
            int statusIndex = Array.IndexOf(statuses, payment.Status);
            if (statusIndex >= 0)
                CmbStatus.SelectedIndex = statusIndex;
            else
                CmbStatus.SelectedIndex = 1;

            TxtNotes.Text = payment.Notes;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (CmbLease.SelectedItem == null)
            {
                MessageBox.Show("Выберите договор аренды.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DpPaymentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату платежа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new RieltorEntities())
            {
                Payments payment;
                if (_paymentId.HasValue)
                {
                    // Редактирование существующего платежа
                    payment = db.Payments.Find(_paymentId);
                    if (payment == null)
                    {
                        MessageBox.Show("Платеж не найден.");
                        return;
                    }
                }
                else
                {
                    // Создание нового платежа
                    payment = new Payments();
                    db.Payments.Add(payment);
                }

                payment.LeaseID = ((Leases)CmbLease.SelectedItem).LeaseID;
                payment.PaymentDate = DpPaymentDate.SelectedDate.Value;
                payment.Amount = amount;
                payment.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "В ожидании";
                payment.Notes = TxtNotes.Text?.Trim();

                db.SaveChanges();

                MessageBox.Show($"Платеж успешно {(_paymentId.HasValue ? "обновлён" : "добавлен")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

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

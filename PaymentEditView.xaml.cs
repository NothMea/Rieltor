using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для PaymentEditView.xaml
    /// </summary>
    public partial class PaymentEditView : Window
    {
        private readonly int? _paymentId;
        private readonly Action _onSave;

        public PaymentEditView(Action onSave)
        {
            InitializeComponent();
            _paymentId = null;
            _onSave = onSave;
            using (var db = new RieltorEntities())
            {
                LoadComboBoxes(db);
                Title = "Добавление платежа";
                LblTitle.Text = "Добавление платежа";
                DpPaymentDate.SelectedDate = DateTime.Today;
            }
        }

        public PaymentEditView(int paymentId, Action onSave) : this((int?)paymentId, onSave)
        {
        }

        public PaymentEditView(int? paymentId, Action onSave)
        {
            InitializeComponent();
            _paymentId = paymentId;
            _onSave = onSave;
            using (var db = new RieltorEntities())
            {
                LoadComboBoxes(db);

                if (_paymentId.HasValue)
                {
                    LoadPaymentData(db);
                    // При редактировании делаем ComboBox с договорами недоступным
                    CmbLease.IsEnabled = false;
                }
                else
                {
                    Title = "Добавление платежа";
                    LblTitle.Text = "Добавление платежа";
                    DpPaymentDate.SelectedDate = DateTime.Today;
                }
            }
        }

        private void LoadComboBoxes(RieltorEntities db)
        {
            // Загрузка договоров аренды
            var leases = db.Leases.ToList();
            CmbLease.ItemsSource = leases;
        }

        private void LoadPaymentData(RieltorEntities db)
        {
            var payment = db.Payments.Find(_paymentId);
            if (payment == null)
            {
                MessageBox.Show("Платёж не найден.");
                this.Close();
                return;
            }

            CmbLease.SelectedValue = payment.LeaseID;
            DpPaymentDate.SelectedDate = payment.PaymentDate;
            TxtAmount.Text = payment.Amount.ToString();

            // Установка статуса
            string[] statuses = { "Ожидает", "Подтверждён", "Отклонён" };
            int statusIndex = Array.IndexOf(statuses, payment.Status);
            if (statusIndex >= 0)
                CmbStatus.SelectedIndex = statusIndex;

            TxtNotes.Text = payment.Notes;
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
                    // Редактирование существующего платежа
                    payment = db.Payments.Find(_paymentId);
                    if (payment == null)
                    {
                        MessageBox.Show("Платёж не найден.");
                        return;
                    }
                }
                else
                {
                    // Создание нового платежа
                    payment = new Payments();
                    db.Payments.Add(payment);
                }

                payment.LeaseID = (int)CmbLease.SelectedValue;
                payment.PaymentDate = DpPaymentDate.SelectedDate.Value;
                payment.Amount = amount;
                payment.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "Ожидает";
                payment.Notes = TxtNotes.Text.Trim();

                db.SaveChanges();

                MessageBox.Show($"Платёж успешно {( _paymentId.HasValue ? "обновлён" : "создан")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                _onSave?.Invoke();
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

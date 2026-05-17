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
    public partial class PaymentEditView : UserControl
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
                    LblTitle.Text = "Редактирование платежа";
                }
                else
                {
                    LblTitle.Text = "Добавление платежа";
                    DpPaymentDate.SelectedDate = DateTime.Today;
                }
            }
        }

        private void LoadComboBoxes(RieltorEntities db)
        {
            var leases = db.Leases.ToList();
            CmbLease.ItemsSource = leases;
        }

        private void LoadPaymentData(RieltorEntities db)
        {
            var payment = db.Payments.Find(_paymentId);
            if (payment == null)
            {
                MessageBox.Show("Платёж не найден.");
                return;
            }

            CmbLease.SelectedValue = payment.LeaseID;
            DpPaymentDate.SelectedDate = payment.PaymentDate;
            TxtAmount.Text = payment.Amount.ToString();
            
            string[] statuses = { "Ожидает", "Оплачен", "Просрочен", "Отменен" };
            int statusIndex = Array.IndexOf(statuses, payment.Status);
            if (statusIndex >= 0)
                CmbStatus.SelectedIndex = statusIndex;
            
            TxtNotes.Text = payment.Notes;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
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

                MessageBox.Show($"Платёж успешно {( _paymentId.HasValue ? "обновлён" : "создан")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _onSave?.Invoke();
                
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

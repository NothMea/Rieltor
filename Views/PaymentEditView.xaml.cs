using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PaymentEditView.xaml
    /// </summary>
    public partial class PaymentEditView : UserControl
    {
        private readonly int? _paymentId;
        private readonly Action _onSaveCallback;
        private Payments _existingPayment;

        public PaymentEditView(Action onSaveCallback)
        {
            InitializeComponent();
            _onSaveCallback = onSaveCallback;
            LoadLeases();
        }

        public PaymentEditView(int paymentId, Action onSaveCallback)
        {
            InitializeComponent();
            _paymentId = paymentId;
            _onSaveCallback = onSaveCallback;
            LoadLeases();
            LoadPaymentData();
        }

        private void LoadLeases()
        {
            using (var db = new RieltorEntities())
            {
                var leases = db.Leases
                    .Include("Tenants")
                    .Include("Property")
                    .Where(l => l.Status == "Активен" || l.Status == "Завершен")
                    .ToList();

                CmbLease.ItemsSource = leases.Select(l => new
                {
                    l.LeaseID,
                    DisplayInfo = $"№{l.LeaseNumber} - {l.Tenants.Name} ({l.Property.Address})"
                }).ToList();
            }
        }

        private void LoadPaymentData()
        {
            using (var db = new RieltorEntities())
            {
                _existingPayment = db.Payments
                    .Include("Leases")
                    .FirstOrDefault(p => p.PaymentID == _paymentId);

                if (_existingPayment != null)
                {
                    CmbLease.SelectedValue = _existingPayment.LeaseID;
                    DtpPaymentDate.SelectedDate = _existingPayment.PaymentDate;
                    TxtAmount.Text = _existingPayment.Amount.ToString();
                    
                    foreach (ComboBoxItem item in CmbStatus.Items)
                    {
                        if (item.Content?.ToString() == _existingPayment.Status)
                        {
                            CmbStatus.SelectedItem = item;
                            break;
                        }
                    }
                    
                    TxtNotes.Text = _existingPayment.Notes;
                }
            }
        }

        private void TxtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateForm()
        {
            if (CmbLease.SelectedItem == null)
            {
                MessageBox.Show("Выберите договор аренды", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!DtpPaymentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату платежа", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму платежа", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус платежа", "Ошибка валидации", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                using (var db = new RieltorEntities())
                {
                    Payments payment;

                    if (_paymentId.HasValue)
                    {
                        payment = db.Payments.Find(_paymentId.Value);
                        if (payment == null)
                        {
                            MessageBox.Show("Платеж не найден", "Ошибка", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        payment = new Payments();
                        db.Payments.Add(payment);
                    }

                    payment.LeaseID = (int)CmbLease.SelectedValue;
                    payment.PaymentDate = DtpPaymentDate.SelectedDate.Value;
                    payment.Amount = decimal.Parse(TxtAmount.Text);
                    payment.Status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    payment.Notes = TxtNotes.Text;

                    db.SaveChanges();

                    MessageBox.Show($"Платеж успешно {_paymentId.HasValue ? "обновлен" : "добавлен"}!", 
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    Window.GetWindow(this)?.DialogResult = true;
                    _onSaveCallback?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}

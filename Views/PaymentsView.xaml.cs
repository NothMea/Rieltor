using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PaymentsView.xaml
    /// </summary>
    public partial class PaymentsView : UserControl
    {
        private List<PaymentDisplay> _allPayments;

        public PaymentsView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new RieltorEntities())
            {
                var payments = db.Payments
                    .Select(p => new
                    {
                        p.PaymentID,
                        p.LeaseID,
                        p.PaymentDate,
                        p.Amount,
                        p.Status,
                        LeaseNumber = p.Leases.LeaseNumber,
                        Address = p.Leases.Property.Address,
                        TenantName = p.Leases.Tenants.Name
                    })
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                _allPayments = payments.Select(p => new PaymentDisplay
                {
                    PaymentID = p.PaymentID,
                    LeaseID = p.LeaseID,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Status = p.Status,
                    LeaseNumber = p.LeaseNumber,
                    Address = p.Address,
                    TenantName = p.TenantName
                }).ToList();

                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allPayments.AsEnumerable();

            // Фильтр по статусу
            if (CmbFilterStatus.SelectedItem is System.Windows.Controls.ComboBoxItem statusItem)
            {
                string statusText = statusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(statusText) && statusText != "Все статусы")
                {
                    filtered = filtered.Where(p => p.Status == statusText);
                }
            }

            // Фильтр по датам
            if (DpFromDate.SelectedDate.HasValue)
            {
                filtered = filtered.Where(p => p.PaymentDate >= DpFromDate.SelectedDate.Value.Date);
            }

            if (DpToDate.SelectedDate.HasValue)
            {
                filtered = filtered.Where(p => p.PaymentDate <= DpToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            PaymentsDataGrid.ItemsSource = filtered.ToList();
        }

        private void BtnAddPayment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PaymentEditDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            CmbFilterStatus.SelectedIndex = 0;
            DpFromDate.SelectedDate = null;
            DpToDate.SelectedDate = null;
            LoadData();
        }

        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void EditPayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PaymentDisplay payment)
            {
                var dialog = new PaymentEditDialog(payment);
                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void DeletePayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PaymentDisplay payment)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить платёж №{payment.PaymentID} от {payment.PaymentDate:dd.MM.yyyy} на сумму {payment.Amount:N2} ₽?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new RieltorEntities())
                        {
                            var paymentEntity = db.Payments.Find(payment.PaymentID);
                            if (paymentEntity != null)
                            {
                                db.Payments.Remove(paymentEntity);
                                await Task.Run(() => db.SaveChanges());

                                MessageBox.Show("Платёж успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadData();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}

namespace WpfApp1.Views
{
    public class PaymentDisplay
    {
        public int PaymentID { get; set; }
        public int LeaseID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string LeaseNumber { get; set; }
        public string Address { get; set; }
        public string TenantName { get; set; }
    }
}

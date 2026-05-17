using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WpfApp1;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PaymentsView.xaml
    /// </summary>
    public partial class PaymentsView : UserControl
    {
        private List<Payments> _allPayments;
        private List<Payments> _filteredPayments;

        public PaymentsView()
        {
            InitializeComponent();
            LoadData();
            
            // Установить даты по умолчанию (текущий месяц)
            DpFromDate.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpToDate.SelectedDate = DateTime.Now;
        }

        private void LoadData()
        {
            using (var db = new RieltorEntities())
            {
                _allPayments = db.Payments
                    .Include("Leases")
                    .Include("Leases.Tenants")
                    .Include("Leases.Property")
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                UpdateStatistics(_allPayments);
                ApplyFilters();
            }
        }

        private void UpdateStatistics(List<Payments> payments)
        {
            var total = payments.Count;
            var paid = payments.Where(p => p.Status == "Оплачен").Sum(p => p.Amount);
            var pending = payments.Where(p => p.Status == "Ожидает").Sum(p => p.Amount);
            var overdue = payments.Where(p => p.Status == "Просрочен").Sum(p => p.Amount);

            TxtTotalPayments.Text = total.ToString();
            TxtPaidAmount.Text = $"{paid:N0} ₽";
            TxtPendingAmount.Text = $"{pending:N0} ₽";
            TxtOverdueAmount.Text = $"{overdue:N0} ₽";
        }

        private void ApplyFilters()
        {
            _filteredPayments = _allPayments;

            // Фильтр по статусу
            if (CmbStatusFilter.SelectedItem is ComboBoxItem statusItem && 
                statusItem.Content.ToString() != "Все статусы")
            {
                string status = statusItem.Content.ToString();
                _filteredPayments = _filteredPayments.Where(p => p.Status == status).ToList();
            }

            // Фильтр по дате
            if (DpFromDate.SelectedDate.HasValue)
            {
                _filteredPayments = _filteredPayments.Where(p => p.PaymentDate >= DpFromDate.SelectedDate.Value.Date).ToList();
            }

            if (DpToDate.SelectedDate.HasValue)
            {
                _filteredPayments = _filteredPayments.Where(p => p.PaymentDate <= DpToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.ToLower();
                _filteredPayments = _filteredPayments.Where(p =>
                    p.Leases?.Tenants?.Name?.ToLower().Contains(search) == true ||
                    p.Leases?.Property?.Address?.ToLower().Contains(search) == true ||
                    p.Leases?.LeaseNumber?.ToLower().Contains(search) == true ||
                    p.Notes?.ToLower().Contains(search) == true
                ).ToList();
            }

            PaymentsGrid.ItemsSource = _filteredPayments;
            UpdateStatistics(_filteredPayments);
        }

        private void BtnAddPayment_Click(object sender, RoutedEventArgs e)
        {
            var paymentEdit = new PaymentEditView(null, () => LoadData());
            
            if (paymentEdit.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                var paymentEdit = new PaymentEditView(payment.PaymentID, () => LoadData());

                if (paymentEdit.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void MarkAsPaidButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                try
                {
                    using (var db = new RieltorEntities())
                    {
                        var paymentToUpdate = db.Payments.Find(payment.PaymentID);
                        if (paymentToUpdate != null)
                        {
                            paymentToUpdate.Status = "Оплачен";
                            await Task.Run(() => db.SaveChanges());

                            MessageBox.Show($"Платеж на сумму {payment.Amount:N0} ₽ отмечен как оплаченный!", 
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении платежа: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeletePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить платеж от {payment.PaymentDate:dd.MM.yyyy} на сумму {payment.Amount:N0} ₽?\n\nЭто действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new RieltorEntities())
                        {
                            var paymentToDelete = db.Payments.Find(payment.PaymentID);
                            if (paymentToDelete != null)
                            {
                                db.Payments.Remove(paymentToDelete);
                                await Task.Run(() => db.SaveChanges());

                                MessageBox.Show("Платеж успешно удален!", "Успех", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadData();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            CmbStatusFilter.SelectedIndex = 0;
            DpFromDate.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpToDate.SelectedDate = DateTime.Now;
            TxtSearch.Text = string.Empty;
        }

        private void BtnExportCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"Платежи_{DateTime.Now:yyyy-MM-dd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveDialog.FileName);
                    MessageBox.Show("Экспорт успешно выполнен!", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            var csvContent = new System.Text.StringBuilder();
            
            // Заголовки
            csvContent.AppendLine("ID;Дата платежа;Номер договора;Арендатор;Объект;Сумма;Статус;Примечание");

            // Данные
            foreach (var payment in _filteredPayments)
            {
                string tenantName = payment.Leases?.Tenants?.Name ?? "Н/Д";
                string propertyAddress = payment.Leases?.Property?.Address ?? "Н/Д";
                string leaseNumber = payment.Leases?.LeaseNumber ?? "Н/Д";
                
                csvContent.AppendLine($"{payment.PaymentID};" +
                    $"{payment.PaymentDate:dd.MM.yyyy};" +
                    $"{leaseNumber};" +
                    $"{tenantName};" +
                    $"{propertyAddress};" +
                    $"{payment.Amount};" +
                    $"{payment.Status};" +
                    $"{(payment.Notes ?? "").Replace(";", ",")}");
            }

            System.IO.File.WriteAllText(filePath, csvContent.ToString(), 
                System.Text.Encoding.UTF8);
        }
    }
}

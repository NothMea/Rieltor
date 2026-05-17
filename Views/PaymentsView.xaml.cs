using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

                _filteredPayments = _allPayments.ToList();
                PaymentsGrid.ItemsSource = _filteredPayments;
                UpdateStatistics();
            }
        }

        private void UpdateStatistics()
        {
            var payments = _filteredPayments.Any() ? _filteredPayments : _allPayments;

            TxtTotalPayments.Text = payments.Count.ToString();

            var paid = payments.Where(p => p.Status == "Оплачен").Sum(p => p.Amount);
            var pending = payments.Where(p => p.Status == "В ожидании").Sum(p => p.Amount);
            var overdue = payments.Where(p => p.Status == "Просрочен").Sum(p => p.Amount);

            TxtPaidAmount.Text = $"{paid:N0} ₽";
            TxtPendingAmount.Text = $"{pending:N0} ₽";
            TxtOverdueAmount.Text = $"{overdue:N0} ₽";
        }

        private void ApplyFilters()
        {
            var statusFilter = (CmbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var searchText = TxtSearch.Text.ToLower();

            _filteredPayments = _allPayments.Where(p =>
            {
                var statusMatch = string.IsNullOrEmpty(statusFilter) || 
                                  statusFilter == "Все статусы" || 
                                  p.Status == statusFilter;

                var searchMatch = string.IsNullOrEmpty(searchText) ||
                                  p.Leases.LeaseNumber.ToLower().Contains(searchText) ||
                                  p.Leases.Tenants.Name.ToLower().Contains(searchText) ||
                                  p.Leases.Property.Address.ToLower().Contains(searchText) ||
                                  (p.Notes != null && p.Notes.ToLower().Contains(searchText));

                return statusMatch && searchMatch;
            }).ToList();

            PaymentsGrid.ItemsSource = _filteredPayments;
            UpdateStatistics();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var paymentEdit = new PaymentEditView(() => LoadData());
            var window = new Window
            {
                Title = "Добавление платежа",
                Width = 650,
                Height = 550,
                Content = paymentEdit,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E8F4F8")
            };

            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            CmbStatusFilter.SelectedIndex = 0;
            TxtSearch.Text = "";
            LoadData();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void EditPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                var paymentEdit = new PaymentEditView(payment.PaymentID, () => LoadData());
                var window = new Window
                {
                    Title = "Редактирование платежа",
                    Width = 650,
                    Height = 550,
                    Content = paymentEdit,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E8F4F8")
                };

                if (window.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void DeletePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить платеж №{payment.PaymentID} от {payment.PaymentDate:dd.MM.yyyy} на сумму {payment.Amount:N2} ₽?\n\nЭто действие нельзя отменить!",
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

                                MessageBox.Show("Платеж успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

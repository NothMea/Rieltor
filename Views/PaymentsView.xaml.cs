using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PaymentsView.xaml
    /// </summary>
    public partial class PaymentsView : UserControl
    {
        private List<Payments> _allPayments;

        public PaymentsView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new RieltorEntities())
            {
                // Загружаем все платежи с данными договоров (включаем навигационное свойство Leases)
                _allPayments = db.Payments
                    .Include(p => p.Leases)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                // Сбрасываем фильтры
                CmbStatusFilter.SelectedIndex = 0;

                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allPayments.AsEnumerable();

            // Фильтр по статусу
            if (CmbStatusFilter.SelectedItem is ComboBoxItem statusItem)
            {
                var statusText = statusItem.Content.ToString();
                if (statusText != "Все")
                {
                    filtered = filtered.Where(p => p.Status == statusText);
                }
            }

            DgPayments.ItemsSource = filtered.ToList();
            UpdateSummary(filtered.ToList());
        }

        private void UpdateSummary(List<Payments> payments)
        {
            TxtTotalPayments.Text = payments.Count.ToString();
            TxtTotalAmount.Text = payments.Sum(p => p.Amount).ToString("N2");
            TxtOverdueCount.Text = payments.Count(p => p.Status == "Просрочен").ToString();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnAddPayment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PaymentEditWindow(null, OnPaymentSaved);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        private void OnPaymentSaved()
        {
            LoadData();
        }

        private void BtnMarkAsPaid_Click(object sender, RoutedEventArgs e)
        {
            if (DgPayments.SelectedItem is Payments payment)
            {
                if (payment.Status == "Оплачен")
                {
                    MessageBox.Show("Этот платеж уже оплачен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Отметить платеж №{payment.PaymentID} от {payment.PaymentDate:dd.MM.yyyy} на сумму {payment.Amount:N2}₽ как оплаченный?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var db = new RieltorEntities())
                    {
                        var trackedPayment = db.Payments.Find(payment.PaymentID);
                        if (trackedPayment != null)
                        {
                            trackedPayment.Status = "Оплачен";
                            
                            // Обновляем статус объекта недвижимости на "Сдан"
                            var lease = db.Leases.Find(trackedPayment.LeaseID);
                            if (lease != null)
                            {
                                var property = db.Property.Find(lease.PropertyID);
                                if (property != null)
                                {
                                    property.Status = "Сдан";
                                }
                            }
                            
                            db.SaveChanges();
                            MessageBox.Show("Платеж отмечен как оплаченный", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                            
                            // Обновляем окно объектов, чтобы отобразить новый статус
                            PropertiesView.RefreshPropertiesView();
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите платеж в таблице", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeletePayment_Click(object sender, RoutedEventArgs e)
        {
            if (DgPayments.SelectedItem is Payments payment)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить платеж №{payment.PaymentID} от {payment.PaymentDate:dd.MM.yyyy}?\nЭто действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new RieltorEntities())
                        {
                            var trackedPayment = db.Payments.Find(payment.PaymentID);
                            if (trackedPayment != null)
                            {
                                db.Payments.Remove(trackedPayment);
                                db.SaveChanges();
                                MessageBox.Show("Платеж успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            else
            {
                MessageBox.Show("Выберите платеж в таблице", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

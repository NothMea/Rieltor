using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для LeaseEditView.xaml
    /// </summary>
    public partial class LeaseEditView : Window
    {
        private readonly int? _leaseId;
        private RieltorEntities _db = new RieltorEntities();

        public LeaseEditView(int leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            LoadLeaseData();
            LoadPayments();
        }

        public LeaseEditView() : this(null)
        {
            // Конструктор для создания нового договора
        }

        public LeaseEditView(int? leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            LoadComboBoxes();
            
            if (_leaseId.HasValue)
            {
                LoadLeaseData();
                LoadPayments();
            }
            else
            {
                Title = "Добавление договора аренды";
                // Установить даты по умолчанию
                DpStartDate.SelectedDate = DateTime.Today;
                DpEndDate.SelectedDate = DateTime.Today.AddYears(1);
                CmbStatus.SelectedIndex = 0; // "Активен" по умолчанию
            }
        }

        private void LoadComboBoxes()
        {
            // Загрузка объектов недвижимости
            var properties = _db.Property.ToList();
            CmbProperty.ItemsSource = properties;

            // Загрузка арендаторов
            var tenants = _db.Tenants.ToList();
            CmbTenant.ItemsSource = tenants;
        }

        private void LoadLeaseData()
        {
            var lease = _db.Leases.Find(_leaseId);
            if (lease == null)
            {
                MessageBox.Show("Договор не найден.");
                this.Close();
                return;
            }

            TxtLeaseNumber.Text = lease.LeaseNumber;
            CmbProperty.SelectedValue = lease.PropertyID;
            CmbTenant.SelectedValue = lease.TenantID;
            DpStartDate.SelectedDate = lease.StartDate;
            DpEndDate.SelectedDate = lease.EndDate;
            TxtMonthlyAmount.Text = lease.MonthlyAmount.ToString();
            
            // Установка статуса
            string[] statuses = { "Активен", "Завершен", "Расторгнут", "На подписании" };
            int statusIndex = Array.IndexOf(statuses, lease.Status);
            if (statusIndex >= 0)
                CmbStatus.SelectedIndex = statusIndex;
        }

        private void LoadPayments()
        {
            var payments = _db.Payments
                .Where(p => p.LeaseID == _leaseId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(10)
                .ToList();
            
            PaymentsGrid.ItemsSource = payments;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (CmbProperty.SelectedValue == null)
            {
                MessageBox.Show("Выберите объект недвижимости.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbTenant.SelectedValue == null)
            {
                MessageBox.Show("Выберите арендатора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DpStartDate.SelectedDate.HasValue || !DpEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите даты начала и окончания договора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtMonthlyAmount.Text, out decimal monthlyAmount))
            {
                MessageBox.Show("Введите корректную сумму ежемесячного платежа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Leases lease;
            if (_leaseId.HasValue)
            {
                // Редактирование существующего договора
                lease = _db.Leases.Find(_leaseId);
                if (lease == null)
                {
                    MessageBox.Show("Договор не найден.");
                    return;
                }
            }
            else
            {
                // Создание нового договора
                lease = new Leases();
                _db.Leases.Add(lease);
            }

            lease.LeaseNumber = TxtLeaseNumber.Text.Trim();
            lease.PropertyID = (int)CmbProperty.SelectedValue;
            lease.TenantID = (int)CmbTenant.SelectedValue;
            lease.StartDate = DpStartDate.SelectedDate.Value;
            lease.EndDate = DpEndDate.SelectedDate.Value;
            lease.MonthlyAmount = monthlyAmount;
            lease.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "Активен";

            _db.SaveChanges();
            
            MessageBox.Show($"Договор успешно {( _leaseId.HasValue ? "обновлён" : "создан")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
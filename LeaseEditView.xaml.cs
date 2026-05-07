using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.Entity;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для LeaseEditView.xaml
    /// </summary>
    public partial class LeaseEditView : Window
    {
        private readonly int _leaseId;
        private RieltorEntities _db = new RieltorEntities();
        private Leases _currentLease;

        public LeaseEditView(int leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            LoadComboBoxes();
            LoadLeaseData();
        }

        private void LoadComboBoxes()
        {
            // Загрузка объектов недвижимости
            CmbProperty.ItemsSource = _db.Property.ToList();
            
            // Загрузка арендаторов
            CmbTenant.ItemsSource = _db.Tenants.ToList();
        }

        private void LoadLeaseData()
        {
            _currentLease = _db.Leases.Find(_leaseId);
            if (_currentLease == null)
            {
                MessageBox.Show("Договор не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            // Заполнение полей данными
            TxtLeaseNumber.Text = _currentLease.LeaseNumber;
            CmbProperty.SelectedValue = _currentLease.PropertyID;
            CmbTenant.SelectedValue = _currentLease.TenantID;
            DpStartDate.SelectedDate = _currentLease.StartDate;
            DpEndDate.SelectedDate = _currentLease.EndDate;
            TxtMonthlyAmount.Text = _currentLease.MonthlyAmount.ToString();
            
            // Установка статуса
            var statusItems = CmbStatus.Items.Cast<ComboBoxItem>().ToList();
            var statusItem = statusItems.FirstOrDefault(x => x.Content.ToString() == _currentLease.Status);
            if (statusItem != null)
                CmbStatus.SelectedItem = statusItem;

            // Обновление информационной панели
            UpdateInfoPanel();
        }

        private void UpdateInfoPanel()
        {
            var property = CmbProperty.SelectedItem as Property;
            var tenant = CmbTenant.SelectedItem as Tenants;

            TxtPropertyInfo.Text = property != null 
                ? $"Объект: {property.Address} ({property.PropertyType}, {property.Area} м²)" 
                : "Объект не выбран";
            
            TxtTenantInfo.Text = tenant != null 
                ? $"Арендатор: {tenant.Name} (ИНН: {tenant.INN}, тел: {tenant.Phone})" 
                : "Арендатор не выбран";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(TxtLeaseNumber.Text))
            {
                MessageBox.Show("Введите номер договора.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbProperty.SelectedValue == null)
            {
                MessageBox.Show("Выберите объект недвижимости.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbTenant.SelectedValue == null)
            {
                MessageBox.Show("Выберите арендатора.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DpStartDate.SelectedDate.HasValue || !DpEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите даты начала и окончания договора.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DpStartDate.SelectedDate.Value >= DpEndDate.SelectedDate.Value)
            {
                MessageBox.Show("Дата начала должна быть раньше даты окончания.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal monthlyAmount;
            if (!decimal.TryParse(TxtMonthlyAmount.Text, out monthlyAmount) || monthlyAmount <= 0)
            {
                MessageBox.Show("Введите корректную сумму ежемесячного платежа.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус договора.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Обновление данных договора
                _currentLease.LeaseNumber = TxtLeaseNumber.Text.Trim();
                _currentLease.PropertyID = (int)CmbProperty.SelectedValue;
                _currentLease.TenantID = (int)CmbTenant.SelectedValue;
                _currentLease.StartDate = DpStartDate.SelectedDate.Value;
                _currentLease.EndDate = DpEndDate.SelectedDate.Value;
                _currentLease.MonthlyAmount = monthlyAmount;
                _currentLease.Status = ((ComboBoxItem)CmbStatus.SelectedItem).Content.ToString();

                _db.SaveChanges();
                MessageBox.Show("Договор успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TxtMonthlyAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры и запятую/точку для десятичных чисел
            Regex regex = new Regex("[^0-9,.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CmbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInfoPanel();
        }

        private void CmbTenant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInfoPanel();
        }
    }
}
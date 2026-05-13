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

        public LeaseEditView(int leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            using (var db = new RieltorEntities())
            {
                LoadLeaseData(db);
                LoadPayments(db);
            }
        }

        public LeaseEditView() : this(null)
        {
            // Конструктор для создания нового договора
        }

        public LeaseEditView(int? leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            using (var db = new RieltorEntities())
            {
                LoadComboBoxes(db);
                
                if (_leaseId.HasValue)
                {
                    LoadLeaseData(db);
                    LoadPayments(db);
                    BtnDeleteLease.Visibility = System.Windows.Visibility.Visible;
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
        }

        private void LoadComboBoxes(RieltorEntities db)
        {
            // Загрузка объектов недвижимости
            var properties = db.Property.ToList();
            CmbProperty.ItemsSource = properties;

            // Загрузка арендаторов
            var tenants = db.Tenants.ToList();
            CmbTenant.ItemsSource = tenants;
        }

        private void LoadLeaseData(RieltorEntities db)
        {
            var lease = db.Leases.Find(_leaseId);
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

        private void LoadPayments(RieltorEntities db)
        {
            var payments = db.Payments
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

            using (var db = new RieltorEntities())
            {
                Leases lease;
                if (_leaseId.HasValue)
                {
                    // Редактирование существующего договора
                    lease = db.Leases.Find(_leaseId);
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
                    db.Leases.Add(lease);
                }

                lease.LeaseNumber = TxtLeaseNumber.Text.Trim();
                lease.PropertyID = (int)CmbProperty.SelectedValue;
                lease.TenantID = (int)CmbTenant.SelectedValue;
                lease.StartDate = DpStartDate.SelectedDate.Value;
                lease.EndDate = DpEndDate.SelectedDate.Value;
                lease.MonthlyAmount = monthlyAmount;
                lease.Status = ((ComboBoxItem)CmbStatus.SelectedItem)?.Content?.ToString() ?? "Активен";

                db.SaveChanges();
                
                MessageBox.Show($"Договор успешно {( _leaseId.HasValue ? "обновлён" : "создан")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private async void BtnDeleteLease_Click(object sender, RoutedEventArgs e)
        {
            if (!_leaseId.HasValue)
                return;

            var result = MessageBox.Show(
                "Для подтверждения удаления договора введите: Удалить договор",
                "Подтверждение удаления",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            // Создаем окно для ввода подтверждения
            var inputWindow = new Window
            {
                Title = "Подтверждение удаления",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E8F4F8")
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock
            {
                Text = "Введите \"Удалить договор\":",
                Margin = new System.Windows.Thickness(10),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Grid.SetRow(textBlock, 0);
            Grid.SetColumn(textBlock, 0);
            Grid.SetColumnSpan(textBlock, 2);

            var textBox = new TextBox
            {
                Margin = new System.Windows.Thickness(10),
                Height = 30
            };
            Grid.SetRow(textBox, 1);
            Grid.SetColumn(textBox, 0);
            Grid.SetColumnSpan(textBox, 2);

            var confirmButton = new Button
            {
                Content = "Удалить",
                Width = 100,
                Margin = new System.Windows.Thickness(10),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#CD5C5C"),
                Foreground = System.Windows.Media.Brushes.White
            };
            Grid.SetRow(confirmButton, 2);
            Grid.SetColumn(confirmButton, 0);

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Margin = new System.Windows.Thickness(10),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            Grid.SetRow(cancelButton, 2);
            Grid.SetColumn(cancelButton, 1);

            bool confirmed = false;

            confirmButton.Click += (s, args) =>
            {
                if (textBox.Text == "Удалить договор")
                {
                    confirmed = true;
                    inputWindow.DialogResult = true;
                    inputWindow.Close();
                }
                else
                {
                    MessageBox.Show("Неверная строка подтверждения. Введите точно: Удалить договор", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, args) =>
            {
                inputWindow.DialogResult = false;
                inputWindow.Close();
            };

            grid.Children.Add(textBlock);
            grid.Children.Add(textBox);
            grid.Children.Add(confirmButton);
            grid.Children.Add(cancelButton);

            inputWindow.Content = grid;

            if (inputWindow.ShowDialog() != true || !confirmed)
                return;

            try
            {
                using (var db = new RieltorEntities())
                {
                    var lease = db.Leases.Find(_leaseId);
                    if (lease == null)
                    {
                        MessageBox.Show("Договор не найден.");
                        return;
                    }

                    // Удаляем связанные платежи
                    var payments = db.Payments.Where(p => p.LeaseID == _leaseId).ToList();
                    foreach (var payment in payments)
                    {
                        db.Payments.Remove(payment);
                    }

                    db.Leases.Remove(lease);
                    await Task.Run(() => db.SaveChanges());

                    MessageBox.Show("Договор успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
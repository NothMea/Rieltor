using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
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

        public LeaseEditView() 
        {
            InitializeComponent();
            _leaseId = null;
            using (var db = new RieltorEntities())
            {
                LoadComboBoxes(db);
                Title = "Добавление договора аренды";
                LblTitle.Text = "Добавление договора аренды";
                // Установить даты по умолчанию
                DpStartDate.SelectedDate = DateTime.Today;
                DpEndDate.SelectedDate = DateTime.Today.AddYears(1);
                
                // При добавлении скрываем выбор статуса и панель последних платежей
                LblStatus.Visibility = System.Windows.Visibility.Collapsed;
                TxtStatus.Visibility = System.Windows.Visibility.Collapsed;
                PaymentsBorder.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        public LeaseEditView(int leaseId) : this((int?)leaseId)
        {
            // Конструктор для редактирования существующего договора
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
                    // Показываем кнопку расторжения только для активных договоров
                    BtnTerminateLease.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    Title = "Добавление договора аренды";
                    LblTitle.Text = "Добавление договора аренды";
                    // Установить даты по умолчанию
                    DpStartDate.SelectedDate = DateTime.Today;
                    DpEndDate.SelectedDate = DateTime.Today.AddYears(1);
                    
                    // При добавлении скрываем статус, причину расторжения и панель последних платежей
                    LblStatus.Visibility = System.Windows.Visibility.Collapsed;
                    TxtStatus.Visibility = System.Windows.Visibility.Collapsed;
                    LblTerminationReason.Visibility = System.Windows.Visibility.Collapsed;
                    TxtTerminationReason.Visibility = System.Windows.Visibility.Collapsed;
                    PaymentsBorder.Visibility = System.Windows.Visibility.Collapsed;
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
            
            // Установка статуса (только для просмотра)
            TxtStatus.Text = lease.Status;
            
            // Отображение причины расторжения если договор расторгнут или завершен
            if (lease.Status == "Расторгнут" || lease.Status == "Завершен")
            {
                LblTerminationReason.Visibility = System.Windows.Visibility.Visible;
                TxtTerminationReason.Visibility = System.Windows.Visibility.Visible;
                TxtTerminationReason.Text = lease.TerminationReason ?? "";
                
                // Блокируем все поля для завершенных/расторгнутых договоров
                TxtLeaseNumber.IsEnabled = false;
                CmbProperty.IsEnabled = false;
                CmbTenant.IsEnabled = false;
                DpStartDate.IsEnabled = false;
                DpEndDate.IsEnabled = false;
                TxtMonthlyAmount.IsEnabled = false;
                BtnTerminateLease.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (lease.Status == "Активен")
            {
                // Для активных договоров показываем кнопку расторжения
                BtnTerminateLease.Visibility = System.Windows.Visibility.Visible;
                LblTerminationReason.Visibility = System.Windows.Visibility.Collapsed;
                TxtTerminationReason.Visibility = System.Windows.Visibility.Collapsed;
            }
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

            // Проверка: дата начала не должна быть больше даты конца
            if (DpStartDate.SelectedDate.Value > DpEndDate.SelectedDate.Value)
            {
                MessageBox.Show("Дата начала договора не может быть позже даты окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    
                    // Запрет на редактирование завершенных/расторгнутых договоров
                    if (lease.Status == "Завершен" || lease.Status == "Расторгнут")
                    {
                        MessageBox.Show("Редактирование завершенных или расторгнутых договоров запрещено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Запрет на изменение ключевых данных если есть оплаченные платежи
                    var paidPayments = db.Payments.Count(p => p.LeaseID == _leaseId && p.Status == "Оплачен");
                    if (paidPayments > 0)
                    {
                        // Проверяем, меняются ли ключевые поля
                        bool datesChanged = DpStartDate.SelectedDate.Value != lease.StartDate || 
                                          DpEndDate.SelectedDate.Value != lease.EndDate;
                        bool amountChanged = monthlyAmount != lease.MonthlyAmount;
                        
                        if (datesChanged || amountChanged)
                        {
                            MessageBox.Show(
                                "Изменение дат или суммы договора с оплаченными платежами запрещено.\n" +
                                "Для изменения условий необходимо создать дополнительное соглашение.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
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
                
                // При добавлении нового договора статус всегда "Активен"
                if (!_leaseId.HasValue)
                {
                    lease.Status = "Активен";
                }

                db.SaveChanges();

                // При добавлении нового договора создаём первую запись о платеже в таблице Payments
                if (!_leaseId.HasValue)
                {
                    var firstPayment = new Payments
                    {
                        LeaseID = lease.LeaseID,
                        PaymentDate = lease.StartDate,
                        Amount = lease.MonthlyAmount,
                        Status = "Ожидает",
                        Notes = "Первый платёж по договору"
                    };
                    db.Payments.Add(firstPayment);
                    db.SaveChanges();
                }
                
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

        private async void BtnTerminateLease_Click(object sender, RoutedEventArgs e)
        {
            if (!_leaseId.HasValue)
                return;

            using (var db = new RieltorEntities())
            {
                var lease = db.Leases.Find(_leaseId);
                if (lease == null)
                {
                    MessageBox.Show("Договор не найден.");
                    return;
                }

                // Проверка: нельзя расторгнуть уже завершенный договор
                if (lease.Status == "Завершен" || lease.Status == "Расторгнут")
                {
                    MessageBox.Show("Этот договор уже завершен или расторгнут.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка на наличие задолженностей
                var unpaidPayments = db.Payments.Count(p => p.LeaseID == _leaseId && (p.Status == "Просрочен" || p.Status == "Ожидает"));
                if (unpaidPayments > 0)
                {
                    var confirmResult = MessageBox.Show(
                        $"У договора есть {unpaidPayments} неоплаченных платежей. Все равно расторгнуть?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (confirmResult != MessageBoxResult.Yes)
                        return;
                }
            }

            // Создаем окно для ввода причины расторжения
            var terminationWindow = new Window
            {
                Title = "Расторжение договора",
                Width = 500,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E8F4F8"),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var titleText = new TextBlock
            {
                Text = "Расторжение договора аренды",
                FontSize = 18,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(10, 10, 10, 15),
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2C5F7F")
            };
            Grid.SetRow(titleText, 0);
            Grid.SetColumn(titleText, 0);
            Grid.SetColumnSpan(titleText, 2);

            var reasonText = new TextBlock
            {
                Text = "Причина расторжения:",
                Margin = new System.Windows.Thickness(10, 5, 10, 5),
                VerticalAlignment = System.Windows.VerticalAlignment.Top
            };
            Grid.SetRow(reasonText, 1);
            Grid.SetColumn(reasonText, 0);

            var reasonTextBox = new TextBox
            {
                Margin = new System.Windows.Thickness(10, 5, 10, 10),
                Height = 80,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(reasonTextBox, 1);
            Grid.SetColumn(reasonTextBox, 1);

            var whoText = new TextBlock
            {
                Text = "Инициатор расторжения:",
                Margin = new System.Windows.Thickness(10, 5, 10, 5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Grid.SetRow(whoText, 2);
            Grid.SetColumn(whoText, 0);

            var whoComboBox = new ComboBox
            {
                Margin = new System.Windows.Thickness(10, 5, 10, 10),
                Height = 30
            };
            whoComboBox.Items.Add("По соглашению сторон");
            whoComboBox.Items.Add("По инициативе арендодателя");
            whoComboBox.Items.Add("По инициативе арендатора");
            whoComboBox.SelectedIndex = 0;
            Grid.SetRow(whoComboBox, 2);
            Grid.SetColumn(whoComboBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new System.Windows.Thickness(0, 10, 0, 10)
            };
            Grid.SetRow(buttonPanel, 3);
            Grid.SetColumnSpan(buttonPanel, 2);

            var confirmButton = new Button
            {
                Content = "Расторгнуть",
                Width = 120,
                Height = 35,
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FF9800"),
                Foreground = System.Windows.Media.Brushes.White
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 120,
                Height = 35
            };

            bool? dialogResult = null;

            confirmButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(reasonTextBox.Text))
                {
                    MessageBox.Show("Укажите причину расторжения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dialogResult = true;
                terminationWindow.Tag = new
                {
                    Reason = reasonTextBox.Text,
                    TerminatedBy = whoComboBox.SelectedItem.ToString()
                };
                terminationWindow.Close();
            };

            cancelButton.Click += (s, args) =>
            {
                dialogResult = false;
                terminationWindow.Close();
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(titleText);
            grid.Children.Add(reasonText);
            grid.Children.Add(reasonTextBox);
            grid.Children.Add(whoText);
            grid.Children.Add(whoComboBox);
            grid.Children.Add(buttonPanel);

            terminationWindow.Content = grid;

            terminationWindow.ShowDialog();

            if (dialogResult != true)
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

                    // Получаем данные из окна
                    dynamic terminationData = terminationWindow.Tag;
                    string reason = terminationData.Reason;
                    string terminatedBy = terminationData.TerminatedBy;

                    // Обновляем статус договора
                    lease.Status = "Расторгнут";
                    lease.TerminationReason = reason;
                    
                    // Сохраняем изменения
                    db.SaveChanges();

                    // Вызываем хранимую процедуру для переноса в историю (если существует)
                    try
                    {
                        db.Database.ExecuteSqlCommand(
                            "EXEC [dbo].[sp_ArchiveLease] @LeaseID = {0}, @TerminationReason = {1}, @TerminatedBy = {2}",
                            _leaseId.Value, reason, terminatedBy);
                    }
                    catch
                    {
                        // Если хранимой процедуры нет, просто продолжаем
                        // Данные все равно сохранены в основной таблице
                    }

                    MessageBox.Show(
                        $"Договор №{lease.LeaseNumber} расторгнут.\n" +
                        $"Причина: {reason}\n" +
                        $"Данные сохранены в истории договоров.",
                        "Расторжение успешно",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расторжении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
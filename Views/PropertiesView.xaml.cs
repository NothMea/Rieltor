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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PropertiesView.xaml
    /// </summary>
    public partial class PropertiesView : UserControl
    {
        private List<DisplayProperty> _allProperties;

        public PropertiesView()
        {
            InitializeComponent();
            LoadData();
        }

        private void TerminateLeaseButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем DataContext кнопки — это объект DisplayProperty
            if (sender is FrameworkElement element && element.DataContext is DisplayProperty prop)
            {
                if (prop.ActiveLease != null)
                {
                    var leaseEdit = new LeaseEditView(prop.ActiveLease.LeaseID);
                    if (leaseEdit.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }

        private void EditPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DisplayProperty prop)
            {
                var propertyEdit = new PropertyEditView(prop.PropertyID, () => LoadData());
                var window = new Window
                {
                    Title = "Редактирование объекта недвижимости",
                    Width = 850,
                    Height = 650,
                    Content = propertyEdit,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = (Brush)new BrushConverter().ConvertFrom("#E8F4F8")
                };

                if (window.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void DeletePropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DisplayProperty prop)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить объект \"{prop.Address}\"?\n\nЭто действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new RieltorEntities())
                        {
                            var property = db.Property.Find(prop.PropertyID);
                            if (property != null)
                            {
                                // Проверяем наличие активных договоров
                                var hasActiveLeases = db.Leases.Any(l => l.PropertyID == prop.PropertyID && l.Status == "Активен");
                                if (hasActiveLeases)
                                {
                                    MessageBox.Show(
                                        "Нельзя удалить объект с активными договорами аренды.",
                                        "Ошибка",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                    return;
                                }

                                // Удаляем связанные платежи (если есть)
                                var leases = db.Leases.Where(l => l.PropertyID == prop.PropertyID).ToList();
                                foreach (var lease in leases)
                                {
                                    var payments = db.Payments.Where(p => p.LeaseID == lease.LeaseID).ToList();
                                    foreach (var payment in payments)
                                    {
                                        db.Payments.Remove(payment);
                                    }
                                    db.Leases.Remove(lease);
                                }

                                db.Property.Remove(property);
                                await Task.Run(() => db.SaveChanges());

                                MessageBox.Show("Объект успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void LoadData()
        {
            using (var db = new RieltorEntities())
            {
                // Получаем все объекты и для каждого — активный договор (если есть)
                var propertiesWithLease = db.Property.Select(p => new
                {
                    p.PropertyID,
                    p.Address,
                    p.PropertyType,
                    p.Area,
                    p.MonthlyRent,
                    p.ImagePath,
                    p.Status,
                    ActiveLease = p.Leases
                        .Where(l => l.Status == "Активен")
                        .Select(l => new
                        {
                            l.LeaseID,
                            l.LeaseNumber,
                            l.StartDate,
                            l.EndDate
                        })
                        .OrderByDescending(x => x.StartDate)
                        .FirstOrDefault()
                })
                .ToList();

                // Преобразуем в список для отображения
                var displayList = propertiesWithLease.Select(item =>
                {
                    var obj = new DisplayProperty
                    {
                        PropertyID = item.PropertyID,
                        Address = item.Address,
                        PropertyType = item.PropertyType,
                        Area = item.Area,
                        MonthlyRent = item.MonthlyRent,
                        ImagePath = GetFullImagePath(item.ImagePath),
                        Status = item.Status,
                        ActiveLease = item.ActiveLease != null ? new LeaseInfo
                        {
                            LeaseID = item.ActiveLease.LeaseID,
                            LeaseNumber = item.ActiveLease.LeaseNumber,
                            StartDate = item.ActiveLease.StartDate,
                            EndDate = item.ActiveLease.EndDate,
                            PaymentStatus = GetPaymentStatus(item.ActiveLease.LeaseID)
                        } : null
                    };

                    return obj;
                }).ToList();

                _allProperties = displayList;
                ApplyFiltersAndSort();
            }
        }

        private string GetFullImagePath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            // Если путь уже абсолютный или начинается с /Resources/, возвращаем как есть
            if (imagePath.StartsWith("/Resources/") || imagePath.StartsWith("pack://"))
                return imagePath;

            // Иначе добавляем префикс /Resources/
            return $"/Resources/{imagePath}";
        }

        private string GetPaymentStatus(int leaseId)
        {
            using (var db = new RieltorEntities())
            {
                var lease = db.Leases.Find(leaseId);
                if (lease == null)
                    return "Неизвестно";

                // Получаем все платежи по договору
                var payments = db.Payments
                    .Where(p => p.LeaseID == leaseId)
                    .ToList();

                // Если платежей нет вообще
                if (payments.Count == 0)
                {
                    // Если дата начала договора уже прошла или сегодня — значит просрочен
                    if (lease.StartDate <= DateTime.Today)
                        return "Просрочен";
                    else
                        return "Ожидает";
                }

                // Проверяем, есть ли оплаченный платеж за текущий период
                // Определяем дату следующего платежа (дата начала договора + количество полных месяцев)
                var monthsSinceStart = (DateTime.Today.Year - lease.StartDate.Year) * 12 + (DateTime.Today.Month - lease.StartDate.Month);
                var expectedPaymentDate = lease.StartDate.AddMonths(monthsSinceStart);

                // Если ожидаемая дата платежа в будущем — статус "Ожидает"
                if (expectedPaymentDate > DateTime.Today)
                    return "Ожидает";

                // Ищем платеж с датой >= expectedPaymentDate и статусом "Оплачен"
                var paidPayment = payments
                    .Where(p => p.PaymentDate >= expectedPaymentDate && p.Status == "Оплачен")
                    .OrderByDescending(p => p.PaymentDate)
                    .FirstOrDefault();

                if (paidPayment != null)
                    return "Оплачен";

                // Если платеж должен был быть (expectedPaymentDate <= Today), но не оплачен
                return "Просрочен";
            }
        }

        private void CmbSortPaymentStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void CmbSortOccupancy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void ApplyFiltersAndSort()
        {
            if (_allProperties == null)
                return;

            var filtered = _allProperties.AsEnumerable();

            // Фильтр по статусу платежа
            if (CmbSortPaymentStatus.SelectedItem is ComboBoxItem paymentStatusItem)
            {
                string paymentStatusText = paymentStatusItem.Content?.ToString();
                if (!string.IsNullOrEmpty(paymentStatusText) && paymentStatusText != "Все платежи")
                {
                    filtered = filtered.Where(p => p.ActiveLease != null && p.ActiveLease.PaymentStatus == paymentStatusText);
                }
            }

            // Фильтр по занятости
            if (CmbSortOccupancy.SelectedItem is ComboBoxItem occupancyItem)
            {
                string occupancyText = occupancyItem.Content?.ToString();
                if (!string.IsNullOrEmpty(occupancyText) && occupancyText != "Все объекты")
                {
                    if (occupancyText == "Свободные")
                    {
                        filtered = filtered.Where(p => !p.HasActiveLease);
                    }
                    else if (occupancyText == "Занятые")
                    {
                        filtered = filtered.Where(p => p.HasActiveLease);
                    }
                }
            }

            ItemsList.ItemsSource = filtered.ToList();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var propertyEdit = new PropertyEditView(() => LoadData());
            var window = new Window
            {
                Title = "Добавление объекта недвижимости",
                Width = 850,
                Height = 650,
                Content = propertyEdit,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (Brush)new BrushConverter().ConvertFrom("#E8F4F8")
            };

            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void BtnAddLease_Click(object sender, RoutedEventArgs e)
        {
            var leaseEdit = new LeaseEditView();
            if (leaseEdit.ShowDialog() == true)
            {
                LoadData();
            }
        }
    }

    // Класс для отображения
    public class DisplayProperty
    {
        public int PropertyID { get; set; }
        public string Address { get; set; }
        public string PropertyType { get; set; }
        public decimal Area { get; set; }
        public decimal MonthlyRent { get; set; }
        public string ImagePath { get; set; }
        public string Status { get; set; }
        public LeaseInfo ActiveLease { get; set; }
        public bool HasActiveLease => ActiveLease != null;
    }

    public class LeaseInfo
    {
        public int LeaseID { get; set; }
        public string LeaseNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PaymentStatus { get; set; }
    }

    // Конвертер для цвета статуса - перемещён в отдельный файл Views/StatusColorConverter.cs
}

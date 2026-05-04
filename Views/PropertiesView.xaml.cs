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
        public PropertiesView()
        {
            InitializeComponent();
            LoadData();
        }
        private void EditLeaseButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем DataContext кнопки — это объект DisplayProperty
            if (sender is FrameworkElement element && element.DataContext is DisplayProperty prop)
            {
                if (prop.ActiveLease != null)
                {

                    var leaseEdit = new LeaseEditView(prop.ActiveLease.LeaseID);
                    leaseEdit.Show(); // Откроем как отдельное окно (Window)
                }
            }
        }
        private void LoadData()
        {
            var db = RieltorEntities.GetContext();

            // Получаем все объекты и для каждого — активный договор (если есть)
            var propertiesWithLease = db.Property.Select(p => new
            {
                p.PropertyID,
                p.Address,
                p.PropertyType,
                p.Area,
                p.MonthlyRent,
                p.ImagePath,
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

            ItemsList.ItemsSource = displayList;
        }

        private string GetPaymentStatus(int leaseId)
        {
            var db = RieltorEntities.GetContext();
            var latestPayment = db.Payments
                .Where(p => p.LeaseID == leaseId)
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            if (latestPayment == null)
                return "Не оплачен";

            return latestPayment.Status; // "Оплачен", "Просрочен", "Ожидает"
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Форма добавления объекта будет реализована далее.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

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

    // Конвертер для цвета статуса
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Оплачен":
                        return Brushes.Green;
                    case "Просрочен":
                        return Brushes.Red;
                    case "Ожидает":
                        return Brushes.Orange;
                    default:
                        return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

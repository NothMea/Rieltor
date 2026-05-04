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
    /// Логика взаимодействия для DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            var db = RieltorEntities.GetContext();

            // Статистика
            var totalProperties = db.Property.Count();
            var activeLeases = db.Leases.Count(l => l.Status == "Активен");
            var overduePayments = db.Payments.Count(p => p.Status == "Просрочен");

            TxtTotalProperties.Text = totalProperties.ToString();
            TxtActiveLeases.Text = activeLeases.ToString();
            TxtOverduePayments.Text = overduePayments.ToString();

            // Уведомления
            var warnings = new List<string>();
            if (overduePayments > 0)
                warnings.Add($"Найдено {overduePayments} просроченный(х) платеж(ей). Требуется вмешательство.");

            if (db.Property.Any(p => p.Status == "Свободен" && p.MonthlyRent == 0))
                warnings.Add("Обнаружены объекты со статусом 'Свободен', но без установленной арендной платы.");

            WarningsList.ItemsSource = warnings;

            // Ближайшие события
            var upcoming = new List<string>();

            // Ближайшее окончание договора
            var nearestLease = db.Leases
                .Where(l => l.Status == "Активен")
                .OrderBy(l => l.EndDate)
                .FirstOrDefault();

            if (nearestLease != null)
            {
                upcoming.Add($"Окончание договора №{nearestLease.LeaseNumber} ({nearestLease.Property.Address}) — {nearestLease.EndDate:d}");
            }

            // Ближайший платёж
            var nextPaymentDate = DateTime.Today.AddMonths(1);
            upcoming.Add($"Планируемые платежи за {nextPaymentDate:Y}");

            UpcomingEvents.ItemsSource = upcoming;
        }
    }
}

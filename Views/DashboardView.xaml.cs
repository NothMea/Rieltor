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
            var freeProperties = db.Property.Count(p => p.Status == "Свободен");
            var totalTenants = db.Tenants.Count();

            TxtTotalProperties.Text = totalProperties.ToString();
            TxtActiveLeases.Text = activeLeases.ToString();
            TxtOverduePayments.Text = overduePayments.ToString();

            // Уведомления
            var warnings = new List<string>();
            if (overduePayments > 0)
                warnings.Add($"⚠️ Найдено {overduePayments} просроченный(х) платеж(ей). Требуется вмешательство.");

            if (freeProperties > 0)
                warnings.Add($"🏢 {freeProperties} объектов свободны — можно искать новых арендаторов.");

            // Договоры, истекающие в течение 30 дней
            var thresholdDate = DateTime.Today.AddDays(30);
            var expiringLeases = db.Leases
                .Where(l => l.Status == "Активен" && l.EndDate <= thresholdDate)
                .ToList();
            
            foreach (var lease in expiringLeases)
            {
                var daysLeft = (lease.EndDate - DateTime.Today).Days;
                warnings.Add($"📅 Договор №{lease.LeaseNumber} истекает через {daysLeft} дн. ({lease.EndDate:d})");
            }

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
                var property = db.Property.Find(nearestLease.PropertyID);
                upcoming.Add($"📋 Окончание договора №{nearestLease.LeaseNumber} ({property?.Address}) — {nearestLease.EndDate:d}");
            }

            // Ближайший платёж
            var nextPaymentDate = DateTime.Today.AddMonths(1);
            upcoming.Add($"💰 Планируемые платежи за {nextPaymentDate:Y}");

            // Дни рождения арендаторов (если есть данные)
            // Можно добавить позже при наличии поля даты рождения

            UpcomingEvents.ItemsSource = upcoming;
        }
    }
}

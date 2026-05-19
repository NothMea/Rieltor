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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для LeaseHistoryView.xaml
    /// </summary>
    public partial class LeaseHistoryView : UserControl
    {
        private DateTime? _fromDate;
        private DateTime? _toDate;

        public LeaseHistoryView()
        {
            InitializeComponent();
            LoadHistory();
        }

        private void LoadHistory()
        {
            using (var db = new RieltorEntities())
            {
                // Загружаем историю из таблицы LeaseHistory если она существует
                // Или загружаем завершенные/расторгнутые договоры из основной таблицы
                var historyQuery = db.Leases
                    .Where(l => l.Status == "Завершен" || l.Status == "Расторгнут")
                    .Select(l => new
                    {
                        l.LeaseNumber,
                        PropertyAddress = l.Property.Address,
                        City = l.Property.Address ?? "",
                        TenantName = l.Tenants.Name,
                        l.StartDate,
                        l.EndDate,
                        l.MonthlyAmount,
                        OriginalStatus = l.Status,
                        TerminationDate = (DateTime?)DateTime.Today, // Временное значение
                        TerminationReason = l.TerminationReason ?? "Не указана"
                    })
                    .OrderByDescending(l => l.EndDate)
                    .ToList();

                HistoryGrid.ItemsSource = historyQuery;
            }
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterHistory();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterHistory();
        }

        private void TxtCitySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterHistory();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            FilterHistory();
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            CmbStatusFilter.SelectedIndex = 0;
            TxtSearch.Text = "";
            TxtCitySearch.Text = "";
            DpFromDate.SelectedDate = null;
            DpToDate.SelectedDate = null;
            _fromDate = null;
            _toDate = null;
            LoadHistory();
        }

        private void FilterHistory()
        {
            if (CmbStatusFilter == null || TxtSearch == null)
                return;

            using (var db = new RieltorEntities())
            {
                var selectedStatus = (CmbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var searchText = TxtSearch.Text ?? string.Empty;
                searchText = searchText.ToLower();
                var cityText = TxtCitySearch.Text ?? string.Empty;
                cityText = cityText.ToLower();

                var query = db.Leases
                    .Where(l => l.Status == "Завершен" || l.Status == "Расторгнут");

                // Фильтр по статусу
                if (selectedStatus != null && selectedStatus != "Все статусы")
                {
                    query = query.Where(l => l.Status == selectedStatus);
                }

                // Поиск по номеру договора или арендатору
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(l => 
                        l.LeaseNumber.ToLower().Contains(searchText) ||
                        l.Tenants.Name.ToLower().Contains(searchText));
                }

                // Поиск по городу (в части адреса)
                if (!string.IsNullOrWhiteSpace(cityText))
                {
                    query = query.Where(l => l.Property.Address != null && l.Property.Address.ToLower().Contains(cityText));
                }

                // Фильтр по диапазону дат
                if (DpFromDate.SelectedDate.HasValue)
                {
                    _fromDate = DpFromDate.SelectedDate.Value.Date;
                    query = query.Where(l => l.EndDate >= _fromDate.Value);
                }

                if (DpToDate.SelectedDate.HasValue)
                {
                    _toDate = DpToDate.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // Конец дня
                    query = query.Where(l => l.EndDate <= _toDate.Value);
                }

                var historyQuery = query
                    .Select(l => new
                    {
                        l.LeaseNumber,
                        PropertyAddress = l.Property.Address,
                        City = l.Property.Address ?? "",
                        TenantName = l.Tenants.Name,
                        l.StartDate,
                        l.EndDate,
                        l.MonthlyAmount,
                        OriginalStatus = l.Status,
                        TerminationDate = (DateTime?)DateTime.Today,
                        TerminationReason = l.TerminationReason ?? "Не указана"
                    })
                    .OrderByDescending(l => l.EndDate)
                    .ToList();

                HistoryGrid.ItemsSource = historyQuery;
            }
        }
    }
}

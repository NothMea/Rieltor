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
    /// Логика взаимодействия для TenantsView.xaml
    /// </summary>
    public partial class TenantsView : UserControl
    {
        public TenantsView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new RieltorEntities())
            {
                var tenants = db.Tenants.ToList();
                TenantsGrid.ItemsSource = tenants;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var tenantEdit = new TenantEditView(() => LoadData());
            var window = new Window
            {
                Title = "Добавление арендатора",
                Width = 600,
                Height = 500,
                Content = tenantEdit,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (Brush)new BrushConverter().ConvertFrom("#E8F4F8")
            };

            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditTenantButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Tenants tenant)
            {
                var tenantEdit = new TenantEditView(tenant.TenantID, () => LoadData());
                var window = new Window
                {
                    Title = "Редактирование арендатора",
                    Width = 600,
                    Height = 500,
                    Content = tenantEdit,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = (Brush)new BrushConverter().ConvertFrom("#E8F4F8")
                };

                if (window.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void DeleteTenantButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Tenants tenant)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить арендатора \"{tenant.Name}\"?\n\nЭто действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new RieltorEntities())
                        {
                            // Проверяем наличие активных договоров
                            var hasActiveLeases = db.Leases.Any(l => l.TenantID == tenant.TenantID && l.Status == "Активен");
                            if (hasActiveLeases)
                            {
                                MessageBox.Show(
                                    "Нельзя удалить арендатора с активными договорами аренды.\nСначала завершите или удалите все активные договоры.",
                                    "Ошибка",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }

                            var tenantToDelete = db.Tenants.Find(tenant.TenantID);
                            if (tenantToDelete != null)
                            {
                                // Удаляем связанные договоры (если есть неактивные)
                                var leases = db.Leases.Where(l => l.TenantID == tenant.TenantID).ToList();
                                foreach (var lease in leases)
                                {
                                    var payments = db.Payments.Where(p => p.LeaseID == lease.LeaseID).ToList();
                                    foreach (var payment in payments)
                                    {
                                        db.Payments.Remove(payment);
                                    }
                                    db.Leases.Remove(lease);
                                }

                                db.Tenants.Remove(tenantToDelete);
                                await Task.Run(() => db.SaveChanges());

                                MessageBox.Show("Арендатор успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}

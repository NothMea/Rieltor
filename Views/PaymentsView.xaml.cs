using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1.Views
{
    /// <summary>
    /// Логика взаимодействия для PaymentsView.xaml
    /// </summary>
    public partial class PaymentsView : UserControl
    {
        public PaymentsView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new RieltorEntities())
            {
                var payments = db.Payments.ToList();
                PaymentsGrid.ItemsSource = payments;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var paymentEdit = new PaymentEditDialog(() => LoadData());
            var window = new Window
            {
                Title = "Добавление платежа",
                Width = 500,
                Height = 450,
                Content = paymentEdit,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (Brush)new BrushConverter().ConvertFrom("#E8F4F8")
            };

            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                var paymentEdit = new PaymentEditDialog(payment.PaymentID, () => LoadData());
                var window = new Window
                {
                    Title = "Редактирование платежа",
                    Width = 500,
                    Height = 450,
                    Content = paymentEdit,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = (Brush)new BrushConverter().ConvertFrom("#E8F4F8")
                };

                if (window.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void DeletePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Payments payment)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить платеж №{payment.PaymentID}?\n\nЭто действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new RieltorEntities())
                        {
                            var paymentToDelete = db.Payments.Find(payment.PaymentID);
                            if (paymentToDelete != null)
                            {
                                db.Payments.Remove(paymentToDelete);
                                await Task.Run(() => db.SaveChanges());

                                MessageBox.Show("Платеж успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

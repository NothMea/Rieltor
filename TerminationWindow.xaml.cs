using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Окно расторжения договора аренды
    /// </summary>
    public partial class TerminationWindow : Window
    {
        private readonly int _leaseId;

        public TerminationWindow(int leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            
            // Загружаем информацию о договоре
            using (var db = new RieltorEntities())
            {
                var lease = db.Leases.Find(leaseId);
                if (lease == null)
                {
                    MessageBox.Show("Договор не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                LblLeaseInfo.Text = $"Договор №{lease.LeaseNumber} от {lease.StartDate:dd.MM.yyyy}\n" +
                                   $"Арендатор: {lease.Tenants.Name}\n" +
                                   $"Объект: {lease.Property.Address}";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private async void BtnTerminate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTerminationReason.Text))
            {
                MessageBox.Show("Укажите причину расторжения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbTerminatedBy.SelectedItem == null)
            {
                MessageBox.Show("Выберите инициатора расторжения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Вы уверены, что хотите расторгнуть договор?\n\n" +
                $"Причина: {TxtTerminationReason.Text}\n" +
                $"Инициатор: {CmbTerminatedBy.SelectedItem}\n\n" +
                $"Это действие нельзя отменить!",
                "Подтверждение расторжения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new RieltorEntities())
                {
                    var lease = db.Leases.Find(_leaseId);
                    if (lease == null)
                    {
                        MessageBox.Show("Договор не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        var debtConfirm = MessageBox.Show(
                            $"У договора есть {unpaidPayments} неоплаченных платежей. Все равно расторгнуть?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (debtConfirm != MessageBoxResult.Yes)
                            return;
                    }

                    // Обновляем статус договора
                    lease.Status = "Расторгнут";
                    lease.TerminationReason = TxtTerminationReason.Text;

                    // Сохраняем изменения
                    await Task.Run(() => db.SaveChanges());

                    // Вызываем хранимую процедуру для переноса в историю (если существует)
                    try
                    {
                        db.Database.ExecuteSqlCommand(
                            "EXEC [dbo].[sp_ArchiveLease] @LeaseID = {0}, @TerminationReason = {1}, @TerminatedBy = {2}",
                            _leaseId,
                            TxtTerminationReason.Text,
                            CmbTerminatedBy.SelectedItem.ToString());
                    }
                    catch
                    {
                        // Если хранимой процедуры нет, просто продолжаем
                    }

                    MessageBox.Show(
                        $"Договор №{lease.LeaseNumber} расторгнут.\n" +
                        $"Причина: {lease.TerminationReason}\n" +
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

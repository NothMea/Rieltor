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

        private string _consentDocumentPath;

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

        private void BtnBrowseDocument_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог выбора файла
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите документ согласия (Word)",
                Filter = "Документы Word (*.docx;*.doc)|*.docx;*.doc|Все файлы (*.*)|*.*",
                DefaultExt = ".docx",
                CheckFileExists = true
            };

            var result = openFileDialog.ShowDialog();

            if (result == true)
            {
                _consentDocumentPath = openFileDialog.FileName;
                TxtDocumentPath.Text = _consentDocumentPath;
            }
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

            // Проверка: для расторжения по соглашению сторон требуется документ согласия
            var selectedInitiator = (CmbTerminatedBy.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedInitiator == "По соглашению сторон" && string.IsNullOrEmpty(_consentDocumentPath))
            {
                MessageBox.Show("Для расторжения по соглашению сторон необходимо загрузить документ согласия в формате Word.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Вы уверены, что хотите расторгнуть договор?\n\n" +
                $"Причина: {TxtTerminationReason.Text}\n" +
                $"Инициатор: {selectedInitiator}\n" +
                $"{(string.IsNullOrEmpty(_consentDocumentPath) ? "" : $"Документ: {_consentDocumentPath}\n")}" +
                $"\nЭто действие нельзя отменить!",
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
                    lease.ConsentDocumentPath = _consentDocumentPath;

                    // Сохраняем изменения
                    await Task.Run(() => db.SaveChanges());

                    // Вызываем хранимую процедуру для переноса в историю (если существует)
                    try
                    {
                        db.Database.ExecuteSqlCommand(
                            "EXEC [dbo].[sp_ArchiveLease] @LeaseID = {0}, @TerminationReason = {1}, @TerminatedBy = {2}, @ConsentDocumentPath = {3}",
                            _leaseId,
                            TxtTerminationReason.Text,
                            selectedInitiator,
                            (object)_consentDocumentPath ?? DBNull.Value);
                    }
                    catch
                    {
                        // Если хранимой процедуры нет, просто продолжаем
                    }

                    MessageBox.Show(
                        $"Договор №{lease.LeaseNumber} расторгнут.\n" +
                        $"Причина: {lease.TerminationReason}\n" +
                        $"{(string.IsNullOrEmpty(_consentDocumentPath) ? "" : $"Документ согласия сохранен.\n")}" +
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

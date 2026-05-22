using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для LeaseEditView.xaml
    /// </summary>
    public partial class LeaseEditView : UserControl
    {
        private readonly int? _leaseId;
        private readonly Action _onSaveCallback;

        public LeaseEditView(Action onSaveCallback = null)
        {
            InitializeComponent();
            _onSaveCallback = onSaveCallback;
            LoadComboBoxes();
        }

        public LeaseEditView(int leaseId, Action onSaveCallback = null)
        {
            InitializeComponent();
            _leaseId = leaseId;
            _onSaveCallback = onSaveCallback;
            LoadComboBoxes();
            LoadLeaseData();
        }

        private void LoadComboBoxes()
        {
            using (var db = new RieltorEntities())
            {
                // Загружаем арендаторов
                CmbTenant.ItemsSource = db.Tenants.OrderBy(t => t.Name).ToList();

                // Загружаем объекты (только свободные или все)
                CmbProperty.ItemsSource = db.Property
                    .Where(p => p.Status == "Свободен" || (_leaseId.HasValue && p.Leases.Any(l => l.LeaseID == _leaseId.Value)))
                    .OrderBy(p => p.Address)
                    .ToList();
            }
        }

        private void LoadLeaseData()
        {
            if (!_leaseId.HasValue) return;

            using (var db = new RieltorEntities())
            {
                var lease = db.Leases.Find(_leaseId.Value);
                if (lease == null) return;

                TxtLeaseNumber.Text = lease.LeaseNumber;
                CmbTenant.SelectedValue = lease.TenantID;
                CmbProperty.SelectedValue = lease.PropertyID;
                DpStartDate.SelectedDate = lease.StartDate;
                DpEndDate.SelectedDate = lease.EndDate;
                TxtMonthlyAmount.Text = lease.MonthlyAmount.ToString();
            }
        }

        private void TxtMonthlyAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TxtLeaseNumber.Text))
            {
                MessageBox.Show("Введите номер договора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbTenant.SelectedItem == null)
            {
                MessageBox.Show("Выберите арендатора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbProperty.SelectedItem == null)
            {
                MessageBox.Show("Выберите объект недвижимости", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!DpStartDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату начала договора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!DpEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату окончания договора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (DpEndDate.SelectedDate.Value <= DpStartDate.SelectedDate.Value)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            decimal monthlyAmount;
            if (!decimal.TryParse(TxtMonthlyAmount.Text, out monthlyAmount) || monthlyAmount <= 0)
            {
                MessageBox.Show("Введите корректную сумму ежемесячной платы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private Leases CreateLeaseFromForm()
        {
            var lease = new Leases
            {
                LeaseNumber = TxtLeaseNumber.Text.Trim(),
                TenantID = (int)CmbTenant.SelectedValue,
                PropertyID = (int)CmbProperty.SelectedValue,
                StartDate = DpStartDate.SelectedDate.Value,
                EndDate = DpEndDate.SelectedDate.Value,
                MonthlyAmount = decimal.Parse(TxtMonthlyAmount.Text),
                Status = "Активен",
                IsArchived = false,
                TerminationReason = null,
                ConsentDocumentPath = null
            };

            return lease;
        }

        private void SaveLease(bool generateDocument = false)
        {
            if (!ValidateForm()) return;

            try
            {
                using (var db = new RieltorEntities())
                {
                    Leases lease;

                    if (_leaseId.HasValue)
                    {
                        lease = db.Leases.Find(_leaseId.Value);
                        if (lease == null)
                        {
                            MessageBox.Show("Договор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        lease.LeaseNumber = TxtLeaseNumber.Text.Trim();
                        lease.TenantID = (int)CmbTenant.SelectedValue;
                        lease.PropertyID = (int)CmbProperty.SelectedValue;
                        lease.StartDate = DpStartDate.SelectedDate.Value;
                        lease.EndDate = DpEndDate.SelectedDate.Value;
                        lease.MonthlyAmount = decimal.Parse(TxtMonthlyAmount.Text);
                    }
                    else
                    {
                        lease = CreateLeaseFromForm();
                        db.Leases.Add(lease);
                    }

                    db.SaveChanges();

                    if (generateDocument)
                    {
                        GenerateLeaseDocument(lease);
                    }

                    MessageBox.Show(
                        generateDocument 
                            ? "Договор сохранен и документ сформирован!" 
                            : "Договор успешно сохранен!", 
                        "Успех", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);

                    _onSaveCallback?.Invoke();

                    if (Window.GetWindow(this) != null)
                    {
                        Window.GetWindow(this).DialogResult = true;
                        Window.GetWindow(this).Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateLeaseDocument(Leases lease)
        {
            try
            {
                using (var db = new RieltorEntities())
                {
                    var tenant = db.Tenants.Find(lease.TenantID);
                    var property = db.Property.Find(lease.PropertyID);

                    if (tenant == null || property == null)
                    {
                        MessageBox.Show("Не удалось загрузить данные для формирования документа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Путь к шаблону
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "LeaseTemplate.docx");
                    
                    if (!File.Exists(templatePath))
                    {
                        MessageBox.Show(
                            $"Шаблон договора не найден по пути: {templatePath}\n\nПожалуйста, поместите файл LeaseTemplate.docx в папку Resources.",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // Создаем копию шаблона с уникальным именем
                    string outputFileName = $"Договор_{lease.LeaseNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                    string outputPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Договоры аренды",
                        outputFileName);

                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    // Копируем шаблон
                    File.Copy(templatePath, outputPath, true);

                    // Заполняем документ данными
                    FillWordDocument(outputPath, lease, tenant, property);

                    // Сохраняем путь к документу в базе
                    lease.ConsentDocumentPath = outputPath;
                    db.SaveChanges();

                    // Предлагаем открыть документ
                    var result = MessageBox.Show(
                        $"Договор успешно сформирован!\n\nПуть: {outputPath}\n\nОткрыть документ?",
                        "Успех",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(outputPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании документа: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillWordDocument(string documentPath, Leases lease, Tenants tenant, Property property)
        {
            // Используем COM-автоматизацию для заполнения Word документа
            // Для этого требуется установленный Microsoft Office
            try
            {
                // Создаем объект Word Application через позднюю привязку
                Type wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null)
                {
                    throw new Exception("Microsoft Word не установлен на этом компьютере");
                }

                dynamic wordApp = Activator.CreateInstance(wordType);
                wordApp.Visible = false;
                wordApp.DisplayAlerts = false;

                try
                {
                    // Открываем документ
                    object path = documentPath;
                    object readOnly = false;
                    object isVisible = false;

                    dynamic doc = wordApp.Documents.Open(ref path, ref readOnly, ref isVisible);

                    // Заполняем поля формы (закладки)
                    ReplaceBookmark(doc, "LeaseNumber", lease.LeaseNumber);
                    ReplaceBookmark(doc, "LeaseDate", DateTime.Now.ToShortDateString());
                    ReplaceBookmark(doc, "StartDate", lease.StartDate.ToShortDateString());
                    ReplaceBookmark(doc, "EndDate", lease.EndDate.ToShortDateString());
                    ReplaceBookmark(doc, "MonthlyAmount", lease.MonthlyAmount.ToString("N2"));
                    
                    // Данные арендатора
                    ReplaceBookmark(doc, "TenantName", tenant.Name ?? "");
                    ReplaceBookmark(doc, "TenantINN", tenant.INN ?? "");
                    ReplaceBookmark(doc, "TenantPhone", tenant.Phone ?? "");
                    ReplaceBookmark(doc, "TenantEmail", tenant.Email ?? "");
                    
                    // Данные объекта
                    ReplaceBookmark(doc, "PropertyAddress", property.Address ?? "");
                    ReplaceBookmark(doc, "PropertyArea", property.Area.ToString("F2"));
                    ReplaceBookmark(doc, "PropertyType", property.PropertyType ?? "");
                    ReplaceBookmark(doc, "MonthlyRent", property.MonthlyRent.ToString("N2"));

                    // Сохраняем документ
                    doc.Save();
                    doc.Close();
                }
                finally
                {
                    wordApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при заполнении Word документа: {ex.Message}");
            }
        }

        private void ReplaceBookmark(dynamic doc, string bookmarkName, string value)
        {
            try
            {
                if (doc.Bookmarks.Exists(bookmarkName))
                {
                    doc.Bookmarks[bookmarkName].Range.Text = value;
                }
            }
            catch
            {
                // Закладка не найдена - пропускаем
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveLease(false);
        }

        private void BtnSaveAndGenerate_Click(object sender, RoutedEventArgs e)
        {
            SaveLease(true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) != null)
            {
                Window.GetWindow(this).DialogResult = false;
                Window.GetWindow(this).Close();
            }
        }
    }
}

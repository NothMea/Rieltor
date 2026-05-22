using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Office.Core;
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
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                string detailedError = $"Ошибка DbUpdateException при сохранении договора: {dbEx.Message}";
                
                // Получаем информацию о сущности, вызвавшей ошибку
                if (dbEx.Entries != null && dbEx.Entries.Count() > 0)
                {
                    foreach (var entry in dbEx.Entries)
                    {
                        detailedError += $"\n\nСущность: {entry.Entity.GetType().Name}";
                        
                        var properties = entry.CurrentValues.PropertyNames;
                        foreach (var propName in properties)
                        {
                            var value = entry.CurrentValues[propName];
                            detailedError += $"\n  {propName}: {(value ?? "null")}";
                            
                            if (value is string strValue)
                            {
                                detailedError += $" (длина: {strValue.Length})";
                            }
                        }
                    }
                }
                
                if (dbEx.InnerException != null)
                {
                    detailedError += $"\n\nSQL Error: {dbEx.InnerException.Message}";
                    
                    if (dbEx.InnerException.InnerException != null)
                    {
                        detailedError += $"\n\nДетали SQL: {dbEx.InnerException.InnerException.Message}";
                    }
                    
                    // Проверяем на нарушение ограничений NOT NULL
                    if (dbEx.InnerException.Message.Contains("cannot be null") || 
                        dbEx.InnerException.Message.Contains("не может быть пустым") ||
                        dbEx.InnerException.Message.Contains("Cannot insert the value NULL into column"))
                    {
                        detailedError += "\n\n=== ВОЗМОЖНАЯ ПРИЧИНА ===\nПоле ConsentDocumentPath имеет ограничение NOT NULL в базе данных.\nВыполните скрипт FixConsentDocumentPath.sql для исправления.";
                    }
                    
                    // Проверяем на превышение длины строки
                    if (dbEx.InnerException.Message.Contains("String or binary data would be truncated") ||
                        dbEx.InnerException.Message.Contains("слишком длинная"))
                    {
                        detailedError += "\n\n=== ВОЗМОЖНАЯ ПРИЧИНА ===\nПуть к файлу слишком длинный для поля в базе данных.";
                    }
                }
                
                MessageBox.Show(detailedError, "DbUpdateException", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка при сохранении: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    errorMsg += $"\n\nВнутренняя ошибка: {ex.InnerException.Message}";
                    
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMsg += $"\n\nДетали: {ex.InnerException.InnerException.Message}";
                    }
                }
                
                MessageBox.Show(errorMsg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateLeaseDocument(Leases lease)
        {
            try
            {
                using (var db = new RieltorEntities())
                {
                    // Прикрепляем сущность к новому контексту
                    var trackedLease = db.Leases.Find(lease.LeaseID);
                    if (trackedLease == null)
                    {
                        MessageBox.Show("Договор не найден в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

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
                    string outputFileName = $"Договор_{trackedLease.LeaseNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
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
                    FillWordDocument(outputPath, trackedLease, tenant, property);

                    // Сохраняем путь к документу в базе
                    trackedLease.ConsentDocumentPath = outputPath;
                    
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
                    {
                        string detailedError = $"Ошибка сохранения в базе данных: {dbEx.Message}";
                        
                        // Получаем информацию о сущности, вызвавшей ошибку
                        if (dbEx.Entries != null && dbEx.Entries.Count() > 0)
                        {
                            foreach (var entry in dbEx.Entries)
                            {
                                detailedError += $"\n\nСущность: {entry.Entity.GetType().Name}";
                                
                                // Получаем все свойства и их значения
                                var properties = entry.CurrentValues.PropertyNames;
                                foreach (var propName in properties)
                                {
                                    var value = entry.CurrentValues[propName];
                                    detailedError += $"\n  {propName}: {(value ?? "null")}";
                                    
                                    // Проверяем длину строковых полей
                                    if (value is string strValue)
                                    {
                                        detailedError += $" (длина: {strValue.Length})";
                                    }
                                }
                            }
                        }
                        
                        if (dbEx.InnerException != null)
                        {
                            detailedError += $"\n\nSQL Error: {dbEx.InnerException.Message}";
                            
                            // Проверяем на нарушение ограничений NOT NULL
                            if (dbEx.InnerException.Message.Contains("cannot be null") || 
                                dbEx.InnerException.Message.Contains("не может быть пустым") ||
                                dbEx.InnerException.Message.Contains("Cannot insert the value NULL into column"))
                            {
                                detailedError += "\n\nВОЗМОЖНАЯ ПРИЧИНА: Поле ConsentDocumentPath имеет ограничение NOT NULL в базе данных.";
                            }
                            
                            // Проверяем на превышение длины строки
                            if (dbEx.InnerException.Message.Contains("String or binary data would be truncated") ||
                                dbEx.InnerException.Message.Contains("слишком длинная"))
                            {
                                detailedError += $"\n\nПуть к файлу слишком длинный для поля в базе данных.\nДлина пути: {outputPath.Length} символов.";
                            }
                        }
                        
                        throw new Exception(detailedError, dbEx);
                    }

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
                string errorMessage = $"Ошибка при создании документа: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nВнутренняя ошибка: {ex.InnerException.Message}";
                    
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $"\n\nДетали: {ex.InnerException.InnerException.Message}";
                    }
                }
                
                // Если это DbUpdateException, покажем детали ошибок валидации
                if (ex is System.Data.Entity.Infrastructure.DbUpdateException dbEx)
                {
                    if (dbEx.InnerException != null)
                    {
                        errorMessage += $"\n\nSQL Error: {dbEx.InnerException.Message}";
                    }
                }
                
                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillWordDocument(string documentPath, Leases lease, Tenants tenant, Property property)
        {
            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document doc = null;
            object missing = System.Reflection.Missing.Value;

            try
            {
                // 1. Убиваем все зависшие процессы WINWORD перед запуском
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("WINWORD"))
                {
                    try { proc.Kill(); proc.WaitForExit(1000); } catch { }
                }
                System.Threading.Thread.Sleep(500);

                // 2. Создаем экземпляр Word с явной типизацией
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = false;
                wordApp.DisplayAlerts = Microsoft.Office.Interop.Word.WdAlertLevel.wdAlertsNone;
                wordApp.ScreenUpdating = false;
                wordApp.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityLow;

                // 3. Открываем документ
                object path = documentPath;
                object readOnly = false;
                object isVisible = false;

                doc = wordApp.Documents.Open(ref path, ref missing, ref readOnly, ref missing, 
                                            ref missing, ref missing, ref missing, ref missing, 
                                            ref missing, ref missing, ref missing, ref isVisible, 
                                            ref missing, ref missing, ref missing, ref missing);

                // 4. Функция замены закладок
                void ReplaceBookmark(string bookmarkName, string value)
                {
                    if (doc.Bookmarks.Exists(bookmarkName))
                    {
                        var range = doc.Bookmarks[bookmarkName].Range;
                        range.Text = value ?? "";
                    }
                }

                // 5. Заполняем данные
                ReplaceBookmark("LeaseNumber", lease.LeaseNumber ?? "");
                ReplaceBookmark("LeaseDate", DateTime.Now.ToShortDateString());
                ReplaceBookmark("StartDate", lease.StartDate.ToShortDateString());
                ReplaceBookmark("EndDate", lease.EndDate.ToShortDateString());
                ReplaceBookmark("MonthlyAmount", lease.MonthlyAmount.ToString("N2"));

                // Данные арендатора
                ReplaceBookmark("TenantName", tenant.Name ?? "");
                ReplaceBookmark("TenantINN", tenant.INN ?? "");
                ReplaceBookmark("TenantPhone", tenant.Phone ?? "");
                ReplaceBookmark("TenantEmail", tenant.Email ?? "");

                // Данные объекта
                ReplaceBookmark("PropertyAddress", property.Address ?? "");
                ReplaceBookmark("PropertyArea", property.Area.ToString("F2"));
                ReplaceBookmark("PropertyType", property.PropertyType ?? "");
                ReplaceBookmark("MonthlyRent", property.MonthlyRent.ToString("N2"));

                // 6. Сохраняем
                doc.Save();
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                throw new Exception(
                    $"COM-ошибка при работе с Word (0x{comEx.ErrorCode:X}): {comEx.Message}\n\n" +
                    $"Возможные решения:\n" +
                    $"1. Выполните восстановление Office (Control Panel > Programs > Microsoft Office > Change > Repair)\n" +
                    $"2. Убедитесь, что версии Office и приложения совпадают (обе 32-bit или обе 64-bit)\n" +
                    $"3. Проверьте антивирус - он может блокировать автоматизацию Office\n" +
                    $"4. Перезапустите компьютер для очистки зависших процессов Word");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при заполнении Word документа: {ex.Message}", ex);
            }
            finally
            {
                // 7. Корректное закрытие ресурсов
                if (doc != null)
                {
                    try
                    {
                        object saveChanges = Microsoft.Office.Interop.Word.WdSaveOptions.wdSaveChanges;
                        doc.Close(ref saveChanges, ref missing, ref missing);
                    }
                    catch { }
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                }

                if (wordApp != null)
                {
                    try
                    {
                        wordApp.Quit(ref missing, ref missing, ref missing);
                    }
                    catch { }
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }

                doc = null;
                wordApp = null;

                // Принудительная сборка мусора для освобождения COM
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
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

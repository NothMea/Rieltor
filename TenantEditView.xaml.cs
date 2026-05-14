using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для TenantEditView.xaml
    /// </summary>
    public partial class TenantEditView : UserControl
    {
        private readonly int? _tenantId;
        private event Action OnDataSaved;

        public TenantEditView(int tenantId, Action onDataSaved = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            OnDataSaved = onDataSaved;
            using (var db = new RieltorEntities())
            {
                LoadTenantData(db);
            }
        }

        public TenantEditView(Action onDataSaved = null)
        {
            InitializeComponent();
            _tenantId = null;
            OnDataSaved = onDataSaved;
        }

        private void LoadTenantData(RieltorEntities db)
        {
            var tenant = db.Tenants.Find(_tenantId);
            if (tenant == null)
            {
                MessageBox.Show("Арендатор не найден.");
                return;
            }

            TxtName.Text = tenant.Name;
            TxtPhone.Text = tenant.Phone;
            TxtINN.Text = tenant.INN;
            TxtEmail.Text = tenant.Email;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите ФИО/Название арендатора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new RieltorEntities())
            {
                Tenants tenant;
                if (_tenantId.HasValue)
                {
                    // Редактирование существующего арендатора
                    tenant = db.Tenants.Find(_tenantId);
                    if (tenant == null)
                    {
                        MessageBox.Show("Арендатор не найден.");
                        return;
                    }
                }
                else
                {
                    // Создание нового арендатора
                    tenant = new Tenants();
                    db.Tenants.Add(tenant);
                }

                tenant.Name = TxtName.Text.Trim();
                tenant.Phone = TxtPhone.Text.Trim();
                tenant.INN = TxtINN.Text.Trim();
                tenant.Email = TxtEmail.Text.Trim();

                db.SaveChanges();

                MessageBox.Show($"Арендатор успешно {(_tenantId.HasValue ? "обновлён" : "добавлен")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                OnDataSaved?.Invoke();

                // Если это UserControl в окне, закрываем окно
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}

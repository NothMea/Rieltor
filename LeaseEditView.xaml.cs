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
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для LeaseEditView.xaml
    /// </summary>
    public partial class LeaseEditView : Window
    {
        private readonly int _leaseId;
        private RieltorEntities _db = new RieltorEntities();

        public LeaseEditView(int leaseId)
        {
            InitializeComponent();
            _leaseId = leaseId;
            LoadLeaseData();
        }

        private void LoadLeaseData()
        {
            var lease = _db.Leases.Find(_leaseId);
            if (lease == null)
            {
                MessageBox.Show("Договор не найден.");
                this.Close();
                return;
            }

            TxtLeaseNumber.Text = lease.LeaseNumber;

            // Загружаем связанные данные
            var property = _db.Property.Find(lease.PropertyID);
            var tenant = _db.Tenants.Find(lease.TenantID);

            TxtProperty.Text = property?.Address ?? "Не указан";
            TxtTenant.Text = tenant?.Name ?? "Не указан";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Сохранение (заглушка)
            var lease = _db.Leases.Find(_leaseId);
            if (lease != null)
            {
                lease.LeaseNumber = TxtLeaseNumber.Text.Trim();
                _db.SaveChanges();
                MessageBox.Show("Договор успешно обновлён!");
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
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
using WpfApp1.Views;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ConnectionString { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            // Устанавливаем полноэкранный режим
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.CanResize;
            LoadHome();
        }
        private void LoadHome()
        {
            MainContent.Content = new DashboardView();

        }
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {

            this.Close();


        }
        private void MenuItem_Home_Click(object sender, RoutedEventArgs e) => LoadHome();
        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e) => MainContent.Content = new PropertiesView();
        private void MenuItem_Tenants_Click(object sender, RoutedEventArgs e) => MainContent.Content = new TenantsView();

    }
}


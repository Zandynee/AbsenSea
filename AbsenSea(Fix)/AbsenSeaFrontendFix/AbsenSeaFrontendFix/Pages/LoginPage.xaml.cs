using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement login logic
            // For now, navigate to Dashboard
            var dashboardPage = new DashboardPage();
            NavigationService?.Navigate(dashboardPage);
        }

        private void RegisterLink_Click(object sender, MouseButtonEventArgs e)
        {
            var registerPage = new RegisterPage();
            NavigationService?.Navigate(registerPage);
        }
    }
}

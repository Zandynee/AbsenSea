using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement registration logic
            MessageBox.Show("Registration successful! Please sign in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            var loginPage = new LoginPage();
            NavigationService?.Navigate(loginPage);
        }

        private void LoginLink_Click(object sender, MouseButtonEventArgs e)
        {
            var loginPage = new LoginPage();
            NavigationService?.Navigate(loginPage);
        }
    }
}

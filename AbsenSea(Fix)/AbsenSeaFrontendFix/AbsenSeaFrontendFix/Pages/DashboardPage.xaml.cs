using System.Windows;
using System.Windows.Controls;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private void NavigateToAttendance_Click(object sender, RoutedEventArgs e)
        {
            var attendancePage = new AttendancePage();
            NavigationService?.Navigate(attendancePage);
        }

        private void NavigateToReports_Click(object sender, RoutedEventArgs e)
        {
            var reportsPage = new AttendanceReportPage();
            NavigationService?.Navigate(reportsPage);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginPage = new LoginPage();
            NavigationService?.Navigate(loginPage);
        }
    }
}

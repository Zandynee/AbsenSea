using System.Windows;
using System.Windows.Controls;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class AttendancePage : Page
    {
        public AttendancePage()
        {
            InitializeComponent();
        }

        private void NavigateToDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardPage = new DashboardPage();
            NavigationService?.Navigate(dashboardPage);
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

        private void CrewIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Implement real-time validation
        }

        private void VerifyId_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement ID verification with database
            // For demo purposes, show crew info
            if (!string.IsNullOrWhiteSpace(CrewIdTextBox.Text))
            {
                CrewInfoPanel.Visibility = Visibility.Visible;
                CrewNameText.Text = "John Smith";
                CrewPositionText.Text = "Engineer • Deck A";
            }
        }

        private void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement camera initialization
            MessageBox.Show("Camera started. Ready for equipment detection.", "Camera Active", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CaptureAnalyze_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement AI analysis
            // For demo purposes, show detection result
            DetectionStatus.Visibility = Visibility.Visible;
            DetectionStatusText.Text = "✓ Equipment Detected";
            DetectionDetailsText.Text = "Helmet ✓ • Vest ✓";
            
            MessageBox.Show("Attendance recorded successfully!\n\nCrew: John Smith\nStatus: Compliant\nEquipment: Helmet ✓ | Vest ✓", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Reset for next crew member
            CrewIdTextBox.Clear();
            CrewInfoPanel.Visibility = Visibility.Collapsed;
            DetectionStatus.Visibility = Visibility.Collapsed;
        }
    }
}

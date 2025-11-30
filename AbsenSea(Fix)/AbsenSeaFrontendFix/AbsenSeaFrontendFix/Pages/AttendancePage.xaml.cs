using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
            // Auto-suggest already handled by IsEditable ComboBox
        }

        private void VerifyId_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement ID verification with database
            // For demo purposes, show crew info
            string selectedText = CrewIdComboBox.Text;
            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                CrewInfoPanel.Visibility = Visibility.Visible;
                // Extract name from selection (e.g., "CR-001 - John Smith")
                if (selectedText.Contains(" - "))
                {
                    var parts = selectedText.Split(new[] { " - " }, StringSplitOptions.None);
                    CrewNameText.Text = parts.Length > 1 ? parts[1] : "Unknown";
                }
                else
                {
                    CrewNameText.Text = "Unknown Crew";
                }
                CrewPositionText.Text = "Engineer • Deck A";
            }
        }

        private void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement camera initialization
            MessageBox.Show("Camera started. Ready for equipment detection.", "Camera Active", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select image
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Crew Photo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"Photo uploaded: {System.IO.Path.GetFileName(filePath)}\n\nProcessing for equipment detection...", 
                    "Photo Uploaded", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // TODO: Process uploaded image with AI detection
                // Simulate detection result
                DetectionStatus.Visibility = Visibility.Visible;
                DetectionStatusText.Text = "✓ Equipment Detected";
                DetectionDetailsText.Text = "Helmet ✓ • Vest ✓";
            }
        }

        private void CaptureAnalyze_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement AI analysis
            // For demo purposes, show detection result
            DetectionStatus.Visibility = Visibility.Visible;
            DetectionStatusText.Text = "✓ Equipment Detected";
            DetectionDetailsText.Text = "Helmet ✓ • Vest ✓";
            
            MessageBox.Show("Attendance recorded successfully!\n\nCrew: " + CrewNameText.Text + "\nStatus: Compliant\nEquipment: Helmet ✓ | Vest ✓", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Reset for next crew member
            CrewIdComboBox.Text = "";
            CrewInfoPanel.Visibility = Visibility.Collapsed;
            DetectionStatus.Visibility = Visibility.Collapsed;
        }
    }
}

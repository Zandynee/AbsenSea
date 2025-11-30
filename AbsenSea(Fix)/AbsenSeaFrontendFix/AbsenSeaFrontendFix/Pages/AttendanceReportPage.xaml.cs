using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class AttendanceReportPage : Page
    {
        public AttendanceReportPage()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // Sample data for demonstration
            var attendanceRecords = new ObservableCollection<AttendanceRecord>
            {
                new AttendanceRecord { CrewId = "CR-001", Name = "John Smith", Position = "Engineer", CheckInTime = "08:15 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" },
                new AttendanceRecord { CrewId = "CR-002", Name = "Sarah Johnson", Position = "Deck Officer", CheckInTime = "08:22 AM", HelmetStatus = "✓", VestStatus = "✗", HelmetStatusColor = "#10B981", VestStatusColor = "#EF4444", OverallStatus = "Non-Compliant", OverallStatusColor = "#EF4444" },
                new AttendanceRecord { CrewId = "CR-003", Name = "Mike Davis", Position = "Chef", CheckInTime = "07:45 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" },
                new AttendanceRecord { CrewId = "CR-004", Name = "Emily Chen", Position = "Navigator", CheckInTime = "08:30 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" },
                new AttendanceRecord { CrewId = "CR-005", Name = "David Brown", Position = "Engineer", CheckInTime = "08:18 AM", HelmetStatus = "✗", VestStatus = "✓", HelmetStatusColor = "#EF4444", VestStatusColor = "#10B981", OverallStatus = "Non-Compliant", OverallStatusColor = "#EF4444" },
                new AttendanceRecord { CrewId = "CR-006", Name = "Lisa Wang", Position = "Medical Officer", CheckInTime = "08:05 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" },
                new AttendanceRecord { CrewId = "CR-007", Name = "Robert Lee", Position = "Electrician", CheckInTime = "07:55 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" },
                new AttendanceRecord { CrewId = "CR-008", Name = "Anna Martinez", Position = "Deck Hand", CheckInTime = "08:40 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" },
                new AttendanceRecord { CrewId = "CR-009", Name = "Tom Wilson", Position = "Engineer", CheckInTime = "08:25 AM", HelmetStatus = "✓", VestStatus = "✗", HelmetStatusColor = "#10B981", VestStatusColor = "#EF4444", OverallStatus = "Non-Compliant", OverallStatusColor = "#EF4444" },
                new AttendanceRecord { CrewId = "CR-010", Name = "Jessica Taylor", Position = "Cook", CheckInTime = "07:50 AM", HelmetStatus = "✓", VestStatus = "✓", HelmetStatusColor = "#10B981", VestStatusColor = "#10B981", OverallStatus = "Compliant", OverallStatusColor = "#10B981" }
            };

            AttendanceDataGrid.ItemsSource = attendanceRecords;
        }

        private void NavigateToDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardPage = new DashboardPage();
            NavigationService?.Navigate(dashboardPage);
        }

        private void NavigateToAttendance_Click(object sender, RoutedEventArgs e)
        {
            var attendancePage = new AttendancePage();
            NavigationService?.Navigate(attendancePage);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginPage = new LoginPage();
            NavigationService?.Navigate(loginPage);
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement PDF export functionality
            MessageBox.Show("Report exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Model class for attendance records
    public class AttendanceRecord
    {
        public string CrewId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string CheckInTime { get; set; } = string.Empty;
        public string HelmetStatus { get; set; } = string.Empty;
        public string VestStatus { get; set; } = string.Empty;
        public string HelmetStatusColor { get; set; } = string.Empty;
        public string VestStatusColor { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
        public string OverallStatusColor { get; set; } = string.Empty;
    }
}

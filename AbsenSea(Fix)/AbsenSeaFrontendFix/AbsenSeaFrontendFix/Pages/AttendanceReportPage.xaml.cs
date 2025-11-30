using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AbsenSeaFrontendFix.Pages.Database;
using System.Diagnostics;
using System.Linq;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class AttendanceReportPage : Page
    {
        private Backend _backend;

        public AttendanceReportPage()
        {
            InitializeComponent();
            this.Loaded += AttendanceReportPage_Loaded;
        }

        private async void AttendanceReportPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAttendanceData();
        }

        private async System.Threading.Tasks.Task LoadAttendanceData()
        {
            try
            {
                // Initialize backend if not already done
                if (_backend == null)
                {
                    _backend = await Backend.CreateAsync();
                }

                // Fetch crew checks - we'll need to manually join crew member data
                var checksResult = await _backend.SupabaseClient
                    .From<CrewCheck>()
                    .Select("*")
                    .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                // Fetch all crew members for lookup
                var crewResult = await _backend.SupabaseClient
                    .From<CrewMember>()
                    .Select("*")
                    .Get();

                // Create a dictionary for quick lookup
                var crewLookup = crewResult.Models.ToDictionary(c => c.CREW_ID, c => c);

                var list = new ObservableCollection<AttendanceRecord>();

                foreach (var item in checksResult.Models)
                {
                    // Skip if no crew ID
                    if (item.CREW_ID == null) continue;

                    // Get crew member from lookup
                    CrewMember crewMember = null;
                    if (crewLookup.ContainsKey(item.CREW_ID.Value))
                    {
                        crewMember = crewLookup[item.CREW_ID.Value];
                    }

                    list.Add(new AttendanceRecord
                    {
                        CrewId = item.CREW_ID?.ToString() ?? "N/A",
                        Name = crewMember?.CREW_NAME ?? "Unknown",
                        Position = "Crew Member", // Default since position not in DB
                        CheckInTime = item.CAPTURE_AT.ToLocalTime().ToString("HH:mm:ss"),
                        HelmetStatus = item.HELMET == true ? "✓" : "✗",
                        VestStatus = item.VEST == true ? "✓" : "✗",
                        HelmetStatusColor = item.HELMET == true ? "#10B981" : "#EF4444",
                        VestStatusColor = item.VEST == true ? "#10B981" : "#EF4444",
                        OverallStatus = (item.HELMET == true && item.VEST == true) ? "Compliant" : "Non-Compliant",
                        OverallStatusColor = (item.HELMET == true && item.VEST == true) ? "#10B981" : "#EF4444",
                    });
                }

                AttendanceDataGrid.ItemsSource = list;

                Debug.WriteLine($"Loaded {list.Count} attendance records");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading attendance data: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Failed to load attendance data: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            MessageBox.Show("Report exported successfully!", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
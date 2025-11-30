using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static AbsenSeaFrontendFix.Pages.Backend;
using Supabase;
using Supabase.Interfaces;
using AbsenSeaFrontendFix.Pages.Database;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class AttendanceReportPage : Page
    {


        private Backend _backend;
        public AttendanceReportPage()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private async void LoadSampleData()
        {

            // Sample data for demonstration
            // your supabase service
            try
            {
                if (_backend == null)
                {
                    _backend = await Backend.CreateAsync();
                }

                var result = await _backend.SupabaseClient
                    .From<CrewCheck>()
          
                    .Get();


                var list = new ObservableCollection<AttendanceRecord>();

                foreach (var item in result.Models)
                {
                    list.Add(new AttendanceRecord
                    {
                        CrewId = item.CREW_ID.ToString(),
                        Name = item.CREW_MEMBER?.CREW_NAME,
                        Position = "Unknown", // unless you add a column for this
                        CheckInTime = item.CAPTURE_AT.ToShortTimeString(),
                        HelmetStatus = item.HELMET == true ? "✓" : "✗",
                        VestStatus = item.VEST == true ? "✓" : "✗",
                        HelmetStatusColor = item.HELMET == true ? "#10B981" : "#EF4444",
                        VestStatusColor = item.VEST == true ? "#10B981" : "#EF4444",
                        OverallStatus = (item.HELMET == true && item.VEST == true) ? "Compliant" : "Non-Compliant",
                        OverallStatusColor = (item.HELMET == true && item.VEST == true) ? "#10B981" : "#EF4444",
                    });
                }

                AttendanceDataGrid.ItemsSource = list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

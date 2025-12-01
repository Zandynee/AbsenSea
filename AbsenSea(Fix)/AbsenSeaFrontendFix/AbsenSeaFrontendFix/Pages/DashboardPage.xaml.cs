using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AbsenSeaFrontendFix.Pages.Database;
using System.Diagnostics;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class DashboardPage : Page
    {
        private Backend _backend;

        public DashboardPage()
        {
            InitializeComponent();
            this.Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardData();
        }

        private async System.Threading.Tasks.Task LoadDashboardData()
        {
            try
            {
                // Initialize backend if needed
                if (_backend == null)
                {
                    _backend = await Backend.CreateAsync();
                }

                // Load ship information if available
                if (Application.Current.Properties.Contains("CurrentShipName"))
                {
                    var shipName = Application.Current.Properties["CurrentShipName"]?.ToString();
                    var shipId = Application.Current.Properties["CurrentShipId"]?.ToString();
                    ShipInfoText.Text = $"Ship: {shipName} � ID: {shipId}";
                }

                // Get dashboard statistics
                var stats = await _backend.GetTodayStats();

                // Update UI on UI thread
                Dispatcher.Invoke(() =>
                {
                    UpdateStatistics(stats);
                });

                // Load recent activity
                await LoadRecentActivity();

                // Load attendance chart
                await LoadAttendanceChart();

                Debug.WriteLine($"Dashboard loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                MessageBox.Show($"Failed to load dashboard data: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateStatistics(DashboardStats stats)
        {
            try
            {
                // Update Total Crew
                TotalCrewText.Text = stats.TotalCrew.ToString();

                // Update Checked In Today
                CheckedInText.Text = stats.CheckedInToday.ToString();
                AttendanceRateText.Text = $"{stats.AttendanceRate:F1}% attendance rate";

                // Update Compliant
                CompliantText.Text = stats.Compliant.ToString();
                ComplianceRateText.Text = $"{stats.ComplianceRate:F1}% with full equipment";

                // Update Non-Compliant
                NonCompliantText.Text = stats.NonCompliant.ToString();
                var nonComplianceRate = stats.CheckedInToday > 0
                    ? ((stats.NonCompliant * 100.0) / stats.CheckedInToday)
                    : 0;
                NonComplianceRateText.Text = $"{nonComplianceRate:F1}% missing equipment";

                // Update Equipment Status
                if (stats.CheckedInToday > 0)
                {
                    var helmetRate = stats.HelmetRate;
                    var vestRate = stats.VestRate;
                    var bothRate = stats.ComplianceRate;

                    EquipmentStatusText.Text =
                        $"🪖 Safety Helmet: {helmetRate:F0}%\n" +
                        $"🦺 Safety Vest: {vestRate:F0}%\n" +
                        $"✓ Both Equipment: {bothRate:F0}%\n\n";

                    if (stats.NonCompliant > 0)
                    {
                        EquipmentStatusText.Text +=
                            $"⚠️ {stats.NonCompliant} crew members need to complete their safety equipment.";
                    }
                    else
                    {
                        EquipmentStatusText.Text += "✓ All checked-in crew members are compliant!";
                    }
                }
                else
                {
                    EquipmentStatusText.Text = "No check-ins recorded today.";
                }

                Debug.WriteLine($"Statistics updated: {stats.TotalCrew} crew, {stats.CheckedInToday} checked in");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadRecentActivity()
        {
            try
            {
                var today = DateTime.Today;

                // Get recent checks
                var checksResult = await _backend.SupabaseClient
                    .From<CrewCheck>()
                    .Select("*")
                    .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(20)
                    .Get();
                
                // Filter to today's checks
                var recentChecks = checksResult.Models
                    .Where(c => c.CHECK_DATE.HasValue && c.CHECK_DATE.Value.Date == today)
                    .Take(10)
                    .ToList();

                // Get crew members for lookup
                var crewResult = await _backend.SupabaseClient
                    .From<CrewMember>()
                    .Select("*")
                    .Get();

                var crewLookup = crewResult.Models.ToDictionary(c => c.CREW_ID, c => c);

                // Clear and rebuild activity panel
                RecentActivityPanel.Children.Clear();

                if (recentChecks.Count == 0)
                {
                    var noActivityText = new TextBlock
                    {
                        Text = "No activity recorded today.",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                    };
                    RecentActivityPanel.Children.Add(noActivityText);
                    return;
                }

                foreach (var check in recentChecks)
                {
                    if (check.CREW_ID == null) continue;

                    // Get crew member from lookup
                    CrewMember crewMember = null;
                    if (crewLookup.ContainsKey(check.CREW_ID.Value))
                    {
                        crewMember = crewLookup[check.CREW_ID.Value];
                    }

                    if (crewMember == null) continue;

                    var isCompliant = check.HELMET == true && check.VEST == true;
                    // Convert UTC time from database to local time
                    var localCaptureTime = check.CAPTURE_AT.Kind == DateTimeKind.Utc 
                        ? check.CAPTURE_AT.ToLocalTime() 
                        : check.CAPTURE_AT;
                    var timeSinceCheck = DateTime.Now - localCaptureTime;
                    var timeText = FormatTimeAgo(timeSinceCheck);

                    // Create activity item
                    var activityBorder = CreateActivityItem(
                        crewMember.CREW_NAME,
                        isCompliant,
                        check.HELMET == true,
                        check.VEST == true,
                        timeText
                    );

                    RecentActivityPanel.Children.Add(activityBorder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent activity: {ex.Message}");
            }
        }

        private Border CreateActivityItem(string crewName, bool isCompliant, bool hasHelmet, bool hasVest, string timeAgo)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 12, 0, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(isCompliant ?
                    Color.FromRgb(209, 250, 229) : Color.FromRgb(254, 226, 226)),
                Margin = new Thickness(0, 0, 15, 0)
            };
            var iconText = new TextBlock
            {
                Text = isCompliant ? "?" : "?",
                FontSize = 18,
                Foreground = new SolidColorBrush(isCompliant ?
                    Color.FromRgb(16, 185, 129) : Color.FromRgb(239, 68, 68)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);

            // Details
            var detailsPanel = new StackPanel();
            var nameText = new TextBlock
            {
                Text = isCompliant ? $"{crewName} checked in" : $"{crewName} - Equipment Missing",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };
            var statusText = new TextBlock
            {
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0)
            };

            if (isCompliant)
            {
                statusText.Text = "Full compliance � Helmet & Vest detected";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else
            {
                var missing = new System.Collections.Generic.List<string>();
                if (!hasHelmet) missing.Add("helmet");
                if (!hasVest) missing.Add("vest");
                statusText.Text = $"{string.Join(" and ", missing)} not detected";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }

            detailsPanel.Children.Add(nameText);
            detailsPanel.Children.Add(statusText);
            Grid.SetColumn(detailsPanel, 1);

            // Time
            var timeText = new TextBlock
            {
                Text = timeAgo,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeText, 2);

            grid.Children.Add(iconBorder);
            grid.Children.Add(detailsPanel);
            grid.Children.Add(timeText);

            border.Child = grid;
            return border;
        }

        private string FormatTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours > 1 ? "s" : "")} ago";
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays > 1 ? "s" : "")} ago";
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
            // Clear session data
            Application.Current.Properties.Clear();

            var loginPage = new LoginPage();
            NavigationService?.Navigate(loginPage);
        }

        private async System.Threading.Tasks.Task LoadAttendanceChart()
        {
            try
            {
                // Clear canvas
                AttendanceChartCanvas.Children.Clear();

                // Get attendance data for last 7 days
                var attendanceData = new List<(string Day, int Count)>();
                
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var checks = await _backend.SupabaseClient
                        .From<CrewCheck>()
                        .Select("*")
                        .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                        .Limit(200)
                        .Get();
                    
                    // Filter by date locally
                    var dayCount = checks.Models
                        .Count(c => c.CHECK_DATE.HasValue && c.CHECK_DATE.Value.Date == date);
                    
                    attendanceData.Add((date.ToString("ddd"), dayCount));
                }

                // Draw the chart
                DrawBarChart(attendanceData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading attendance chart: {ex.Message}");
                
                // Show error message on canvas
                var errorText = new TextBlock
                {
                    Text = "Unable to load chart data",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(errorText, AttendanceChartCanvas.ActualWidth / 2 - 75);
                Canvas.SetTop(errorText, AttendanceChartCanvas.ActualHeight / 2);
                AttendanceChartCanvas.Children.Add(errorText);
            }
        }

        private void DrawBarChart(List<(string Day, int Count)> data)
        {
            if (data.Count == 0) return;

            var canvasWidth = AttendanceChartCanvas.ActualWidth;
            var canvasHeight = AttendanceChartCanvas.ActualHeight;

            // If canvas not rendered yet, use default size
            if (canvasWidth == 0) canvasWidth = 600;
            if (canvasHeight == 0) canvasHeight = 200;

            var maxValue = data.Max(d => d.Count);
            if (maxValue == 0) maxValue = 1; // Avoid division by zero

            var barWidth = (canvasWidth - 100) / data.Count;
            var chartHeight = canvasHeight - 60;

            // Draw grid lines
            for (int i = 0; i <= 5; i++)
            {
                var y = chartHeight - (i * chartHeight / 5);
                
                // Horizontal grid line
                var gridLine = new System.Windows.Shapes.Line
                {
                    X1 = 50,
                    Y1 = y,
                    X2 = canvasWidth - 20,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    StrokeThickness = 1
                };
                AttendanceChartCanvas.Children.Add(gridLine);

                // Y-axis label
                var label = new TextBlock
                {
                    Text = ((maxValue * i / 5)).ToString(),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175))
                };
                Canvas.SetLeft(label, 20);
                Canvas.SetTop(label, y - 8);
                AttendanceChartCanvas.Children.Add(label);
            }

            // Draw bars
            for (int i = 0; i < data.Count; i++)
            {
                var (day, count) = data[i];
                var barHeight = (count * chartHeight) / maxValue;
                var x = 60 + (i * barWidth);
                var y = chartHeight - barHeight;

                // Bar background
                var barBg = new System.Windows.Shapes.Rectangle
                {
                    Width = barWidth - 15,
                    Height = chartHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                    RadiusX = 6,
                    RadiusY = 6
                };
                Canvas.SetLeft(barBg, x);
                Canvas.SetTop(barBg, 0);
                AttendanceChartCanvas.Children.Add(barBg);

                // Bar
                var bar = new System.Windows.Shapes.Rectangle
                {
                    Width = barWidth - 15,
                    Height = barHeight > 0 ? barHeight : 0,
                    RadiusX = 6,
                    RadiusY = 6
                };
                
                // Gradient fill
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(37, 99, 235), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(59, 130, 246), 1));
                bar.Fill = gradient;

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                AttendanceChartCanvas.Children.Add(bar);

                // Value label on top of bar
                if (count > 0)
                {
                    var valueLabel = new TextBlock
                    {
                        Text = count.ToString(),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235))
                    };
                    Canvas.SetLeft(valueLabel, x + (barWidth - 15) / 2 - 8);
                    Canvas.SetTop(valueLabel, y - 20);
                    AttendanceChartCanvas.Children.Add(valueLabel);
                }

                // Day label
                var dayLabel = new TextBlock
                {
                    Text = day,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                };
                Canvas.SetLeft(dayLabel, x + (barWidth - 15) / 2 - 15);
                Canvas.SetTop(dayLabel, chartHeight + 10);
                AttendanceChartCanvas.Children.Add(dayLabel);
            }
        }
    }
}
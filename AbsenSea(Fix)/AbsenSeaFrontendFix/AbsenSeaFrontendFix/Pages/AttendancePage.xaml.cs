using System;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using YoloDotNet;
using YoloDotNet.Models;
using OpenCvSharp;
using SkiaSharp;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using AbsenSeaFrontendFix.Pages.Database;
using System.Collections.Generic;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class AttendancePage : Page
    {
        private Yolo yolo;
        private VideoCapture? capture;
        private bool webcamRunning = false;
        private CancellationTokenSource? cancellationTokenSource;
        private long currentCrewId = 0;
        private CrewMember currentCrewMember = null;
        private bool helmetDetected = false;
        private bool vestDetected = false;
        private Backend backend;

        // Dynamically created UI elements
        private Image cameraImage;
        private Grid cameraGrid;

        public AttendancePage()
        {
            InitializeComponent();
            InitializeBackendAndYolo();
        }

        private async void InitializeBackendAndYolo()
        {
            try
            {
                // Initialize backend
                backend = await Backend.CreateAsync();
                System.Diagnostics.Debug.WriteLine("Backend initialized successfully");

                // Initialize YOLO
                InitializeYolo();

                // Setup camera display and load crew members
                SetupCameraDisplay();
                await LoadCrewMembers();
                
                // Load dashboard data
                await LoadTodaysSummary();
                await LoadRecentCheckIns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize application: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCrewMembers()
        {
            try
            {
                if (backend == null || CrewIdComboBox == null) return;

                // Fetch all crew members
                var result = await backend.SupabaseClient
                    .From<CrewMember>()
                    .Select("*")
                    .Get();

                var crewMembers = result.Models;

                // Clear existing items (except placeholder items if any)
                CrewIdComboBox.Items.Clear();

                // Populate ComboBox with real data
                foreach (var crew in crewMembers)
                {
                    var displayItem = new ComboBoxItem
                    {
                        Content = $"{crew.CREW_ID} - {crew.CREW_NAME}",
                        Tag = crew
                    };
                    CrewIdComboBox.Items.Add(displayItem);
                }

                if (crewMembers.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded {crewMembers.Count} crew members");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading crew members: {ex.Message}");
                MessageBox.Show($"Failed to load crew members: {ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetupCameraDisplay()
        {
            // Find the camera placeholder border in XAML
            var mainGrid = this.Content as Grid;
            if (mainGrid == null) return;

            FindAndSetupCameraArea(mainGrid);
        }

        private void FindAndSetupCameraArea(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Look for the Border with Background="#1F2937" (camera area)
                if (child is Border border && border.Background is SolidColorBrush brush)
                {
                    if (brush.Color == Color.FromRgb(31, 41, 55)) // #1F2937
                    {
                        // Found the camera border, inject our image
                        if (border.Child is Grid grid)
                        {
                            cameraGrid = grid;
                            cameraImage = new Image
                            {
                                Stretch = System.Windows.Media.Stretch.Uniform,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            // Add to the beginning so it's behind overlays
                            grid.Children.Insert(0, cameraImage);
                        }
                        return;
                    }
                }

                FindAndSetupCameraArea(child);
            }
        }

        private void InitializeYolo()
        {
            try
            {
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "best.onnx");

                if (!File.Exists(modelPath))
                {
                    MessageBox.Show($"YOLO model not found at:\n{modelPath}\n\nPlease ensure 'best.onnx' is in the Resources folder.",
                        "Model Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                yolo = new Yolo(new YoloOptions
                {
                    OnnxModel = modelPath
                });

                System.Diagnostics.Debug.WriteLine($"YOLO model loaded successfully from: {modelPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize YOLO model: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToDashboard_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            var dashboardPage = new DashboardPage();
            NavigationService?.Navigate(dashboardPage);
        }

        private void NavigateToReports_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            var reportsPage = new AttendanceReportPage();
            NavigationService?.Navigate(reportsPage);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            var loginPage = new LoginPage();
            NavigationService?.Navigate(loginPage);
        }

        private void CrewIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Real-time validation when user types
            if (CrewIdComboBox.IsEditable && !string.IsNullOrWhiteSpace(CrewIdComboBox.Text))
            {
                // Check if it matches any crew member
                var matchingItem = CrewIdComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString().Contains(CrewIdComboBox.Text));

                if (matchingItem != null)
                {
                    CrewIdComboBox.Background = Brushes.White;
                }
            }
        }

        private async void VerifyId_Click(object sender, RoutedEventArgs e)
        {
            if (CrewIdComboBox == null || CrewIdComboBox.SelectedItem == null)
            {
                // Try to parse from text if user typed something
                if (!string.IsNullOrWhiteSpace(CrewIdComboBox.Text))
                {
                    // Extract crew ID from text (assuming format "ID - Name")
                    var parts = CrewIdComboBox.Text.Split('-');
                    if (parts.Length > 0 && long.TryParse(parts[0].Trim(), out long crewId))
                    {
                        await LoadCrewMemberById(crewId);
                        return;
                    }
                }

                MessageBox.Show("Please select a crew member from the dropdown.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get selected crew member
            if (CrewIdComboBox.SelectedItem is ComboBoxItem item && item.Tag is CrewMember crew)
            {
                currentCrewMember = crew;
                currentCrewId = crew.CREW_ID;
                DisplayCrewInfo();

                // Automatically start camera after ID verification
                await Task.Delay(500);
                await StartCameraAsync();
            }
        }

        private async Task LoadCrewMemberById(long crewId)
        {
            try
            {
                var result = await backend.SupabaseClient
                    .From<CrewMember>()
                    .Where(c => c.CREW_ID == crewId)
                    .Get();

                if (result.Models.Count > 0)
                {
                    currentCrewMember = result.Models[0];
                    currentCrewId = crewId;
                    DisplayCrewInfo();

                    await Task.Delay(500);
                    await StartCameraAsync();
                }
                else
                {
                    MessageBox.Show($"Crew ID {crewId} not found in database.", "ID Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading crew member: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayCrewInfo()
        {
            CrewInfoPanel.Visibility = Visibility.Visible;
            CrewNameText.Text = currentCrewMember.CREW_NAME ?? "Unknown";
            CrewPositionText.Text = $"Crew ID: {currentCrewId}";
        }

        private async void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            await StartCameraAsync();
        }

        private async Task StartCameraAsync()
        {
            if (yolo == null)
            {
                MessageBox.Show("YOLO model is not initialized. Please check the model file.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentCrewId == 0 || currentCrewMember == null)
            {
                MessageBox.Show("Please verify Crew ID first before starting the camera.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Don't start if already running
            if (webcamRunning)
            {
                return;
            }

            try
            {
                capture = new VideoCapture(0);
                if (!capture.IsOpened())
                {
                    MessageBox.Show("Cannot open webcam! Please check your camera connection.",
                        "Camera Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                webcamRunning = true;
                cancellationTokenSource = new CancellationTokenSource();
                CaptureButton.IsEnabled = true;

                await ProcessWebcamFeed(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting camera: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCamera();
            }
        }

        private async Task ProcessWebcamFeed(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                while (webcamRunning && !token.IsCancellationRequested)
                {
                    try
                    {
                        using Mat frame = new Mat();
                        capture?.Read(frame);

                        if (frame.Empty()) continue;

                        // Convert Mat to SKBitmap for YOLO
                        using var skBitmap = MatToSKBitmap(frame);

                        // Run detection
                        var results = yolo.RunObjectDetection(skBitmap);

                        // Check for helmet and vest
                        helmetDetected = results.Any(r => r.Label.Name.ToLower().Contains("helmet") ||
                                                         r.Label.Name.ToLower().Contains("hard hat") ||
                                                         r.Label.Name.ToLower().Contains("hardhat"));
                        vestDetected = results.Any(r => r.Label.Name.ToLower().Contains("vest") ||
                                                       r.Label.Name.ToLower().Contains("jacket") ||
                                                       r.Label.Name.ToLower().Contains("safety"));

                        // Draw detections on the bitmap
                        var displayBitmap = DrawDetections(skBitmap, results);

                        // Update UI on UI thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            UpdateCameraFeed(displayBitmap);
                            UpdateDetectionStatus(helmetDetected, vestDetected);
                        });

                        displayBitmap?.Freeze();
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing frame: {ex.Message}");
                        });
                    }

                    // Delay to control frame rate (~30 FPS)
                    await Task.Delay(33, token);
                }
            }, token);
        }

        private BitmapSource DrawDetections(SKBitmap skBitmap, IEnumerable<ObjectDetection> results)
        {
            using var surface = SKSurface.Create(new SKImageInfo(skBitmap.Width, skBitmap.Height));
            var canvas = surface.Canvas;

            canvas.Clear();
            canvas.DrawBitmap(skBitmap, 0, 0);

            var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4,
                IsAntialias = true
            };

            var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 24,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            foreach (var det in results)
            {
                // Different colors for different equipment types
                bool isHelmet = det.Label.Name.ToLower().Contains("helmet") ||
                               det.Label.Name.ToLower().Contains("hard hat");
                bool isVest = det.Label.Name.ToLower().Contains("vest") ||
                             det.Label.Name.ToLower().Contains("jacket");

                paint.Color = isHelmet ? SKColors.LimeGreen :
                             isVest ? SKColors.Yellow : SKColors.Red;

                var rect = new SKRect(
                    det.BoundingBox.Left,
                    det.BoundingBox.Top,
                    det.BoundingBox.Left + det.BoundingBox.Width,
                    det.BoundingBox.Top + det.BoundingBox.Height
                );

                canvas.DrawRect(rect, paint);

                string label = $"{det.Label.Name} {det.Confidence:P0}";

                var bgPaint = new SKPaint
                {
                    Color = paint.Color,
                    Style = SKPaintStyle.Fill
                };

                var textBounds = new SKRect();
                textPaint.MeasureText(label, ref textBounds);

                float labelWidth = textBounds.Width + 20;
                float labelHeight = textBounds.Height + 20;

                canvas.DrawRect(
                    det.BoundingBox.Left,
                    det.BoundingBox.Top - labelHeight - 5,
                    labelWidth,
                    labelHeight,
                    bgPaint
                );

                textPaint.Color = SKColors.Black;
                canvas.DrawText(label,
                    det.BoundingBox.Left + 10,
                    det.BoundingBox.Top - 10,
                    textPaint);
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private void UpdateCameraFeed(BitmapSource bitmap)
        {
            if (cameraImage != null)
            {
                cameraImage.Source = bitmap;
                cameraImage.Visibility = Visibility.Visible;
            }
        }

        private void UpdateDetectionStatus(bool helmet, bool vest)
        {
            if (helmet && vest)
            {
                DetectionStatus.Visibility = Visibility.Visible;
                DetectionStatus.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                DetectionStatusText.Text = "✓ Equipment Detected";
                DetectionDetailsText.Text = "Helmet ✓ • Vest ✓";
            }
            else if (helmet || vest)
            {
                DetectionStatus.Visibility = Visibility.Visible;
                DetectionStatus.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Orange
                DetectionStatusText.Text = "⚠ Partial Detection";
                DetectionDetailsText.Text = $"Helmet {(helmet ? "✓" : "✗")} • Vest {(vest ? "✓" : "✗")}";
            }
            else
            {
                DetectionStatus.Visibility = Visibility.Visible;
                DetectionStatus.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                DetectionStatusText.Text = "✗ Equipment Missing";
                DetectionDetailsText.Text = "Helmet ✗ • Vest ✗";
            }
        }

        private async void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (currentCrewId == 0 || currentCrewMember == null)
            {
                MessageBox.Show("Please select and verify a crew member first.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Open file dialog to select image
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Crew Photo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;

                    // Load image and process with YOLO
                    using var skBitmap = SKBitmap.Decode(filePath);
                    if (skBitmap == null)
                    {
                        MessageBox.Show("Failed to load image file.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var results = yolo.RunObjectDetection(skBitmap);

                    // Check for helmet and vest
                    bool helmet = results.Any(r => r.Label.Name.ToLower().Contains("helmet") ||
                                                   r.Label.Name.ToLower().Contains("hard hat") ||
                                                   r.Label.Name.ToLower().Contains("hardhat"));
                    bool vest = results.Any(r => r.Label.Name.ToLower().Contains("vest") ||
                                                 r.Label.Name.ToLower().Contains("jacket") ||
                                                 r.Label.Name.ToLower().Contains("safety"));

                    // Save to database
                    await SaveAttendanceRecord(currentCrewId, helmet, vest, true);

                    bool isCompliant = helmet && vest;
                    string status = isCompliant ? "Compliant" : "Non-Compliant";
                    string equipment = $"Helmet {(helmet ? "✓" : "✗")} | Vest {(vest ? "✓" : "✗")}";

                    MessageBox.Show(
                        $"Photo processed and attendance recorded!\n\n" +
                        $"Crew ID: {currentCrewId}\n" +
                        $"Name: {currentCrewMember.CREW_NAME}\n" +
                        $"Status: {status}\n" +
                        $"Equipment: {equipment}\n" +
                        $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        isCompliant ? "Success" : "Warning",
                        MessageBoxButton.OK,
                        isCompliant ? MessageBoxImage.Information : MessageBoxImage.Warning
                    );

                    // Update UI
                    UpdateDetectionStatus(helmet, vest);

                    // Reset for next crew member
                    ResetForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing photo: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CaptureAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (!webcamRunning)
            {
                MessageBox.Show("Please start the camera first.", "Camera Not Active",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (currentCrewId == 0 || currentCrewMember == null)
            {
                MessageBox.Show("Please select and verify a crew member first.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Stop camera first
                StopCamera();

                // Wait a moment for camera to fully stop
                await Task.Delay(300);

                // Save attendance record to database AFTER stopping camera
                await SaveAttendanceRecord(currentCrewId, helmetDetected, vestDetected, true);

                // Capture current detection state
                bool isCompliant = helmetDetected && vestDetected;
                string status = isCompliant ? "Compliant" : "Non-Compliant";
                string equipment = $"Helmet {(helmetDetected ? "✓" : "✗")} | Vest {(vestDetected ? "✓" : "✗")}";

                MessageBox.Show(
                    $"Attendance recorded successfully!\n\n" +
                    $"Crew ID: {currentCrewId}\n" +
                    $"Name: {currentCrewMember.CREW_NAME}\n" +
                    $"Status: {status}\n" +
                    $"Equipment: {equipment}\n" +
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    isCompliant ? "Success" : "Warning",
                    MessageBoxButton.OK,
                    isCompliant ? MessageBoxImage.Information : MessageBoxImage.Warning
                );

                // Reset for next crew member
                ResetForm();
                
                // Refresh the summary and recent check-ins
                await LoadTodaysSummary();
                await LoadRecentCheckIns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving attendance record: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetForm()
        {
            currentCrewId = 0;
            currentCrewMember = null;

            if (CrewIdComboBox != null)
            {
                CrewIdComboBox.SelectedItem = null;
                CrewIdComboBox.Text = "";
            }

            CrewInfoPanel.Visibility = Visibility.Collapsed;
            DetectionStatus.Visibility = Visibility.Collapsed;
            helmetDetected = false;
            vestDetected = false;

            if (cameraImage != null)
            {
                cameraImage.Source = null;
                cameraImage.Visibility = Visibility.Collapsed;
            }
        }

        private void StopCamera()
        {
            webcamRunning = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            capture?.Release();
            capture?.Dispose();
            capture = null;

            CaptureButton.IsEnabled = false;

            if (cameraImage != null)
            {
                cameraImage.Visibility = Visibility.Collapsed;
            }
        }

        // Helper method to convert Mat to SKBitmap
        private SKBitmap MatToSKBitmap(Mat mat)
        {
            byte[] imageData = new byte[mat.Total() * mat.ElemSize()];
            Marshal.Copy(mat.Data, imageData, 0, imageData.Length);

            var info = new SKImageInfo(mat.Width, mat.Height, SKColorType.Bgra8888);
            var skBitmap = new SKBitmap(info);

            if (mat.Channels() == 3)
            {
                using var bgraMat = new Mat();
                Cv2.CvtColor(mat, bgraMat, ColorConversionCodes.BGR2BGRA);

                byte[] bgraData = new byte[bgraMat.Total() * bgraMat.ElemSize()];
                Marshal.Copy(bgraMat.Data, bgraData, 0, bgraData.Length);
                Marshal.Copy(bgraData, 0, skBitmap.GetPixels(), bgraData.Length);
            }
            else
            {
                Marshal.Copy(imageData, 0, skBitmap.GetPixels(), imageData.Length);
            }

            return skBitmap;
        }

        // Save attendance record to Supabase database using Backend
        private async Task SaveAttendanceRecord(long crewId, bool hasHelmet, bool hasVest, bool present)
        {
            try
            {
                var now = DateTime.Now;
                // Store CHECK_DATE at noon to avoid timezone conversion issues
                var checkDate = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0);
                
                var newCheck = new CrewCheck
                {
                    CREW_ID = crewId,
                    CAPTURE_AT = now,
                    CHECK_DATE = checkDate,
                    PRESENT = present,
                    HELMET = hasHelmet,
                    VEST = hasVest
                };

                var result = await backend.InsertCrewCheck(newCheck);

                if (result != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Attendance record saved successfully. Check ID: {result.CHECK_ID}");
                }
                else
                {
                    throw new Exception("Failed to insert crew check - no result returned");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving attendance: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }

        // Load today's summary from database
        private async Task LoadTodaysSummary()
        {
            try
            {
                var today = DateTime.Today;

                // Get all recent checks and filter by date locally to handle timezone issues
                var checksResult = await backend.SupabaseClient
                    .From<CrewCheck>()
                    .Select("*")
                    .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(100)
                    .Get();
                
                // Filter to today's checks (comparing date part only)
                var todayChecks = checksResult.Models
                    .Where(c => c.CHECK_DATE.HasValue && c.CHECK_DATE.Value.Date == today)
                    .ToList();

                var totalCheckins = todayChecks.Count;
                var compliant = todayChecks.Count(c => c.HELMET == true && c.VEST == true);
                var nonCompliant = todayChecks.Count(c => !(c.HELMET == true && c.VEST == true));

                // Find TextBlocks in the XAML by their names
                // We need to add x:Name to the summary TextBlocks in XAML
                // For now, we'll traverse the visual tree to find them
                UpdateSummaryUI(totalCheckins, compliant, nonCompliant);

                System.Diagnostics.Debug.WriteLine($"Today's summary loaded: {totalCheckins} total, {compliant} compliant, {nonCompliant} non-compliant");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading today's summary: {ex.Message}");
            }
        }

        private void UpdateSummaryUI(int total, int compliant, int nonCompliant)
        {
            // Find the summary panel in the visual tree
            var summaryBorder = FindVisualChild<Border>(this, b => 
            {
                var stack = b.Child as StackPanel;
                if (stack != null && stack.Children.Count > 0)
                {
                    var first = stack.Children[0] as TextBlock;
                    return first?.Text == "Today's Summary";
                }
                return false;
            });

            if (summaryBorder != null && summaryBorder.Child is StackPanel summaryPanel)
            {
                // Update the values in the grids
                var grids = summaryPanel.Children.OfType<Grid>().ToList();
                if (grids.Count >= 3)
                {
                    // Total check-ins (first grid)
                    var totalText = grids[0].Children.OfType<TextBlock>().FirstOrDefault(tb => tb.HorizontalAlignment == HorizontalAlignment.Right);
                    if (totalText != null) totalText.Text = total.ToString();

                    // Compliant (second grid after separator)
                    var compliantText = grids[1].Children.OfType<TextBlock>().FirstOrDefault(tb => tb.HorizontalAlignment == HorizontalAlignment.Right);
                    if (compliantText != null) compliantText.Text = compliant.ToString();

                    // Non-compliant (third grid)
                    var nonCompliantText = grids[2].Children.OfType<TextBlock>().FirstOrDefault(tb => tb.HorizontalAlignment == HorizontalAlignment.Right);
                    if (nonCompliantText != null) nonCompliantText.Text = nonCompliant.ToString();
                }
            }
        }

        // Load recent check-ins from database
        private async Task LoadRecentCheckIns()
        {
            try
            {
                var today = DateTime.Today;

                // Get recent checks
                var checksResult = await backend.SupabaseClient
                    .From<CrewCheck>()
                    .Select("*")
                    .Order("CAPTURE_AT", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(20)
                    .Get();
                
                // Filter to today's checks (comparing date part only)
                var recentChecks = checksResult.Models
                    .Where(c => c.CHECK_DATE.HasValue && c.CHECK_DATE.Value.Date == today)
                    .Take(5)
                    .ToList();

                // Get crew members for lookup
                var crewResult = await backend.SupabaseClient
                    .From<CrewMember>()
                    .Select("*")
                    .Get();

                var crewLookup = crewResult.Models.ToDictionary(c => c.CREW_ID, c => c);

                // Find the recent check-ins panel
                var recentBorder = FindVisualChild<Border>(this, b =>
                {
                    var stack = b.Child as StackPanel;
                    if (stack != null && stack.Children.Count > 0)
                    {
                        var first = stack.Children[0] as TextBlock;
                        return first?.Text == "Recent Check-ins";
                    }
                    return false;
                });

                if (recentBorder != null && recentBorder.Child is StackPanel recentPanel && recentPanel.Children.Count > 1)
                {
                    // Remove old check-in items (keep only the title)
                    while (recentPanel.Children.Count > 1)
                    {
                        recentPanel.Children.RemoveAt(1);
                    }

                    // Add new check-in items
                    foreach (var check in recentChecks)
                    {
                        if (check.CREW_ID == null) continue;

                        CrewMember? crewMember = null;
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

                        var missingItems = new List<string>();
                        if (check.HELMET != true) missingItems.Add("helmet");
                        if (check.VEST != true) missingItems.Add("vest");
                        var statusText = missingItems.Count > 0 ? $" • No {string.Join(", ", missingItems)}" : "";

                        var checkInItem = CreateCheckInItem(crewMember.CREW_NAME, isCompliant, timeText, statusText);
                        recentPanel.Children.Add(checkInItem);
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {recentChecks.Count} recent check-ins");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading recent check-ins: {ex.Message}");
            }
        }

        private Border CreateCheckInItem(string crewName, bool isCompliant, string timeAgo, string statusText)
        {
            var borderThickness = new Thickness(0, 0, 0, 1);
            var padding = new Thickness(0, 0, 0, 12);
            var margin = new Thickness(0, 0, 0, 12);

            var itemBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = borderThickness,
                Padding = padding,
                Margin = margin
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon
            var iconBorder = new Border
            {
                Background = new SolidColorBrush(isCompliant ? Color.FromRgb(209, 250, 229) : Color.FromRgb(254, 226, 226)),
                Width = 35,
                Height = 35,
                CornerRadius = new CornerRadius(17.5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            var iconText = new TextBlock
            {
                Text = isCompliant ? "✓" : "⚠",
                FontSize = 16,
                Foreground = new SolidColorBrush(isCompliant ? Color.FromRgb(16, 185, 129) : Color.FromRgb(239, 68, 68)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);

            // Details
            var detailsPanel = new StackPanel();
            var nameText = new TextBlock
            {
                Text = crewName,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };
            var timeStatusText = new TextBlock
            {
                Text = timeAgo + statusText,
                FontSize = 11,
                Foreground = new SolidColorBrush(isCompliant ? Color.FromRgb(156, 163, 175) : Color.FromRgb(239, 68, 68))
            };
            detailsPanel.Children.Add(nameText);
            detailsPanel.Children.Add(timeStatusText);
            Grid.SetColumn(detailsPanel, 1);

            grid.Children.Add(iconBorder);
            grid.Children.Add(detailsPanel);
            itemBorder.Child = grid;

            return itemBorder;
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

        // Helper method to find visual children
        private T? FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild && predicate(typedChild))
                {
                    return typedChild;
                }

                var foundChild = FindVisualChild(child, predicate);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }

            return null;
        }
    }
}
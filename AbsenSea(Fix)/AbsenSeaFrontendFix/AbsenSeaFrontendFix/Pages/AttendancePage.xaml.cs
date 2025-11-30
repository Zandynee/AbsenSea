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
                var newCheck = new CrewCheck
                {
                    CREW_ID = crewId,
                    CAPTURE_AT = DateTime.UtcNow,
                    CHECK_DATE = DateTime.Today,
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
    }
}
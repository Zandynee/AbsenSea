using YoloDotNet;
using YoloDotNet.Models;
using OpenCvSharp;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace AbsenSea_Yolo
{
    public partial class Form1 : Form
    {
        private Yolo yolo;
        private VideoCapture? capture;
        private bool webcamRunning = false;

        public Form1()
        {
            InitializeComponent();
            string modelPath = Path.Combine(Application.StartupPath, "best.onnx");

            yolo = new Yolo(new YoloOptions
            {
                OnnxModel = modelPath
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        // Helper method to convert System.Drawing.Bitmap to SKBitmap (safe code)
        private SKBitmap BitmapToSKBitmap(Bitmap bitmap)
        {
            var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888);
            var skBitmap = new SKBitmap(info);

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            IntPtr skPtr = skBitmap.GetPixels();
            int bytes = bitmapData.Stride * bitmap.Height;

            // Safe copy using Marshal
            Marshal.Copy(bitmapData.Scan0,
                        new byte[bytes], 0, bytes);
            byte[] buffer = new byte[bytes];
            Marshal.Copy(bitmapData.Scan0, buffer, 0, bytes);
            Marshal.Copy(buffer, 0, skPtr, bytes);

            bitmap.UnlockBits(bitmapData);
            return skBitmap;
        }

        // Helper method to convert Mat to SKBitmap
        private SKBitmap MatToSKBitmap(Mat mat)
        {
            // Convert Mat to byte array
            byte[] imageData = new byte[mat.Total() * mat.ElemSize()];
            Marshal.Copy(mat.Data, imageData, 0, imageData.Length);

            // Create SKBitmap
            var info = new SKImageInfo(mat.Width, mat.Height, SKColorType.Bgra8888);
            var skBitmap = new SKBitmap(info);

            // Convert BGR to BGRA if needed
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

        // Helper method to convert SKBitmap to System.Drawing.Bitmap for display
        private Bitmap SKBitmapToBitmap(SKBitmap skBitmap)
        {
            var bitmap = new Bitmap(skBitmap.Width, skBitmap.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            IntPtr skPtr = skBitmap.GetPixels();
            int bytes = bitmapData.Stride * bitmap.Height;

            // Safe copy using Marshal
            byte[] buffer = new byte[bytes];
            Marshal.Copy(skPtr, buffer, 0, bytes);
            Marshal.Copy(buffer, 0, bitmapData.Scan0, bytes);

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                using var originalBitmap = new Bitmap(ofd.FileName);
                using var skBitmap = BitmapToSKBitmap(originalBitmap);

                // Run detection
                var results = yolo.RunObjectDetection(skBitmap);

                // Draw on a new bitmap
                var resultBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
                using (Graphics g = Graphics.FromImage(resultBitmap))
                {
                    g.DrawImage(originalBitmap, 0, 0);

                    Pen pen = new Pen(Color.LimeGreen, 3);
                    Font font = new Font("Arial", 14, FontStyle.Bold);

                    foreach (var det in results)
                    {
                        Rectangle rect = new Rectangle(
                            det.BoundingBox.Left,
                            det.BoundingBox.Top,
                            det.BoundingBox.Width,
                            det.BoundingBox.Height
                        );

                        g.DrawRectangle(pen, rect);

                        string label = $"{det.Label.Name} {det.Confidence:P1}";
                        var textSize = g.MeasureString(label, font);

                        // Draw background for text
                        g.FillRectangle(Brushes.LimeGreen,
                            det.BoundingBox.Left,
                            det.BoundingBox.Top - textSize.Height - 2,
                            textSize.Width,
                            textSize.Height);

                        g.DrawString(label, font, Brushes.Black,
                            det.BoundingBox.Left,
                            det.BoundingBox.Top - textSize.Height - 2);
                    }
                }

                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();

                pictureBox1.Image = resultBitmap;
            }
        }

        private async void btnWebcam_Click(object sender, EventArgs e)
        {
            capture = new VideoCapture(0);
            if (!capture.IsOpened())
            {
                MessageBox.Show("Cannot open webcam!");
                return;
            }

            webcamRunning = true;
            btnWebcam.Enabled = false;
            btnStopWebcam.Enabled = true;

            await Task.Run(() =>
            {
                while (webcamRunning)
                {
                    using Mat frame = new Mat();
                    capture.Read(frame);

                    if (frame.Empty()) continue;

                    // Convert Mat to SKBitmap for YOLO
                    using var skBitmap = MatToSKBitmap(frame);

                    // Run detection
                    var results = yolo.RunObjectDetection(skBitmap);

                    // Convert to regular Bitmap for drawing
                    var displayBitmap = SKBitmapToBitmap(skBitmap);

                    using (Graphics g = Graphics.FromImage(displayBitmap))
                    {
                        Pen pen = new Pen(Color.Red, 3);
                        Font font = new Font("Arial", 12, FontStyle.Bold);

                        foreach (var det in results)
                        {
                            Rectangle rect = new Rectangle(
                                det.BoundingBox.Left,
                                det.BoundingBox.Top,
                                det.BoundingBox.Width,
                                det.BoundingBox.Height
                            );

                            g.DrawRectangle(pen, rect);

                            string label = $"{det.Label.Name} {det.Confidence:P1}";
                            var textSize = g.MeasureString(label, font);

                            // Draw background for text
                            g.FillRectangle(Brushes.Red,
                                det.BoundingBox.Left,
                                det.BoundingBox.Top - textSize.Height - 2,
                                textSize.Width,
                                textSize.Height);

                            g.DrawString(label, font, Brushes.White,
                                det.BoundingBox.Left,
                                det.BoundingBox.Top - textSize.Height - 2);
                        }
                    }

                    // Update UI on UI thread
                    if (pictureBox1.InvokeRequired)
                    {
                        pictureBox1.Invoke(new Action(() =>
                        {
                            var oldImage = pictureBox1.Image;
                            pictureBox1.Image = displayBitmap;
                            oldImage?.Dispose();
                        }));
                    }
                    else
                    {
                        var oldImage = pictureBox1.Image;
                        pictureBox1.Image = displayBitmap;
                        oldImage?.Dispose();
                    }

                    // Small delay to prevent overwhelming the system
                    Thread.Sleep(30);
                }
            });
        }

        private void btnStopWebcam_Click(object sender, EventArgs e)
        {
            webcamRunning = false;
            capture?.Release();
            capture?.Dispose();
            capture = null;

            btnWebcam.Enabled = true;
            btnStopWebcam.Enabled = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webcamRunning = false;
            capture?.Release();
            capture?.Dispose();
            yolo?.Dispose();
            base.OnFormClosing(e);
        }


    }
}
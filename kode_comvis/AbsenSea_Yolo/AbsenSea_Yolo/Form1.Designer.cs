namespace AbsenSea_Yolo
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            btnLoadImage = new Button();
            btnWebcam = new Button();
            btnStopWebcam = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(249, 38);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(530, 381);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // btnLoadImage
            // 
            btnLoadImage.Location = new Point(12, 38);
            btnLoadImage.Name = "btnLoadImage";
            btnLoadImage.Size = new Size(190, 58);
            btnLoadImage.TabIndex = 1;
            btnLoadImage.Text = "Load Image";
            btnLoadImage.UseVisualStyleBackColor = true;
            btnLoadImage.Click += btnLoadImage_Click;
            // 
            // btnWebcam
            // 
            btnWebcam.Location = new Point(12, 109);
            btnWebcam.Name = "btnWebcam";
            btnWebcam.Size = new Size(190, 60);
            btnWebcam.TabIndex = 2;
            btnWebcam.Text = "Start Webcam";
            btnWebcam.UseVisualStyleBackColor = true;
            btnWebcam.Click += btnWebcam_Click;
            // 
            // btnStopWebcam
            // 
            btnStopWebcam.Location = new Point(12, 192);
            btnStopWebcam.Name = "btnStopWebcam";
            btnStopWebcam.Size = new Size(190, 66);
            btnStopWebcam.TabIndex = 3;
            btnStopWebcam.Text = "Stop Camera";
            btnStopWebcam.UseVisualStyleBackColor = true;
            btnStopWebcam.Click += btnStopWebcam_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnStopWebcam);
            Controls.Add(btnWebcam);
            Controls.Add(btnLoadImage);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private Button btnLoadImage;
        private Button btnWebcam;
        private Button btnStopWebcam;
    }
}

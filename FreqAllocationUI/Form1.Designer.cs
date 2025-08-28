namespace FreqAllocationUI
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
            Text = "Akıllı Frekans Tahsis Sistemi";
            Size = new Size(1200, 700);
            StartPosition = FormStartPosition.CenterScreen;

            // Spektrum görselleştirme
            spectrumPictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(1160, 300),
                BorderStyle = BorderStyle.FixedSingle
            };
            spectrumPictureBox.Paint += SpectrumPictureBox_Paint;

            // Tahsis listesi
            allocationListBox = new ListBox
            {
                Location = new Point(10, 320),
                Size = new Size(700, 250),
                Font = new Font("Consolas", 9)
            };

            // Kontrol paneli
            var controlPanel = new Panel
            {
                Location = new Point(720, 320),
                Size = new Size(450, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            var bandwidthLabel = new Label
            {
                Text = "Band Genişliği (MHz):",
                Location = new Point(10, 20),
                Size = new Size(150, 25)
            };

            bandwidthTextBox = new TextBox
            {
                Location = new Point(170, 20),
                Size = new Size(100, 25),
                Text = "20"
            };

            allocateButton = new Button
            {
                Text = "Frekans Tahsis Et",
                Location = new Point(10, 60),
                Size = new Size(260, 40),
                BackColor = Color.Green,
                ForeColor = Color.White
            };
            allocateButton.Click += AllocateButton_Click;

            deallocateButton = new Button
            {
                Text = "Seçili Tahsisi Kaldır",
                Location = new Point(10, 110),
                Size = new Size(260, 40),
                BackColor = Color.Red,
                ForeColor = Color.White
            };
            deallocateButton.Click += DeallocateButton_Click;

            utilizationLabel = new Label
            {
                Text = "Spektrum Kullanımı: 0%",
                Location = new Point(10, 170),
                Size = new Size(260, 25),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            controlPanel.Controls.AddRange(new Control[] {
                bandwidthLabel, bandwidthTextBox, allocateButton,
                deallocateButton, utilizationLabel
            });

            // Status bar
            var statusLabel = new Label
            {
                Text = "Hazır - Frekans aralığı: " + allocator.GetType().GetField("minFreq",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(allocator) +
                    " - " + allocator.GetType().GetField("maxFreq",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(allocator) + " GHz",
                Location = new Point(10, 580),
                Size = new Size(1160, 25),
                BorderStyle = BorderStyle.Fixed3D
            };

            Controls.AddRange(new Control[] {
                spectrumPictureBox, allocationListBox, controlPanel, statusLabel
            });
        }

        #endregion
    }
}

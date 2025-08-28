using AIDrivenFrequencyAllocation;
using Timer = System.Windows.Forms.Timer;

namespace FreqAllocationUI
{
    public partial class Form1 : Form
    {
        private IntelligentFrequencyAllocator allocator;
        private PictureBox spectrumPictureBox;
        private ListBox allocationListBox;
        private TextBox bandwidthTextBox;
        private Button allocateButton;
        private Button deallocateButton;
        private Label utilizationLabel;
        private Timer refreshTimer;

        public Form1(double minFreq, double maxFreq)
        {
            allocator = new IntelligentFrequencyAllocator(minFreq, maxFreq);
            InitializeComponent();
            SetupTimer();
        }

        private void SetupTimer()
        {
            refreshTimer = new Timer { Interval = 100 };
            refreshTimer.Tick += (s, e) => {
                spectrumPictureBox.Invalidate();
                UpdateUtilizationLabel();
            };
            refreshTimer.Start();
        }

        private void AllocateButton_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(bandwidthTextBox.Text, out double bandwidth))
            {
                MessageBox.Show("Geçersiz band geniþliði!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var request = new FrequencyAllocationRequest
            {
                Bandwidth = bandwidth / 1000.0, // MHz to GHz
                RequestId = Guid.NewGuid().ToString().Substring(0, 8),
                RequestTime = DateTime.Now,
                Priority = 1
            };

            var allocation = allocator.AllocateFrequency(request);

            if (allocation != null)
            {
                string info = $"ID: {allocation.AllocationId} | " +
                            $"Merkez: {allocation.CenterFrequency:F3} GHz | " +
                            $"Band: {allocation.Bandwidth * 1000:F1} MHz | " +
                            $"Aralýk: [{allocation.StartFreq:F3} - {allocation.EndFreq:F3}] GHz";

                allocationListBox.Items.Add(info);

                MessageBox.Show($"Tahsis baþarýlý!\nMerkez Frekansý: {allocation.CenterFrequency:F3} GHz\n" +
                              $"Band Geniþliði: {allocation.Bandwidth * 1000:F1} MHz",
                              "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Uygun frekans bulunamadý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DeallocateButton_Click(object sender, EventArgs e)
        {
            if (allocationListBox.SelectedItem != null)
            {
                string selectedText = allocationListBox.SelectedItem.ToString();
                string id = selectedText.Split('|')[0].Replace("ID:", "").Trim();

                allocator.DeallocateFrequency(id);
                allocationListBox.Items.Remove(allocationListBox.SelectedItem);

                MessageBox.Show("Tahsis kaldýrýldý!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateUtilizationLabel()
        {
            double utilization = allocator.GetSpectrumUtilization();
            utilizationLabel.Text = $"Spektrum Kullanýmý: {utilization:F1}%";
            utilizationLabel.ForeColor = utilization > 80 ? Color.Red :
                                       utilization > 50 ? Color.Orange : Color.Green;
        }

        private void SpectrumPictureBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);

            var allocations = allocator.GetAllocatedBlocks();
            float width = spectrumPictureBox.Width - 40;
            float height = spectrumPictureBox.Height - 40;

            // Get frequency range (using reflection for demo - in real code make these public)
            var minFreqField = allocator.GetType().GetField("minFreq",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxFreqField = allocator.GetType().GetField("maxFreq",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            double minFreq = (double)(minFreqField?.GetValue(allocator) ?? 1.0);
            double maxFreq = (double)(maxFreqField?.GetValue(allocator) ?? 6.0);
            double freqRange = maxFreq - minFreq;

            // Arka plan ýzgarasý
            using (var gridPen = new Pen(Color.LightGray, 1))
            {
                for (int i = 0; i <= 10; i++)
                {
                    float x = 20 + (width / 10) * i;
                    g.DrawLine(gridPen, x, 20, x, 20 + height);

                    double freq = minFreq + (freqRange / 10) * i;
                    g.DrawString($"{freq:F1}", new Font("Arial", 8), Brushes.Black, x - 15, height + 25);
                }
            }

            // Tahsis edilmiþ bloklarý çiz
            var colors = new[] { Color.Blue, Color.Red, Color.Green, Color.Orange, Color.Purple,
                                Color.Brown, Color.Pink, Color.Cyan, Color.Magenta, Color.Yellow };
            int colorIndex = 0;

            foreach (var block in allocations)
            {
                float startX = 20 + (float)((block.StartFreq - minFreq) / freqRange * width);
                float endX = 20 + (float)((block.EndFreq - minFreq) / freqRange * width);
                float blockWidth = endX - startX;

                using (var brush = new SolidBrush(Color.FromArgb(180, colors[colorIndex % colors.Length])))
                {
                    g.FillRectangle(brush, startX, 20, blockWidth, height);
                }

                using (var pen = new Pen(colors[colorIndex % colors.Length], 2))
                {
                    g.DrawRectangle(pen, startX, 20, blockWidth, height);
                }

                // Merkez frekansý göster
                float centerX = startX + blockWidth / 2;
                using (var centerPen = new Pen(Color.Black, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    g.DrawLine(centerPen, centerX, 20, centerX, 20 + height);
                }

                // Bilgi etiketi
                string label = $"{block.CenterFrequency:F2} GHz\n{block.Bandwidth * 1000:F0} MHz";
                var labelSize = g.MeasureString(label, new Font("Arial", 8));

                float labelX = centerX - labelSize.Width / 2;
                float labelY = 20 + height / 2 - labelSize.Height / 2;

                g.FillRectangle(Brushes.White, labelX - 2, labelY - 2, labelSize.Width + 4, labelSize.Height + 4);
                g.DrawString(label, new Font("Arial", 8), Brushes.Black, labelX, labelY);

                colorIndex++;
            }

            // Baþlýk ve eksen etiketleri
            g.DrawString("Frekans Spektrumu (GHz)", new Font("Arial", 12, FontStyle.Bold),
                        Brushes.Black, width / 2 - 50, 2);

            // Çerçeve
            using (var framePen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(framePen, 20, 20, width, height);
            }
        }
    }
}

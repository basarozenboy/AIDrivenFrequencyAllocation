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
        private Button batchAllocateButton;
        private Label utilizationLabel;
        private Timer refreshTimer;

        public Form1(double minFreq, double maxFreq)
        {
            allocator = new IntelligentFrequencyAllocator(minFreq, maxFreq);
            InitializeComponents();
            SetupTimer();
        }

        private void BatchAllocateButton_Click(object? sender, EventArgs e)
        {
            // Toplu tahsis dialog'u
            var batchDialog = new Form
            {
                Text = "Toplu Frekans Tahsisi",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            var instructionLabel = new Label
            {
                Text = "Tahsis edilecek frekansların band genişliklerini girin:\n(Her satıra bir band genişliği - MHz)",
                Location = new Point(10, 10),
                Size = new Size(460, 40)
            };

            var batchTextBox = new TextBox
            {
                Location = new Point(10, 55),
                Size = new Size(460, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10)
            };

            // Örnek veriler ekle
            //batchTextBox.Text = "20\n30\n40\n25\n35\n50";

            var quickFillLabel = new Label
            {
                Text = "Hızlı Doldurma:",
                Location = new Point(10, 265),
                Size = new Size(100, 25),
                Visible = false
            };

            var countTextBox = new TextBox
            {
                Location = new Point(115, 265),
                Size = new Size(50, 25),
                Text = "6",
                Visible = false
            };

            var minBandTextBox = new TextBox
            {
                Location = new Point(170, 265),
                Size = new Size(50, 25),
                Text = "10",
                Visible = false
            };

            var maxBandTextBox = new TextBox
            {
                Location = new Point(225, 265),
                Size = new Size(50, 25),
                Text = "50",
                Visible = false
            };

            var randomFillButton = new Button
            {
                Text = "Rastgele Doldur",
                Location = new Point(285, 263),
                Size = new Size(120, 28),
                BackColor = Color.Orange,
                Visible = false
            };

            randomFillButton.Click += (s, ev) =>
            {
                if (int.TryParse(countTextBox.Text, out int count) &&
                    double.TryParse(minBandTextBox.Text, out double minBand) &&
                    double.TryParse(maxBandTextBox.Text, out double maxBand))
                {
                    var random = new Random();
                    var bands = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        double band = minBand + (maxBand - minBand) * random.NextDouble();
                        bands.Add(band.ToString("F0"));
                    }
                    batchTextBox.Text = string.Join("\n", bands);
                }
            };

            var executeButton = new Button
            {
                Text = "Toplu Tahsis Yap",
                Location = new Point(10, 300),
                Size = new Size(200, 40),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            var cancelButton = new Button
            {
                Text = "İptal",
                Location = new Point(220, 300),
                Size = new Size(100, 40)
            };

            executeButton.Click += (s, ev) =>
            {
                var lines = batchTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var requests = new List<FrequencyAllocationRequest>();

                foreach (var line in lines)
                {
                    if (double.TryParse(line.Trim(), out double bandwidth))
                    {
                        requests.Add(new FrequencyAllocationRequest
                        {
                            Bandwidth = bandwidth / 1000.0, // MHz to GHz
                            RequestId = Guid.NewGuid().ToString().Substring(0, 8),
                            RequestTime = DateTime.Now,
                            Priority = 1
                        });
                    }
                }

                if (requests.Count > 0)
                {
                    // Yapay zeka ile toplu tahsis
                    var result = allocator.AllocateBatch(requests);

                    // Sonuçları göster
                    var resultMessage = $"Toplu Tahsis Sonucu:\n" +
                                      $"✅ Başarılı: {result.SuccessfulAllocations.Count}\n" +
                                      $"❌ Başarısız: {result.FailedRequests.Count}\n" +
                                      $"📊 Fragmentasyon: {result.TotalFragmentation:P1}\n" +
                                      $"🎯 Optimizasyon Skoru: {result.OptimizationScore:F1}/100\n\n";

                    if (result.SuccessfulAllocations.Count > 0)
                    {
                        resultMessage += "Tahsis Edilen Frekanslar:\n";
                        foreach (var allocation in result.SuccessfulAllocations)
                        {
                            string info = $"ID: {allocation.AllocationId} | " +
                                        $"Merkez: {allocation.CenterFrequency:F3} GHz | " +
                                        $"Band: {allocation.Bandwidth * 1000:F1} MHz | " +
                                        $"Aralık: [{allocation.StartFreq:F3} - {allocation.EndFreq:F3}] GHz";

                            allocationListBox.Items.Add(info);
                            resultMessage += $"• {allocation.CenterFrequency:F3} GHz ({allocation.Bandwidth * 1000:F0} MHz)\n";
                        }
                    }

                    if (result.FailedRequests.Count > 0)
                    {
                        resultMessage += "\nTahsis Edilemeyen Bandlar:\n";
                        foreach (var failed in result.FailedRequests)
                        {
                            resultMessage += $"• {failed.Bandwidth * 1000:F0} MHz\n";
                        }
                    }

                    MessageBox.Show(resultMessage, "Toplu Tahsis Sonucu",
                                  MessageBoxButtons.OK,
                                  result.FailedRequests.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                    batchDialog.Close();
                }
                else
                {
                    MessageBox.Show("Geçerli band genişliği bulunamadı!", "Hata",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            cancelButton.Click += (s, ev) => batchDialog.Close();

            batchDialog.Controls.AddRange(new Control[] {
                instructionLabel, batchTextBox, quickFillLabel, countTextBox,
                minBandTextBox, maxBandTextBox, randomFillButton,
                executeButton, cancelButton
            });

            // Açıklama etiketleri
            var countLabel = new Label { Text = "Adet", Location = new Point(115, 290), Size = new Size(50, 20), Font = new Font("Arial", 7), Visible = false };
            var minLabel = new Label { Text = "Min MHz", Location = new Point(170, 290), Size = new Size(50, 20), Font = new Font("Arial", 7), Visible = false };
            var maxLabel = new Label { Text = "Max MHz", Location = new Point(225, 290), Size = new Size(50, 20), Font = new Font("Arial", 7), Visible = false };

            batchDialog.Controls.AddRange(new Control[] { countLabel, minLabel, maxLabel });

            batchDialog.ShowDialog();
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
                MessageBox.Show("Geçersiz band genişliği!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            $"Aralık: [{allocation.StartFreq:F3} - {allocation.EndFreq:F3}] GHz";

                allocationListBox.Items.Add(info);

                //MessageBox.Show($"Tahsis başarılı!\nMerkez Frekansı: {allocation.CenterFrequency:F3} GHz\n" +
                //              $"Band Genişliği: {allocation.Bandwidth * 1000:F1} MHz",
                //              "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Uygun frekans bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                //MessageBox.Show("Tahsis kaldırıldı!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateUtilizationLabel()
        {
            double utilization = allocator.GetSpectrumUtilization();
            utilizationLabel.Text = $"Spektrum Kullanımı: {utilization:F1}%";
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

            // Arka plan ızgarası
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

            // Tahsis edilmiş blokları çiz
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

                // Merkez frekansı göster
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

            // Başlık ve eksen etiketleri
            g.DrawString("Frekans Spektrumu (GHz)", new Font("Arial", 11, FontStyle.Bold),
                        Brushes.Black, width / 2 - 50, -2);

            // Çerçeve
            using (var framePen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(framePen, 20, 20, width, height);
            }
        }
    }
}

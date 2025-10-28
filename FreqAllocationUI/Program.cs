namespace FreqAllocationUI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Frekans aralýðýný kullanýcýdan al
            double minFreq = 2; // GHz
            double maxFreq = 18; // GHz

            var inputForm = new Form
            {
                Text = "Frekans Aralýðý Ayarlarý",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterScreen
            };

            var minLabel = new Label { Text = "Min Frekans (GHz):", Location = new Point(20, 20), Size = new Size(120, 25) };
            var minTextBox = new TextBox { Text = "2", Location = new Point(150, 20), Size = new Size(100, 25) };

            var maxLabel = new Label { Text = "Max Frekans (GHz):", Location = new Point(20, 60), Size = new Size(120, 25) };
            var maxTextBox = new TextBox { Text = "18", Location = new Point(150, 60), Size = new Size(100, 25) };

            var startButton = new Button
            {
                Text = "Baþlat",
                Location = new Point(150, 100),
                Size = new Size(100, 30),
                DialogResult = DialogResult.OK
            };

            inputForm.Controls.AddRange(new Control[] { minLabel, minTextBox, maxLabel, maxTextBox, startButton });

            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                if (double.TryParse(minTextBox.Text, out minFreq) &&
                    double.TryParse(maxTextBox.Text, out maxFreq) &&
                    minFreq < maxFreq)
                {
                    Application.Run(new Form1(minFreq, maxFreq));
                }
                else
                {
                    MessageBox.Show("Geçersiz frekans aralýðý!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
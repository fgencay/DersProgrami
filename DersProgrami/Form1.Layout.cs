using System;
using System.Drawing;
using System.Windows.Forms;

namespace DersProgrami
{
    partial class Form1
    {
        private TextBox txtDersAd, txtOgretmen, txtGrup;
        private ComboBox cmbSinif, cmbFiltre;
        private NumericUpDown nudSure;
        private Button btnBaslat, btnDersEkle, btnListeTemizle, btnIptal, btnKaydet, btnAc, btnCsv;
        private ListBox lstDersler;
        private Label lblFitness;
        private DataGridView dgvProgram;
        private ProgressBar ilerlemeCubugu;
        private TableLayoutPanel girisPaneli, listePaneli;

        private static Button Dugme(string metin, EventHandler olay)
        {
            var b = new Button { Text = metin, AutoSize = true, MinimumSize = new Size(95, 34), Margin = new Padding(4),
                FlatStyle = FlatStyle.System, Padding = new Padding(8, 3, 8, 3) };
            b.Click += olay;
            return b;
        }

        private void ArayuzuHazirla()
        {

            SuspendLayout();
            Text = "Ders Programı • Haftalık Planlayıcı";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(1250, 800);
            MinimumSize = new Size(1000, 780);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 247, 250);

            var ana = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 5 };
            ana.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            ana.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ana.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            ana.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            ana.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            ana.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            ana.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            var baslik = new Label { Text = "Haftalık ders programı", AutoSize = true, Font = new Font("Segoe UI", 20F, FontStyle.Bold) };
            ana.Controls.Add(baslik, 0, 0); ana.SetColumnSpan(baslik, 2);

            var dosyalar = new FlowLayoutPanel { Dock = DockStyle.Fill };
            btnAc = Dugme("Dersleri aç", Ac);
            btnKaydet = Dugme("Dersleri kaydet", (s, e) => Kaydet());
            btnCsv = Dugme("Programı CSV aktar", CsvAktar); btnCsv.Enabled = false;
            dosyalar.Controls.AddRange(new Control[] { btnAc, btnKaydet, btnCsv });
            ana.Controls.Add(dosyalar, 0, 1); ana.SetColumnSpan(dosyalar, 2);

            var sol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            sol.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
            sol.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            girisPaneli = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(0, 0, 12, 0) };
            girisPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            girisPaneli.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 8; i++) girisPaneli.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            txtDersAd = new TextBox { Dock = DockStyle.Fill, MaxLength = 150 };
            txtOgretmen = new TextBox { Dock = DockStyle.Fill, MaxLength = 150 };
            txtGrup = new TextBox { Dock = DockStyle.Fill, MaxLength = 100 };
            cmbSinif = new ComboBox { Dock = DockStyle.Fill, MaxLength = 100 };
            nudSure = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 8, Value = 1 };
            string[] etiketler = { "Ders adı", "Öğretmen", "Grup", "Sınıf / oda", "Süre (saat)" };
            Control[] alanlar = { txtDersAd, txtOgretmen, txtGrup, cmbSinif, nudSure };
            for (int i = 0; i < alanlar.Length; i++)
            {
                girisPaneli.Controls.Add(new Label { Text = etiketler[i], AutoSize = true, Anchor = AnchorStyles.Left }, 0, i);
                girisPaneli.Controls.Add(alanlar[i], 1, i);
            }
            btnDersEkle = Dugme("Ders ekle", btnDersEkle_Click); btnDersEkle.Dock = DockStyle.Fill;
            girisPaneli.Controls.Add(btnDersEkle, 0, 5); girisPaneli.SetColumnSpan(btnDersEkle, 2);
            var vazgec = Dugme("Girişi temizle", (s, e) => GirisiTemizle()); vazgec.Dock = DockStyle.Fill;
            girisPaneli.Controls.Add(vazgec, 0, 6); girisPaneli.SetColumnSpan(vazgec, 2);
            var ipucu = new Label { Text = "Süre, aralıksız ders saatini belirtir.", AutoSize = true, ForeColor = Color.DimGray };
            girisPaneli.Controls.Add(ipucu, 0, 7); girisPaneli.SetColumnSpan(ipucu, 2);
            sol.Controls.Add(girisPaneli, 0, 0);

            listePaneli = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(0, 0, 10, 0) };
            listePaneli.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            listePaneli.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listePaneli.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
            listePaneli.Controls.Add(new Label { Text = "Dersler • Düzenlemek için çift tıklayın", AutoSize = true }, 0, 0);
            lstDersler = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
            lstDersler.DoubleClick += Duzenle;
            listePaneli.Controls.Add(lstDersler, 0, 1);
            var listeDugmeleri = new FlowLayoutPanel { Dock = DockStyle.Fill };
            btnListeTemizle = Dugme("Tümünü temizle", btnListeTemizle_Click);
            listeDugmeleri.Controls.AddRange(new Control[] { Dugme("Düzenle", Duzenle), Dugme("Seçileni sil", Sil), btnListeTemizle });
            listePaneli.Controls.Add(listeDugmeleri, 0, 2);
            sol.Controls.Add(listePaneli, 0, 1);
            ana.Controls.Add(sol, 0, 2);

            var sag = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            sag.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            sag.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var filtreSatiri = new FlowLayoutPanel { Dock = DockStyle.Fill };
            cmbFiltre = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltre.SelectedIndexChanged += (s, e) => TabloyuGoster();
            filtreSatiri.Controls.Add(new Label { Text = "Görünüm", AutoSize = true, Margin = new Padding(3, 7, 8, 0) });
            filtreSatiri.Controls.Add(cmbFiltre);
            sag.Controls.Add(filtreSatiri, 0, 0);
            dgvProgram = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, AllowUserToOrderColumns = false, BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersWidth = 135,
                BorderStyle = BorderStyle.None, SelectionMode = DataGridViewSelectionMode.CellSelect,
                EnableHeadersVisualStyles = false, ColumnHeadersHeight = 36 };
            dgvProgram.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvProgram.DefaultCellStyle.Padding = new Padding(5);
            dgvProgram.RowHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvProgram.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 61, 91);
            dgvProgram.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvProgram.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 61, 91);
            sag.Controls.Add(dgvProgram, 0, 1);
            ana.Controls.Add(sag, 1, 2);

            var islemler = new FlowLayoutPanel { Dock = DockStyle.Fill };
            btnBaslat = Dugme("Program oluştur", btnBaslat_Click);
            btnIptal = Dugme("İptal", (s, e) => iptal?.Cancel()); btnIptal.Enabled = false;
            ilerlemeCubugu = new ProgressBar { Width = 180, Height = 25, Maximum = 250, Visible = false, Margin = new Padding(12, 10, 0, 0) };
            islemler.Controls.AddRange(new Control[] { btnBaslat, btnIptal, ilerlemeCubugu });
            ana.Controls.Add(islemler, 0, 3); ana.SetColumnSpan(islemler, 2);
            lblFitness = new Label { Text = "Başlamak için ders ekleyin veya kayıtlı dersleri açın.", Dock = DockStyle.Fill,
                AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(35, 61, 91) };
            ana.Controls.Add(lblFitness, 0, 4); ana.SetColumnSpan(lblFitness, 2);
            Controls.Add(ana);
            Load += Form1_Load;
            FormClosing += Kapanirken;
            ResumeLayout(false);
        }
    }
}

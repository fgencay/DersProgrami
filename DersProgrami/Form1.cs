using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DersProgrami
{
    public partial class Form1 : Form
    {
        private List<Ders> dersler = new List<Ders>();
        private Kromozom sonProgram;
        private CancellationTokenSource iptal;
        private bool degisti;
        private bool kapat;
        private int duzenlenen = -1;

        public Form1() { InitializeComponent(); ArayuzuHazirla(); }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbSinif.Items.AddRange(new object[] { "101", "102" });
            cmbSinif.SelectedIndex = 0;
            cmbFiltre.Items.Add("Tüm sınıflar");
            cmbFiltre.SelectedIndex = 0;
            TabloyuGoster();
        }

        private void DegisiklikSonrasi()
        {
            degisti = true;
            sonProgram = null;
            btnCsv.Enabled = false;
            lstDersler.Items.Clear();
            foreach (var d in dersler)
                lstDersler.Items.Add($"{d.Ad} | {d.Ogretmen} | {d.Grup} | {d.Sinif} | {d.Sure} sa.");
            lblFitness.Text = $"{dersler.Count} ders • {dersler.Sum(d => d.Sure)} saat. Program oluşturabilirsiniz.";
            lblFitness.ForeColor = Color.FromArgb(35, 61, 91);
            cmbFiltre.Items.Clear();
            cmbFiltre.Items.Add("Tüm sınıflar");
            foreach (var sinif in dersler.Select(d => d.Sinif).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
                cmbFiltre.Items.Add(sinif);
            cmbFiltre.SelectedIndex = 0;
            TabloyuGoster();
        }

        private void btnDersEkle_Click(object sender, EventArgs e)
        {
            var ders = new Ders { Ad = txtDersAd.Text.Trim(), Ogretmen = txtOgretmen.Text.Trim(),
                Grup = txtGrup.Text.Trim(), Sinif = cmbSinif.Text.Trim(), Sure = (int)nudSure.Value };
            try
            {
                var aday = new List<Ders>(dersler);
                if (duzenlenen >= 0) aday[duzenlenen] = ders; else aday.Add(ders);
                Planlayici.Dogrula(aday);
                dersler = aday;
                if (!cmbSinif.Items.Contains(ders.Sinif)) cmbSinif.Items.Add(ders.Sinif);
                DegisiklikSonrasi();
                GirisiTemizle();
                txtDersAd.Focus();
            }
            catch (ArgumentException ex) { Bilgi(ex.Message); }
        }

        private void GirisiTemizle()
        {
            duzenlenen = -1;
            txtDersAd.Clear(); txtOgretmen.Clear(); txtGrup.Clear(); nudSure.Value = 1;
            btnDersEkle.Text = "Ders ekle";
            lstDersler.ClearSelected();
        }

        private void Duzenle(object sender, EventArgs e)
        {
            if (lstDersler.SelectedIndex < 0) return;
            duzenlenen = lstDersler.SelectedIndex;
            var d = dersler[duzenlenen];
            txtDersAd.Text = d.Ad; txtOgretmen.Text = d.Ogretmen; txtGrup.Text = d.Grup;
            cmbSinif.Text = d.Sinif; nudSure.Value = d.Sure;
            btnDersEkle.Text = "Değişiklikleri uygula";
        }

        private void Sil(object sender, EventArgs e)
        {
            if (lstDersler.SelectedIndex < 0) { Bilgi("Silmek için listeden bir ders seçin."); return; }
            dersler.RemoveAt(lstDersler.SelectedIndex);
            GirisiTemizle(); DegisiklikSonrasi();
        }

        private void btnListeTemizle_Click(object sender, EventArgs e)
        {
            if (dersler.Count == 0) return;
            if (MessageBox.Show(this, "Tüm dersler listeden silinsin mi?", "Listeyi temizle", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;
            dersler.Clear(); GirisiTemizle(); DegisiklikSonrasi();
        }

        private async void btnBaslat_Click(object sender, EventArgs e)
        {
            if (iptal != null) return;
            try { Planlayici.Dogrula(dersler); }
            catch (ArgumentException ex) { Bilgi(ex.Message); return; }
            var kaynak = new CancellationTokenSource();
            iptal = kaynak;
            Mesgul(true);
            lblFitness.Text = "Program hazırlanıyor…";
            var ilerleme = new Progress<int>(n => { if (iptal == kaynak) ilerlemeCubugu.Value = Math.Min(n, 250); });
            try
            {
                var sonuc = await Task.Run(() => new Planlayici().Uret(dersler, kaynak.Token, ilerleme));
                if (kaynak.IsCancellationRequested) throw new OperationCanceledException();
                sonProgram = sonuc.Program;
                TabloyuGoster();
                lblFitness.Text = sonProgram.CakismaSayisi == 0
                    ? $"Çakışmasız program • Boşluk: {sonProgram.BoslukCezasi} saat • Nesil: {sonuc.Jenerasyon}"
                    : $"Uygun program bulunamadı: {sonProgram.CakismaSayisi} çakışma. Dersleri gözden geçirin veya yeniden deneyin.";
                lblFitness.ForeColor = sonProgram.CakismaSayisi == 0 ? Color.DarkGreen : Color.Firebrick;
            }
            catch (OperationCanceledException) { lblFitness.Text = "İşlem iptal edildi. Önceki sonuç korundu."; }
            catch (Exception ex) { Bilgi("Program oluşturulamadı: " + ex.Message); lblFitness.Text = "Program oluşturulamadı."; }
            finally
            {
                iptal = null; kaynak.Dispose(); Mesgul(false);
                if (kapat) Close();
            }
        }

        private void Mesgul(bool durum)
        {
            girisPaneli.Enabled = !durum;
            listePaneli.Enabled = !durum;
            btnBaslat.Enabled = !durum;
            btnIptal.Enabled = durum;
            btnKaydet.Enabled = btnAc.Enabled = !durum;
            btnCsv.Enabled = !durum && sonProgram != null && sonProgram.CakismaSayisi == 0;
            ilerlemeCubugu.Visible = durum;
            ilerlemeCubugu.Value = 0;
            UseWaitCursor = durum;
        }

        private void TabloyuGoster()
        {
            dgvProgram.Columns.Clear(); dgvProgram.Rows.Clear();
            foreach (var gun in Planlayici.Gunler)
            {
                int sutun = dgvProgram.Columns.Add(gun, gun);
                dgvProgram.Columns[sutun].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            for (int saat = Planlayici.Baslangic; saat < Planlayici.Bitis; saat++)
            {
                int index = dgvProgram.Rows.Add();
                var row = dgvProgram.Rows[index];
                row.HeaderCell.Value = $"{saat:00}:00–{saat + 1:00}:00";
                for (int j = 0; j < Planlayici.Gunler.Length; j++)
                {
                    var atamalar = sonProgram == null ? new List<DersAtamasi>() : sonProgram.Atamalar.Where(a =>
                        a.Slot.Gun == Planlayici.Gunler[j] && a.Slot.Saat <= saat && saat < a.Slot.Saat + a.Ders.Sure &&
                        (cmbFiltre.SelectedIndex <= 0 || Planlayici.Ayni(a.Sinif, cmbFiltre.Text))).ToList();
                    row.Cells[j].Value = atamalar.Count == 0 ? "—" : string.Join("\n\n", atamalar.Select(a =>
                        $"{a.Ders.Ad} · {a.Sinif}\n{a.Ders.Ogretmen} · {a.Ders.Grup}\n{a.Slot.Saat:00}:00–{a.Slot.Saat + a.Ders.Sure:00}:00"));
                    bool cakisma = atamalar.Any(a => sonProgram.Atamalar.Any(b => !ReferenceEquals(a, b) && Planlayici.Cakisiyor(a, b)));
                    row.Cells[j].Style.BackColor = cakisma ? Color.MistyRose : atamalar.Count > 0 ? Color.FromArgb(229, 242, 252) : Color.White;
                }
            }
            dgvProgram.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        private bool Kaydet()
        {
            using (var dialog = new SaveFileDialog { Filter = "Ders dosyası (*.xml)|*.xml", FileName = "dersler.xml", DefaultExt = "xml" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                try { DosyaIslemleri.Kaydet(dialog.FileName, dersler); degisti = false; return true; }
                catch (Exception ex) { Bilgi("Dosya kaydedilemedi: " + ex.Message); return false; }
            }
        }

        private bool DegisiklikleriKoru()
        {
            if (!degisti) return true;
            var cevap = MessageBox.Show(this, "Ders listesindeki değişiklikler kaydedilsin mi?", "Kaydedilmemiş değişiklikler",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return cevap == DialogResult.No || (cevap == DialogResult.Yes && Kaydet());
        }

        private void Ac(object sender, EventArgs e)
        {
            if (!DegisiklikleriKoru()) return;
            using (var dialog = new OpenFileDialog { Filter = "Ders dosyası (*.xml)|*.xml" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var yuklenen = DosyaIslemleri.Yukle(dialog.FileName);
                    dersler = yuklenen; GirisiTemizle(); DegisiklikSonrasi(); degisti = false;
                }
                catch (Exception ex) { Bilgi("Dosya açılamadı. Mevcut dersler korundu.\n" + ex.Message); }
            }
        }

        private void CsvAktar(object sender, EventArgs e)
        {
            if (sonProgram == null || sonProgram.CakismaSayisi != 0) return;
            using (var dialog = new SaveFileDialog { Filter = "CSV dosyası (*.csv)|*.csv", FileName = "ders-programi.csv", DefaultExt = "csv" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try { DosyaIslemleri.CsvKaydet(dialog.FileName, sonProgram); lblFitness.Text = "Program CSV olarak kaydedildi."; }
                catch (Exception ex) { Bilgi("CSV kaydedilemedi: " + ex.Message); }
            }
        }

        private void Kapanirken(object sender, FormClosingEventArgs e)
        {
            if (iptal != null) { e.Cancel = true; kapat = true; iptal.Cancel(); return; }
            if (!DegisiklikleriKoru()) { e.Cancel = true; kapat = false; }
        }

        private void Bilgi(string mesaj) { MessageBox.Show(this, mesaj, "Ders Programı", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }
}

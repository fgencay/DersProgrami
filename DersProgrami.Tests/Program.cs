using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using DersProgrami;

internal static class Tests
{
    private static int adet;
    private sealed class Ilerleme : IProgress<int>
    {
        private readonly Action<int> eylem;
        public Ilerleme(Action<int> eylem) { this.eylem = eylem; }
        public void Report(int n) { eylem(n); }
    }
    private static void Kontrol(bool dogru, string mesaj) { if (!dogru) throw new Exception(mesaj); }
    private static void Test(string ad, Action test) { test(); adet++; Console.WriteLine("PASS " + ad); }
    private static Ders Ders(string ad = "Matematik", int sure = 1, string ogretmen = "Ali", string grup = "A", string sinif = "101")
    { return new Ders { Ad = ad, Sure = sure, Ogretmen = ogretmen, Grup = grup, Sinif = sinif }; }
    private static DersAtamasi Ata(Ders d, int saat, string gun = "Pazartesi")
    { return new DersAtamasi { Ders = d, Sinif = d.Sinif, Slot = new ZamanSlotu { Gun = gun, Saat = saat } }; }
    private static Kromozom Plan(params DersAtamasi[] a) { return new Kromozom { Atamalar = a.ToList() }; }
    private static void Hata<T>(Action islem) where T : Exception
    { try { islem(); } catch (T) { return; } throw new Exception("Beklenen hata oluşmadı: " + typeof(T).Name); }
    private static T Alan<T>(Form1 f, string ad) { return (T)typeof(Form1).GetField(ad, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f); }
    private static void Cagir(Form1 f, string ad) { typeof(Form1).GetMethod(ad, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { null, EventArgs.Empty }); }

    [STAThread]
    private static int Main()
    {
        try
        {
            Test("Süre boyunca çakışma ve bitiş sınırı", () => {
                var a = Ata(Ders(sure: 3), 9);
                Kontrol(Planlayici.Cakisiyor(a, Ata(Ders("Fizik"), 11)), "Çok saatli ders çakışması kaçırıldı");
                Kontrol(!Planlayici.Cakisiyor(a, Ata(Ders("Fizik"), 12)), "Bitiş saati çakışma sayıldı");
                var k = Plan(a, Ata(Ders("Fizik"), 11)); Planlayici.Degerlendir(k);
                Kontrol(k.CakismaSayisi == 1, "Aynı ders çifti birden fazla sayıldı");
            });
            Test("Kaynak ve gün ayrımı", () => {
                var a = Ata(Ders(), 9);
                Kontrol(!Planlayici.Cakisiyor(a, Ata(Ders(), 9, "Salı")), "Günler ayrılmadı");
                Kontrol(!Planlayici.Cakisiyor(a, Ata(Ders("Fizik", 1, "Veli", "B", "102"), 9)), "Bağımsız kaynaklar çakışıyor");
                Kontrol(Planlayici.Cakisiyor(a, Ata(Ders("Fizik", 1, " ali ", "B", "102"), 9)), "Öğretmen adı normalleşmedi");
                Kontrol(Planlayici.Cakisiyor(a, Ata(Ders("Fizik", 1, "Veli", "A", "102"), 9)), "Grup çakışması kaçırıldı");
                Kontrol(Planlayici.Cakisiyor(a, Ata(Ders("Fizik", 1, "Veli", "B", "101"), 9)), "Sınıf çakışması kaçırıldı");
            });
            Test("Boşluk hesabı gün ve süreye göre", () => {
                var k = Plan(Ata(Ders(sure: 2), 9), Ata(Ders("Fizik"), 12)); Planlayici.Degerlendir(k);
                Kontrol(k.BoslukCezasi == 2, "Bir saat boşluk öğretmen ve grup için sayılmalı");
                k.Atamalar[1].Slot.Gun = "Salı"; Planlayici.Degerlendir(k);
                Kontrol(k.BoslukCezasi == 0, "Farklı günlerin saatleri karşılaştırılıyor");
            });
            Test("Çaprazlama ve mutasyon ebeveynleri korur", () => {
                var p = new Planlayici(42); var a = Plan(Ata(Ders(sure: 2), 9)); var b = Plan(Ata(Ders(sure: 2), 13));
                Planlayici.Degerlendir(a); Planlayici.Degerlendir(b);
                var cocuk = p.Caprazla(a, b); p.Mutasyon(cocuk, 1);
                cocuk.Atamalar[0].Ders.Ad = "Değişti";
                Kontrol(a.Atamalar[0].Slot.Saat == 9 && b.Atamalar[0].Slot.Saat == 13, "Ebeveyn slotu değişti");
                Kontrol(a.Atamalar[0].Ders.Ad == "Matematik" && b.Atamalar[0].Ders.Ad == "Matematik", "Ders nesnesi ortak");
                Kontrol(a.Fitness == 1000 && b.Fitness == 1000, "Ebeveyn puanı değişti");
            });
            Test("Mutasyon sınıfı ve gün sınırlarını korur", () => {
                var p = new Planlayici(12); var k = Plan(Ata(Ders(sure: 8, sinif: "Özel-oda"), 9));
                for (int i = 0; i < 100; i++) { p.Mutasyon(k, 1); Kontrol(k.Atamalar[0].Sinif == "Özel-oda" && k.Atamalar[0].Slot.Saat == 9, "Sınır/sınıf bozuldu"); }
            });
            Test("Geçersiz başlangıç ve sınıf ataması", () => {
                var a = Ata(Ders(sure: 3), 16); var k = Plan(a); Planlayici.Degerlendir(k);
                Kontrol(k.CakismaSayisi > 0, "Gün dışına taşma geçerli sayıldı");
                a.Slot.Saat = 9; a.Sinif = "102"; Planlayici.Degerlendir(k);
                Kontrol(k.CakismaSayisi > 0, "Sınıf değişikliği geçerli sayıldı");
            });
            Test("Boş ve geçersiz giriş reddedilir", () => {
                Hata<ArgumentException>(() => Planlayici.Dogrula(new List<Ders>()));
                Hata<ArgumentException>(() => Planlayici.Dogrula(new[] { Ders(sure: 0) }));
                Hata<ArgumentException>(() => Planlayici.Dogrula(new[] { Ders(sure: 9) }));
                Hata<ArgumentException>(() => Planlayici.Dogrula(new[] { Ders(ogretmen: " ") }));
                Hata<ArgumentException>(() => Planlayici.Dogrula(Enumerable.Range(0, 6).Select(i => Ders(sure: 8)).ToList()));
            });
            Test("İptal isteği uygulanır", () => {
                using (var c = new CancellationTokenSource()) { c.Cancel(); Hata<OperationCanceledException>(() => new Planlayici(1).Uret(new[] { Ders() }, c.Token)); }
            });
            Test("Tek ders ve çok saatli sonuç", () => {
                var sonuc = new Planlayici(1).Uret(new[] { Ders(sure: 8) }, CancellationToken.None);
                Kontrol(sonuc.Program.CakismaSayisi == 0 && sonuc.Program.Atamalar.Count == 1 && sonuc.Jenerasyon == 0, "Tek ders planlanamadı");
            });
            Test("Arama sırasında iptal", () => {
                using (var c = new CancellationTokenSource())
                {
                    var ds = Enumerable.Range(0, 6).Select(i => Ders("Uzun blok " + i, 6)).ToList();
                    Hata<OperationCanceledException>(() => new Planlayici(4).Uret(ds, c.Token, new Ilerleme(n => c.Cancel())));
                }
            });
            Test("Sığmayan bloklar geçerli program olarak sunulmaz", () => {
                var ds = Enumerable.Range(0, 6).Select(i => Ders("Uzun blok " + i, 6)).ToList();
                var sonuc = new Planlayici(4).Uret(ds, CancellationToken.None, nesilSiniri: 5);
                Kontrol(sonuc.Program.CakismaSayisi > 0 && sonuc.Jenerasyon == 5, "İmkansız program geçerli sayıldı");
            });
            Test("Yoğun programlar: 10 farklı rastgele tohum", () => {
                var dersler = Enumerable.Range(0, 20).Select(i => Ders("Ders " + i, 2, "Öğretmen" + i % 5, "Grup" + i % 4, "Oda" + i % 2)).ToList();
                for (int seed = 0; seed < 10; seed++)
                {
                    var k = new Planlayici(seed).Uret(dersler, CancellationToken.None, nesilSiniri: 50).Program;
                    Kontrol(k.CakismaSayisi == 0 && k.Atamalar.Count == dersler.Count, "Yoğun program geçersiz, tohum " + seed);
                    for (int i = 0; i < k.Atamalar.Count; i++)
                    {
                        var a = k.Atamalar[i];
                        Kontrol(a.Ders.Ad == dersler[i].Ad && a.Sinif == dersler[i].Sinif && a.Slot.Saat + a.Ders.Sure <= 17, "Ders/süre/sınıf kaybı");
                        for (int j = i + 1; j < k.Atamalar.Count; j++)
                        { var b = k.Atamalar[j]; if (a.Slot.Gun != b.Slot.Gun) continue;
                          bool ortak = a.Sinif == b.Sinif || a.Ders.Ogretmen == b.Ders.Ogretmen || a.Ders.Grup == b.Ders.Grup;
                          Kontrol(!ortak || a.Slot.Saat + a.Ders.Sure <= b.Slot.Saat || b.Slot.Saat + b.Ders.Sure <= a.Slot.Saat, "Bağımsız doğrulama: çakışma var"); }
                    }
                    int fitness = k.Fitness; Planlayici.Degerlendir(k); Kontrol(k.Fitness == fitness, "Puan güncel değil");
                }
            });
            Test("Tam haftalık kapasite", () => {
                var k = new Planlayici(9).Uret(Enumerable.Range(0, 5).Select(i => Ders("Blok" + i, 8)).ToList(), CancellationToken.None).Program;
                Kontrol(k.CakismaSayisi == 0 && k.Atamalar.Select(a => a.Slot.Gun).Distinct().Count() == 5, "40 saat yerleştirilemedi");
            });
            Test("XML kayıt, üzerine yazma, boş liste ve bozuk dosya", () => {
                string yol = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid() + ".xml");
                try {
                    DosyaIslemleri.Kaydet(yol, new List<Ders> { Ders("Türkçe & bilim", 3, "Öğretmen Çığ", "A", "Özel") });
                    var ds = DosyaIslemleri.Yukle(yol); Kontrol(ds[0].Ad == "Türkçe & bilim" && ds[0].Sure == 3, "XML veri kaybı");
                    DosyaIslemleri.Kaydet(yol, new List<Ders>()); Kontrol(DosyaIslemleri.Yukle(yol).Count == 0, "Boş liste kayıt hatası");
                    File.WriteAllText(yol, "bozuk dosya"); Hata<InvalidOperationException>(() => DosyaIslemleri.Yukle(yol));
                } finally { if (File.Exists(yol)) File.Delete(yol); }
            });
            Test("CSV Türkçe, kaçış, formül ve çakışmalı aktarım", () => {
                string yol = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid() + ".csv");
                try {
                    var k = Plan(Ata(Ders("=SUM(1;2)\"Türkçe", 3), 9)); DosyaIslemleri.CsvKaydet(yol, k);
                    string s = File.ReadAllText(yol); Kontrol(s.Contains("12:00") && s.Contains("'=SUM(1;2)\"\"Türkçe"), "CSV kaçış/süre hatası");
                    k.Atamalar.Add(Ata(Ders(), 10)); Hata<InvalidOperationException>(() => DosyaIslemleri.CsvKaydet(yol, k));
                } finally { if (File.Exists(yol)) File.Delete(yol); }
            });
            Test("Arayüz ekleme, düzenleme, silme ve program geçersizleştirme", () => {
                using (var f = new Form1()) {
                    Cagir(f, "Form1_Load");
                    Alan<TextBox>(f, "txtDersAd").Text = " Matematik "; Alan<TextBox>(f, "txtOgretmen").Text = " Ali ";
                    Alan<TextBox>(f, "txtGrup").Text = " A "; Alan<NumericUpDown>(f, "nudSure").Value = 3;
                    Cagir(f, "btnDersEkle_Click");
                    var ds = Alan<List<Ders>>(f, "dersler"); Kontrol(ds.Count == 1 && ds[0].Ad == "Matematik", "Ekleme hatası");
                    Alan<ListBox>(f, "lstDersler").SelectedIndex = 0; Cagir(f, "Duzenle");
                    Alan<TextBox>(f, "txtDersAd").Text = "Fizik"; Cagir(f, "btnDersEkle_Click");
                    Kontrol(Alan<List<Ders>>(f, "dersler").Single().Ad == "Fizik", "Düzenleme hatası");
                    var k = new Planlayici(1).Uret(Alan<List<Ders>>(f, "dersler"), CancellationToken.None).Program;
                    typeof(Form1).GetField("sonProgram", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(f, k);
                    typeof(Form1).GetMethod("TabloyuGoster", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, null);
                    var grid = Alan<DataGridView>(f, "dgvProgram"); int dolu = 0;
                    foreach (DataGridViewRow row in grid.Rows) foreach (DataGridViewCell cell in row.Cells) if ((string)cell.Value != "—") dolu++;
                    Kontrol(dolu == 3 && grid.Columns.Count == 5 && grid.Rows.Count == 8, "Çok saatli tablo hatası");
                    Alan<ListBox>(f, "lstDersler").SelectedIndex = 0; Cagir(f, "Sil");
                    Kontrol(Alan<List<Ders>>(f, "dersler").Count == 0 && Alan<Kromozom>(f, "sonProgram") == null, "Eski program temizlenmedi");
                }
            });
            Test("Arka plan üretimi sonucu arayüze aktarır", () => {
                using (var f = new Form1())
                {
                    Cagir(f, "Form1_Load");
                    Alan<List<Ders>>(f, "dersler").Add(Ders(sure: 3));
                    Cagir(f, "btnBaslat_Click");
                    var sinir = DateTime.UtcNow.AddSeconds(10);
                    while (Alan<CancellationTokenSource>(f, "iptal") != null && DateTime.UtcNow < sinir)
                    { Application.DoEvents(); Thread.Sleep(10); }
                    Kontrol(Alan<CancellationTokenSource>(f, "iptal") == null, "Arka plan işi tamamlanmadı");
                    Kontrol(Alan<Kromozom>(f, "sonProgram") != null && Alan<Button>(f, "btnCsv").Enabled && Alan<Button>(f, "btnBaslat").Enabled,
                        "Arka plan sonucu veya düğme durumları hatalı");
                }
            });
            Console.WriteLine(adet + " test grubu başarılı."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

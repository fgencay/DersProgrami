using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DersProgrami
{
    public sealed class PlanSonucu
    {
        public Kromozom Program { get; set; }
        public int Jenerasyon { get; set; }
    }

    public sealed class Planlayici
    {
        public static readonly string[] Gunler = { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };
        public const int Baslangic = 9;
        public const int Bitis = 17;
        private readonly Random rnd;

        public Planlayici(int? tohum = null) { rnd = tohum.HasValue ? new Random(tohum.Value) : new Random(); }

        public static bool Ayni(string a, string b)
        {
            return string.Equals(a == null ? null : a.Trim(), b == null ? null : b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static void Dogrula(IList<Ders> dersler)
        {
            if (dersler == null || dersler.Count == 0)
                throw new ArgumentException("En az bir ders ekleyin.");
            foreach (var d in dersler)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.Ad) || string.IsNullOrWhiteSpace(d.Ogretmen) ||
                    string.IsNullOrWhiteSpace(d.Grup) || string.IsNullOrWhiteSpace(d.Sinif))
                    throw new ArgumentException("Her dersin adı, öğretmeni, grubu ve sınıfı dolu olmalıdır.");
                if (d.Sure < 1 || d.Sure > Bitis - Baslangic)
                    throw new ArgumentException("Ders süresi 1–8 saat arasında olmalıdır.");
            }
            Kapasite(dersler, d => d.Ogretmen, "Öğretmen");
            Kapasite(dersler, d => d.Grup, "Grup");
            Kapasite(dersler, d => d.Sinif, "Sınıf");
        }

        private static void Kapasite(IEnumerable<Ders> dersler, Func<Ders, string> anahtar, string tur)
        {
            foreach (var grup in dersler.GroupBy(d => anahtar(d).Trim(), StringComparer.OrdinalIgnoreCase))
                if (grup.Sum(d => (long)d.Sure) > Gunler.Length * (Bitis - Baslangic))
                    throw new ArgumentException(tur + " '" + grup.Key + "' için haftalık 40 saat kapasitesi aşılıyor.");
        }

        public static bool Cakisiyor(DersAtamasi a, DersAtamasi b)
        {
            return a.Slot.Gun == b.Slot.Gun && a.Slot.Saat < b.Slot.Saat + b.Ders.Sure &&
                b.Slot.Saat < a.Slot.Saat + a.Ders.Sure &&
                (Ayni(a.Sinif, b.Sinif) || Ayni(a.Ders.Ogretmen, b.Ders.Ogretmen) || Ayni(a.Ders.Grup, b.Ders.Grup));
        }

        public static void Degerlendir(Kromozom k)
        {
            int cakisma = 0;
            for (int i = 0; i < k.Atamalar.Count; i++)
            {
                var a = k.Atamalar[i];
                if (!Gunler.Contains(a.Slot.Gun) || a.Slot.Saat < Baslangic ||
                    a.Ders.Sure < 1 || a.Slot.Saat + a.Ders.Sure > Bitis || !Ayni(a.Sinif, a.Ders.Sinif)) cakisma++;
                for (int j = i + 1; j < k.Atamalar.Count; j++)
                    if (Cakisiyor(a, k.Atamalar[j])) cakisma++;
            }
            // Boşluklar yalnızca aynı kişinin/grubun aynı günündeki dolu saatler arasında ölçülür.
            int bosluk = Bosluk(k.Atamalar, a => a.Ders.Ogretmen) + Bosluk(k.Atamalar, a => a.Ders.Grup);
            k.CakismaSayisi = cakisma;
            k.BoslukCezasi = bosluk;
            k.Fitness = 1000 - cakisma * 100 - bosluk;
        }

        private static int Bosluk(IEnumerable<DersAtamasi> atamalar, Func<DersAtamasi, string> anahtar)
        {
            int toplam = 0;
            foreach (var grup in atamalar.GroupBy(a => anahtar(a).Trim(), StringComparer.OrdinalIgnoreCase))
                foreach (var gun in grup.GroupBy(a => a.Slot.Gun))
                {
                    var saatler = new HashSet<int>(gun.SelectMany(a => Enumerable.Range(a.Slot.Saat, a.Ders.Sure)));
                    toplam += saatler.Max() - saatler.Min() + 1 - saatler.Count;
                }
            return toplam;
        }

        private static IOrderedEnumerable<Kromozom> Sirala(IEnumerable<Kromozom> p)
        {
            // Geçerlilik her zaman tercihlerden önce gelir; bonuslar çakışmaları gizleyemez.
            return p.OrderBy(k => k.CakismaSayisi).ThenBy(k => k.BoslukCezasi);
        }

        private ZamanSlotu RastgeleSlot(Ders ders)
        {
            return new ZamanSlotu { Gun = Gunler[rnd.Next(Gunler.Length)], Saat = rnd.Next(Baslangic, Bitis - ders.Sure + 1) };
        }

        private Kromozom IlkBirey(IList<Ders> dersler, CancellationToken token)
        {
            var k = new Kromozom();
            foreach (var d in dersler) k.Atamalar.Add(new DersAtamasi { Ders = d, Sinif = d.Sinif, Slot = RastgeleSlot(d) });
            Onar(k, token);
            Degerlendir(k);
            return k;
        }

        private void Onar(Kromozom k, CancellationToken token)
        {
            foreach (var i in Enumerable.Range(0, k.Atamalar.Count).OrderBy(x => rnd.Next()))
            {
                token.ThrowIfCancellationRequested();
                var a = k.Atamalar[i];
                int enAz = k.Atamalar.Where((b, j) => i != j && Cakisiyor(a, b)).Count();
                if (enAz == 0) continue;
                var enIyi = a.Slot;
                foreach (var gun in Gunler.OrderBy(x => rnd.Next()))
                    for (int saat = Baslangic; saat <= Bitis - a.Ders.Sure; saat++)
                    {
                        token.ThrowIfCancellationRequested();
                        a.Slot = new ZamanSlotu { Gun = gun, Saat = saat };
                        int ceza = k.Atamalar.Where((b, j) => i != j && Cakisiyor(a, b)).Count();
                        if (ceza < enAz) { enAz = ceza; enIyi = a.Slot; }
                    }
                a.Slot = enIyi;
            }
        }

        public Kromozom Caprazla(Kromozom a, Kromozom b)
        {
            if (a.Atamalar.Count != b.Atamalar.Count) throw new ArgumentException("Ebeveynlerin ders sayıları eşit olmalıdır.");
            var cocuk = a.Kopyala();
            int nokta = rnd.Next(a.Atamalar.Count + 1);
            var diger = b.Kopyala();
            for (int i = nokta; i < cocuk.Atamalar.Count; i++) cocuk.Atamalar[i] = diger.Atamalar[i];
            Degerlendir(cocuk);
            return cocuk;
        }

        public void Mutasyon(Kromozom k, double oran = 0.15)
        {
            foreach (var a in k.Atamalar)
                if (rnd.NextDouble() < oran) a.Slot = RastgeleSlot(a.Ders);
            Degerlendir(k);
        }

        private Kromozom Sec(List<Kromozom> p)
        {
            return Sirala(Enumerable.Range(0, 3).Select(i => p[rnd.Next(p.Count)])).First();
        }

        public PlanSonucu Uret(IList<Ders> dersler, CancellationToken token, IProgress<int> ilerleme = null, int nesilSiniri = 250)
        {
            Dogrula(dersler);
            token.ThrowIfCancellationRequested();
            var populasyon = new List<Kromozom>();
            for (int i = 0; i < 30; i++) { token.ThrowIfCancellationRequested(); populasyon.Add(IlkBirey(dersler, token)); }
            var enIyi = Sirala(populasyon).First().Kopyala();
            int nesil = 0, durgun = 0;
            while (nesil < nesilSiniri && (enIyi.CakismaSayisi != 0 || enIyi.BoslukCezasi != 0))
            {
                token.ThrowIfCancellationRequested();
                var yeni = new List<Kromozom> { enIyi.Kopyala() };
                while (yeni.Count < populasyon.Count)
                {
                    token.ThrowIfCancellationRequested();
                    var cocuk = Caprazla(Sec(populasyon), Sec(populasyon));
                    Mutasyon(cocuk);
                    Onar(cocuk, token);
                    Degerlendir(cocuk);
                    yeni.Add(cocuk);
                }
                populasyon = yeni;
                var aday = Sirala(populasyon).First();
                if (aday.CakismaSayisi < enIyi.CakismaSayisi ||
                    (aday.CakismaSayisi == enIyi.CakismaSayisi && aday.BoslukCezasi < enIyi.BoslukCezasi))
                { enIyi = aday.Kopyala(); durgun = 0; }
                else durgun++;
                nesil++;
                ilerleme?.Report(nesil);
                if (enIyi.CakismaSayisi == 0 && durgun >= 35) break;
            }
            token.ThrowIfCancellationRequested();
            Degerlendir(enIyi);
            return new PlanSonucu { Program = enIyi, Jenerasyon = nesil };
        }
    }
}

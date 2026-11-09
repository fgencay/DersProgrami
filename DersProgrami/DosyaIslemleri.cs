using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DersProgrami
{
    public static class DosyaIslemleri
    {
        public static void Kaydet(string yol, List<Ders> dersler)
        {
            if (dersler.Count > 0) Planlayici.Dogrula(dersler);
            string gecici = yol + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = File.Create(gecici)) new XmlSerializer(typeof(List<Ders>)).Serialize(stream, dersler);
                if (File.Exists(yol)) File.Replace(gecici, yol, null);
                else File.Move(gecici, yol);
            }
            finally { if (File.Exists(gecici)) File.Delete(gecici); }
        }

        public static List<Ders> Yukle(string yol)
        {
            var ayar = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2000000 };
            using (var reader = XmlReader.Create(yol, ayar))
            {
                var dersler = (List<Ders>)new XmlSerializer(typeof(List<Ders>)).Deserialize(reader);
                if (dersler == null) throw new InvalidDataException("Ders dosyası geçersiz.");
                if (dersler.Count > 0) Planlayici.Dogrula(dersler);
                foreach (var d in dersler)
                { d.Ad = d.Ad.Trim(); d.Ogretmen = d.Ogretmen.Trim(); d.Grup = d.Grup.Trim(); d.Sinif = d.Sinif.Trim(); }
                return dersler;
            }
        }

        private static string Hucre(string metin)
        {
            // Elektronik tablo yazılımlarında kullanıcı metninin formül olarak çalışmasını önler.
            if (!string.IsNullOrEmpty(metin) && "=+-@\t\r\n".Contains(metin[0])) metin = "'" + metin;
            return "\"" + (metin ?? "").Replace("\"", "\"\"") + "\"";
        }

        public static void CsvKaydet(string yol, Kromozom program)
        {
            Planlayici.Degerlendir(program);
            if (program.CakismaSayisi != 0) throw new InvalidOperationException("Çakışmalı program dışa aktarılamaz.");
            var satirlar = new List<string> { "Gün;Başlangıç;Bitiş;Ders;Öğretmen;Grup;Sınıf;Süre" };
            satirlar.AddRange(program.Atamalar.OrderBy(a => Array.IndexOf(Planlayici.Gunler, a.Slot.Gun)).ThenBy(a => a.Slot.Saat)
                .Select(a => string.Join(";", new[] { a.Slot.Gun, a.Slot.Saat.ToString("00") + ":00",
                    (a.Slot.Saat + a.Ders.Sure).ToString("00") + ":00", a.Ders.Ad, a.Ders.Ogretmen,
                    a.Ders.Grup, a.Sinif, a.Ders.Sure.ToString() }.Select(Hucre))));
            File.WriteAllLines(yol, satirlar, new UTF8Encoding(true));
        }
    }
}

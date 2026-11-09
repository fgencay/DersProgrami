# Ders Programı

.NET Framework 4.8 üzerinde çalışan Windows Forms haftalık ders planlayıcısı.
Visual Studio 2022 ve .NET Framework 4.8 geliştirme araçları ile `DersProgrami.sln` açılıp derlenebilir.
Başlangıç projesi `DersProgrami` olmalıdır. Harici NuGet paketi gerekmez.

## Kullanım

1. Ders adı, öğretmen, grup, sınıf/oda ve 1–8 saatlik süreyi girip **Ders ekle** düğmesine basın.
   Sınıf/oda alanına yeni bir ad da yazılabilir.
2. Listeden ders seçerek **Düzenle** veya **Seçileni sil** düğmelerini kullanın.
   Düzenlemeyi bitirmek için **Değişiklikleri uygula**, vazgeçmek için **Girişi temizle** düğmesine basın.
3. **Program oluştur** ile çalıştırın. Hesaplama sırasında **İptal** kullanılabilir.
4. **Görünüm** listesinden belirli bir sınıfın programını inceleyin.
5. **Dersleri kaydet / Dersleri aç** ile ders listesini XML dosyasında saklayın.
   Kapanışta veya başka dosya açarken kaydedilmemiş liste değişiklikleri sorulur.
6. Çakışmasız programı **Programı CSV aktar** ile dışa aktarın. CSV, Türkçe karakterler için
   UTF-8 BOM ve sütun ayırıcı olarak noktalı virgül kullanır; tüm sınıfları içerir.

XML dosyası ders listesini saklar. Üretilen programı saklamak için CSV aktarımını kullanın;
XML açıldıktan sonra program yeniden oluşturulur. Liste değiştirildiğinde eski program temizlenir.

## Planlama kuralları

- Pazartesi–Cuma, 09.00–17.00 arasında sekiz saatlik günlük kapasite vardır.
- Her kayıt haftada bir kez, belirtilen süre kadar kesintisiz ders bloğudur; süre haftaya bölünmez.
- Sınıf/oda seçimi sabittir. Aynı sınıf, öğretmen veya öğrenci grubu aynı anda iki derse atanamaz.
- Kaynak adlarının başındaki/sonundaki boşluklar ve harf büyüklüğü karşılaştırmada göz ardı edilir.
- Öğretmen ve grupların aynı gün içindeki boş saatleri azaltılır. Çakışma sayısı her zaman
  boşluk tercihinden önce değerlendirilir. Uygunluk puanı tek başına geçerlilik ölçütü değildir.
- Genetik arama 30 birey ve en fazla 250 nesil kullanır. Çakışmasız sonuç iyileşmiyorsa 35 nesil sonra durur.
  Arama olasılıksaldır; her mümkün veri kümesi için çözüm veya en iyi sonuç garantisi yoktur.
  Çözüm bulunamazsa sonuç uyarıyla gösterilir, çakışan dersler kırmızı işaretlenir ve CSV aktarımı kapatılır.
- Bir kaynak için 40 saatin üzerindeki yük girişte reddedilir. Bu kontrol tek başına
  yerleştirilebilirlik garantisi değildir; örneğin altı adet 6 saatlik blok beş güne sığmaz.

## Kod yapısı

- `Planlayici.cs`: giriş doğrulama, süre çakışmaları, günlük boşluk hesabı, genetik arama ve onarım.
- `Kromozom.cs`: program durumu ve bağımsız derin kopyalama.
- `DosyaIslemleri.cs`: XML okuma, geçici dosya ile güvenli kayıt ve CSV aktarımı.
- `Form1.cs`: kullanıcı işlemleri, arka planda planlama, iptal ve tablo görünümü.
- `Form1.Layout.cs`: çalışma anında oluşturulan, boyutlanabilir panel tabanlı arayüz.
  Arayüz yerleşimini değiştirmek için bu dosyayı düzenleyin. `Form1.Designer.cs` yalnızca temel form iskeletidir;
  çalışma anında eklenen kontroller Visual Studio tasarım yüzeyinde gösterilmez.
- `DersProgrami.Tests`: harici test paketi gerektirmeyen doğrulama uygulaması; hata durumunda çıkış kodu 1.

## Testleri çalıştırma

Visual Studio Developer PowerShell içinde:

```powershell
msbuild DersProgrami.sln /t:Build /p:Configuration=Debug
& .\DersProgrami.Tests\bin\Debug\DersProgrami.Tests.exe
```

Testler süre/çakışma sınırları, kaynak eşitliği, günlük boşluklar, kopya izolasyonu, sınıf korunması,
giriş kapasitesi, iptal, yoğun programlar, XML/CSV dosyaları ve temel WinForms iş akışlarını kapsar.

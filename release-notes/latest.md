## CashTracker 1.2.1

Bu surum, `1.2.0` yayin kanalini duzeltir ve musterilere gonderebileceginiz tek dosyalik updater exe'sini ekler.

- `InstallerLaunchService` repoya dahil edildi; bu sayede GitHub Actions release build'i artik eksik dosya yuzunden dusmez.
- `CashTracker-Fabesco-Updater.exe` eklendi. Musteri bu tek exe'yi calistirdiginda GitHub `latest` release'den guncel kurulumu indirip sessizce yukleyebilir.
- Updater, update manifest'i veya GitHub release asset'lerini okuyup kurulumu SHA-256 ile dogrular.
- Sessiz kurulumda masaustune `Cashtracker Fabesco` kisayolu garanti edilir.
- Telegram artik zorunlu degil; uygulama local-first calisiyor.
- Lisans/trial altyapisi eklendi ve ayarlardan yonetilebilir hale getirildi.
- Ayrica satici tarafi icin ayri lisans yonetim araci eklendi.
- Dashboard acilisi ve veri yukleme akislari hizlandirildi.
- Guncelleme denetimi imzali manifest ve GitHub fallback mantigiyla guclendirildi.
- Turkce karakter, font, DPI ve WinForms yerlesim sorunlari buyuk olcude duzeltildi.
- PIN ekranlari, ayarlar ve ana ekran yerlesimleri daha tutarli hale getirildi.
- Yedek, onbellek, test ve lokal demo senaryolari icin yeni arac scriptleri eklendi.

Kurulumdan sonra masaustunde uygulama kisayolu `Cashtracker Fabesco` olarak olusturulur.

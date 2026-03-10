## CashTracker 1.2.2

Bu surum, yayin kanalini calisir hale getirir ve musterilere gonderebileceginiz tek dosyalik updater exe'sini resmi release asset'i olarak ekler.

- `Write Update Manifest` adimi secret yoksa release'i artik dusurmez; uygulama ve updater GitHub release fallback ile calismaya devam eder.
- `CI` workflow'u tag push'larda calismaz; gereksiz fail mailleri azalir.
- `InstallerLaunchService` repoya dahil edildi; bu sayede GitHub Actions release build'i artik eksik dosya yuzunden dusmez.
- `CashTracker-Fabesco-Updater.exe` resmi release asset'i olarak eklendi. Musteri bu tek exe'yi calistirdiginda GitHub `latest` release'den guncel kurulumu indirip sessizce yukleyebilir.
- Updater, GitHub release asset'lerini okuyup kurulumu SHA-256 ile dogrular.
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

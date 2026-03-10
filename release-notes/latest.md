## CashTracker 1.2.2

Bu surum, yayin kanalini sadeleştirir ve ana uygulama release akisini duzeltmeye odaklanir.

- `Write Update Manifest` adimi secret yoksa release'i artik dusurmez.
- `CI` workflow'u tag push'larda calismaz; gereksiz fail mailleri azalir.
- `InstallerLaunchService` repoya dahil edildi; bu sayede uygulama ici installer baslatma akisi eksik dosya yuzunden bozulmaz.
- Telegram artik zorunlu degil; uygulama local-first calisiyor.
- Lisans/trial altyapisi eklendi ve ayarlardan yonetilebilir hale getirildi.
- Ayrica satici tarafi icin ayri lisans yonetim araci eklendi.
- Dashboard acilisi ve veri yukleme akislari hizlandirildi.
- Guncelleme denetimi imzali manifest ve GitHub fallback mantigiyla guclendirildi.
- Turkce karakter, font, DPI ve WinForms yerlesim sorunlari buyuk olcude duzeltildi.
- PIN ekranlari, ayarlar ve ana ekran yerlesimleri daha tutarli hale getirildi.
- Yedek, onbellek, test ve lokal demo senaryolari icin yeni arac scriptleri eklendi.

Kurulumdan sonra masaustunde uygulama kisayolu `Cashtracker Fabesco` olarak olusturulur.

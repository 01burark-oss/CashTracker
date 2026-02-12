# CashTracker EXE Öncesi Son Kontrol (Manual UX Smoke Test)

Bu checklist, release EXE almadan önce kritik kullanıcı akışlarını 10-15 dakikada doğrulamak içindir.

## 0) Ön Hazırlık

- [ ] Uygulama kapalı.
- [ ] Test için temiz başlangıç gerekiyorsa yerel kullanıcı verisi temizlendi:
  - `%LOCALAPPDATA%\CashTracker\telegram-setup.json`
  - `%LOCALAPPDATA%\CashTracker\cashtracker.db`
- [ ] İnternet erişimi açık (Telegram + GitHub update testi için).

## 1) İlk Açılış / Kurulum Ekranı

- [ ] Uygulama ilk kez açıldığında kurulum ekranı geliyor.
- [ ] Ekranda Türkçe metinler bozuk değil (Ã, Ä, Å gibi karakter yok).
- [ ] Kurulum ekranında aşağı kaydırma ihtiyacı yok.
- [ ] `Bot Token` ve `Telegram User ID` alan doğrulamaları doğru çalışıyor.
- [ ] `Bağlantıyı Test Et` başarılı olduğunda kurulum tamamlanabiliyor.
- [ ] Geçersiz token/user id ile kurulum tamamlanamıyor.

Kabul kriteri:
- Doğrulama ve test olmadan `Kurulumu Tamamla` aktif olmamalı.

## 2) Chat ID Otomatik Alma Güvenliği

- [ ] `/start` sonrası `Chat ID'yi Otomatik Al` doğru Chat ID yazıyor.
- [ ] Birden fazla chat senaryosunda yanlış ID otomatik seçilmiyor.
- [ ] Belirsiz durumda kullanıcıya uyarı veriliyor.

Kabul kriteri:
- Yanlış kullanıcıya ait Chat ID sessizce atanmamalı.

## 3) Ana Ekran UX / Fullscreen

- [ ] Ana ekranda Türkçe metinler doğru görünüyor.
- [ ] Tam ekran yapınca ana layout bozulmuyor.
- [ ] Ana ekranda istenmeyen dikey scroll oluşmuyor.
- [ ] Sidebar kapalıyken logo merkezli kompakt görünüm doğru.
- [ ] Sidebar açıldığında `CASHTRACKER` adı tam görünüyor, taşma yok.

Kabul kriteri:
- Tam ekran + normal ekran arasında yerleşim kırılması olmamalı.

## 4) Sol Menü Akışları

- [ ] `Gelir / Gider Kayıtları` formu açılıyor.
- [ ] `Botu Değiştir` kurulum ekranını açıyor.
- [ ] Reconfigure modunda `Geri Dön` butonu çalışıyor.
- [ ] Bot değiştirip kaydettiğinde uygulama yeniden başlıyor.

Kabul kriteri:
- Kullanıcı yanlış tıklamada geri dönebilmeli, uygulamadan kopmamalı.

## 5) Kasa Formu

- [ ] Listeleme, yeni kayıt, güncelleme, silme akışları çalışıyor.
- [ ] `Gider` seçiliyken gider türü zorunlu.
- [ ] Form resize olduğunda kullanım bozulmuyor.
- [ ] Sağ panelde gereksiz scroll davranışı yok.

Kabul kriteri:
- Kayıt düzenlemede veri tutarlılığı korunmalı.

## 6) Telegram Gönderimleri

- [ ] Günlük / 30 gün / 365 gün gönderimleri çalışıyor.
- [ ] Aylık ve yıllık gönderim çalışıyor.
- [ ] Hata durumunda kullanıcı anlamlı mesaj alıyor.

Kabul kriteri:
- Başarılı gönderimde onay, başarısızda güvenli hata mesajı gösterilmeli.

## 7) Güncelleme Akışı

- [ ] `Güncellemeleri Denetle` yeni sürümü bulabiliyor.
- [ ] Checksum (`.sha256`) yoksa güncelleme güvenlik nedeniyle duruyor.
- [ ] Checksum doğrulaması geçerse kurulum devam ediyor.
- [ ] Zip paket güncellemesi sonrası yeni sürüm açılıyor.

Kabul kriteri:
- Doğrulama olmadan paket çalıştırılmamalı.

## 8) Release Teknik Kontrol

- [ ] `dotnet build CashTracker.sln` başarılı.
- [ ] `dotnet publish` başarılı.
- [ ] Release workflow dosyasında:
  - `--self-contained true`
  - `.sha256` üretimi
  - `.sha256` asset upload

## 9) Son Onay

- [ ] Kritik bug yok.
- [ ] Türkçe metin kalitesi kabul edilebilir.
- [ ] Güncelleme, kurulum ve bot değiştir akışları geçti.
- [ ] EXE release için onay verildi.


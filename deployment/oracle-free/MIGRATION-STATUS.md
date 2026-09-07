# Geçiş durumu — 2 Eylül 2026, 10:42 UTC

## Doğrulananlar

- 11:00 UTC sonrası doğrulama: kaynak DigitalOcean'daki 33 tablo son taşıma dump'ı ile birebir eşleşti. Kaynak şeması sütunları üzerinden karşılaştırılan 297 kaydın tamamı Oracle'da aynı içerikle mevcut (eşleşmeyen kaynak kaydı: 0). Oracle'da yeni şema ve ek kayıtlar korunuyor.
- `ef5f390` Oracle'da derlendi ve yayınlandı; readiness ve smoke geçti. GitHub deposu private olduğundan sunucudaki `git pull` kimlik doğrulaması başarısız; bu yayın doğrulanmış Git bundle ile yapıldı.
- 10:58:50 UTC güncel Oracle yedeği bilgisayara DPAPI ile şifreli indirildi; checksum/şifre çözme kontrolleri geçti. Aynı dump ayrıca `systemcel_restore_check_20260902` veritabanına hatasız geri yüklendi.
- Geçici kaynak veritabanı erişim dosyaları bilgisayardan ve Oracle'dan kaldırıldı. Denetim kopyaları Oracle'da `/opt/systemcel/backups/source-audit-20260902` altında tutuluyor.

- Oracle reserved IP: `89.168.102.124`.
- Uygulama, Caddy ve PostgreSQL 18 container'ları çalışıyor; veritabanı healthy.
- Oracle IP'sine doğrudan yönlendirilen HTTPS readiness isteği başarılı.
- `systemcel.app` ve `www.systemcel.app` sertifikaları alınmış; www, ana domaine 301 yönleniyor.
- Name.com yetkili DNS ve Google DNS Oracle IP'sini döndürüyor. Cloudflare DNS eski DigitalOcean adreslerini önbellekten döndürüyor; yayılım tamamlandı sayılmıyor.
- Clerk public config canlı anahtar kullanıyor. Ortam etiketi hâlâ `oracle-production-candidate`.
- Günlük yedek timer'ı etkin. İlk yedek 10:39 UTC'de tamamlandı; dump/arşiv checksum kontrolleri geçti.
- Disk kullanımı %16. Yeni ücretli kaynak oluşturulmadı.

## Açık işler

- Kaynak-hedef karşılaştırması geçti; kaynak kapatılana kadar mevcut bağlantıların tekrar yazma ihtimalini gözet.
- DNS yayılımı sonrası gerçek kullanıcı oturumu, OAuth, tenant izolasyonu ve bildirim teslimatını doğrula.
- 10:50:28 UTC yedeğinin şifreli bir kopyası Windows bilgisayarına indirildi (`%LOCALAPPDATA%\Systemcel\Backups`). SHA-256 ve DPAPI şifre çözme kontrolü geçti. Bu kopya ilgili Windows kullanıcı profiline bağlıdır; profil/anahtar kaybına karşı taşınabilir kurtarma anahtarı değildir. Otomatik sunucu dışı aktarım henüz kurulmadı.
- Oracle'a otomatik GitHub dağıtımı kurulmadı. Mevcut CI yalnız test/build yapıyor; `scripts/deploy.sh` sunucudaki checkout'u dağıtır, GitHub'dan güncellemez.
- Kullanıcının açık onayıyla DigitalOcean `systemcel-staging` uygulaması ve `systemcel-db-dev` managed veritabanı 2 Eylül 2026 yaklaşık 11:05 UTC'de kalıcı silindi; boş uygulama/veritabanı listeleri doğrulandı. 11:06 UTC'de kullanıcının bilgisayarından Oracle IP'sine HTTPS readiness 200 döndü. Geçmiş borç $22.46 ve panelde görülen ay içi tahmini kullanım $0.86 silinmez.

Oracle'da yeni yazmalar başladıktan sonra yalnız DNS'i geri çevirmek güvenli bir geri dönüş değildir; önce veri farkı uzlaştırılmalıdır. PayTR kapsam dışıdır.

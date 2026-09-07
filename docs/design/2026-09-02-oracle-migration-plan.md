# Systemcel DigitalOcean → Oracle geçiş planı

**Durum:** Öneri
**Tarih:** 2026-09-02
**Karar:** Canlı sistem, önce Oracle üzerinde gerçek verinin kopyasıyla prova edilecek; nihai geçiş kısa bir yazma kesintisi, son PostgreSQL dump/restore ve kontrollü DNS değişikliğiyle yapılacak.

Bu karar belgeyi hak ediyor: kalıcı veriyi, kimlik doğrulamayı, DNS'i ve iki ayrı bulut sağlayıcısını birlikte etkiliyor; başarısız bir geçiş veri kaybına veya oturum kesintisine yol açabilir ve geri dönüş maliyetlidir.

## 1. Amaç ve başarı ölçütleri

Amaç, `systemcel.app` trafiğini DigitalOcean App Platform ve Managed PostgreSQL'den Oracle Cloud Always Free sunucusuna taşırken hiçbir işletme kaydını veya yüklenmiş dosyayı kaybetmemektir.

Geçiş tamamlandı sayılmak için:

1. Oracle'daki satır sayıları ve kritik toplamlar son DigitalOcean yedeğiyle eşleşmeli.
2. `SYSTEMCEL_APPDATA` altındaki dosya sayısı ve SHA-256 manifesti eşleşmeli.
3. `/api/health/live` ve `/api/health/ready` HTTP 200 dönmeli.
4. Production Clerk ile e-posta ve Google OAuth girişi çalışmalı.
5. İki farklı işletme arasında tenant izolasyonu yeniden test edilmeli.
6. Ana ekran, finans, gelir/gider, fatura, hızlı satış, dosya indirme ve muhasebeci sohbeti smoke testten geçmeli.
7. SMTP ve Telegram kontrollü test edilmeli; aynı mesajı iki ortam birden göndermemeli.
8. PayTR onayı gelene kadar `SYSTEMCEL_PAYMENT_PROVIDER=Unconfigured` kalmalı.
9. İlk 24 saat hata oranı, disk, RAM, PostgreSQL bağlantıları ve yedek sonucu izlenmeli.
10. DNS geçişinden önce public IP kalıcılığı güvenceye alınmalı.
11. Mevcut `SYSTEMCEL_SECRET_ENCRYPTION_KEY` aynen taşınmalı; yeni anahtar üretmek şifreli production kayıtlarını okunamaz hale getirebilir.

## 2. Bilinen kapasite ve ölçülmesi gerekenler

Oracle hedefi hazırdır:

- 2 ARM OCPU
- 6 GB RAM
- 46,6 GB disk
- 2 GB swap
- Reserved public IP `89.168.102.124`
- TCP 22/80/443 açık; PostgreSQL internete kapalı
- Docker image `linux/arm64` olarak başarıyla derlendi

Geçiş tarihi belirlenmeden önce aşağıdakiler ölçülmelidir:

- DigitalOcean PostgreSQL sürümü, veritabanı boyutu ve tablo satır sayıları
- `$SYSTEMCEL_APPDATA` toplam boyutu, dosya sayısı ve en büyük dosya
- Günlük istek, eşzamanlı kullanıcı ve yoğun saat
- DNS sağlayıcısı ve mevcut TTL
- DigitalOcean hizmetlerinin fatura nedeniyle kapanacağı kesin tarih

Planlama varsayımı: toplam taşınacak veri 5 GB veya altındaysa 20–30 dakikalık bakım penceresi ayrılır. 5–20 GB arasında 45–90 dakika ayrılır. Ölçüm bu aralıkların dışındaysa pencere yeniden hesaplanır.

Disk kapısı: PostgreSQL verisi + appdata + Docker image/cache + yerel yedeklerin toplamı 30 GB'ı aşmamalı. Aşarsa canlı geçişten önce Oracle Block Volume veya harici nesne depolama eklenmelidir.

## 3. Önerilen mimari

- Caddy: 80/443 ve otomatik TLS
- Systemcel app: yalnız Caddy ağı ve host üzerinde `127.0.0.1:8080`
- PostgreSQL: kaynak DigitalOcean ana sürümüyle aynı veya daha yeni sürüm; yalnız internal Docker ağı, host portu yok
- Named volume `postgres_data`: veritabanı
- Named volume `app_data`: yüklemeler, sohbet ekleri, raporlar ve çalışma zamanı durumu
- Harici yedek: günlük `pg_dump` + appdata arşivi; aynı VM dışında şifreli kopya

Tek app container kullanılacaktır. Uygulama başlangıçta EF migration çalıştırdığı için aynı şemaya birden fazla yeni sürümün eşzamanlı migration uygulaması önlenecektir.

## 4. Alternatifler

### A. DigitalOcean'da kalmak

En iyi yanı yönetilen veritabanı, otomatik dağıtım ve daha az operasyon yüküdür. Ancak mevcut maliyet sorunu çözülmez. Hesap borcu nedeniyle hizmet kesintisi riski de devam eder.

### B. Tek seferde doğrudan Oracle'a geçmek

En hızlı yoldur, ancak gerçek veriyle prova yapılmadan DNS değişir. Dosya, Clerk veya migration hatası ancak kullanıcılar etkilendikten sonra görülür. Reddedildi.

### C. Prova + kontrollü bakım penceresi + DNS geçişi

Bir geçici Oracle adresinde gerçek verinin kopyası doğrulanır; nihai geçişte yazmalar durdurulur, son fark taşınır ve DNS değiştirilir. Birkaç ek adım gerektirir ancak veri kaybı ve geri dönüş riskini en çok azaltır. Önerilen yöntem budur.

Kararı değiştirecek durum: Oracle disk/performans ölçümü yetersiz kalırsa veya güvenilir sunucu dışı yedekleme kurulamazsa DigitalOcean'da kalınmalı ya da düşük maliyetli yönetilen PostgreSQL seçeneği kullanılmalıdır.

## 5. Uygulama sırası

### Aşama 0 — Değişiklik dondurma

1. Geçişe kadar production branch'e otomatik dağıtımı kapat.
2. DigitalOcean uygulamasını yeniden başlatma veya yeniden deploy etme; App Platform yerel dosya sistemi kalıcı değildir.
3. PayTR ve ödeme koduna dokunma.
4. Geçiş sorumlusu, başlangıç saati ve vazgeçme saati belirle.
5. Hesap askıya alınma riski 3 Eylül ise, veritabanı ve appdata dışa aktarımını diğer tüm hazırlıklardan önce tamamla.

### Aşama 1 — Envanter ve ilk yedek

1. PostgreSQL sürümünü ve boyutunu kaydet:

   ```sql
   select version();
   select pg_size_pretty(pg_database_size(current_database()));
   ```

   Kaynak ana sürüm 18 veya daha yeniyse Oracle compose içindeki PostgreSQL 17 image'ı kullanılmaz; hedef image önce kaynakla aynı ana sürüme yükseltilir.

2. Kritik tablolar için satır sayısı ve finansal kontrol toplamı üret. En az işletme, kullanıcı, gelir/gider, fatura, tahsilat, abonelik ve sohbet tablolarını kapsa.
3. DigitalOcean veritabanından `pg_dump --format=custom` al ve `pg_restore --list` ile okunabildiğini doğrula.
4. Çalışan DigitalOcean container içindeki `$SYSTEMCEL_APPDATA` klasörünü tar ve SHA-256 manifesti üret.
5. Appdata arşivini üçüncü taraf açık dosya paylaşım servisine yükleme. Oracle Object Storage pre-authenticated URL veya doğrudan şifreli aktarım kullan.

### Aşama 2 — Oracle prova ortamı

1. `oracle-candidate.systemcel.app` gibi geçici bir DNS kaydını `89.168.102.124` adresine yönlendir.
2. Caddy'nin geçici alan adı için TLS sertifikası aldığını doğrula.
3. Production sırlarını `/opt/systemcel/repo/deployment/oracle-free/.env` içine, `chmod 600` ile yerleştir. Sırlar repoya veya komut çıktısına yazılmamalı.
4. Clerk production instance'a geçici origin/callback ekle; ana `systemcel.app` değerlerini kaldırma.
5. Prova ortamında Telegram token, SMTP gönderimi ve abonelik/bildirim background işlerini etkinleştirme; iki production kopyasının yan etki üretmesine izin verme.
6. İlk PostgreSQL dump ve appdata arşivini Oracle'a geri yükle.
7. `scripts/smoke.sh` ve authenticated tarayıcı testlerini çalıştır.
8. Oracle'da bir prova yedeği al ve ayrı, boş volume/veritabanına geri yükleyerek yedeğin gerçekten kullanılabilir olduğunu kanıtla.
9. `restore.sh` kullanılmadan önce app/Caddy'nin veritabanı işleminden önce durdurulduğunu, `pg_restore --exit-on-error --no-owner --no-acl` kullandığını ve giriş arşivlerinin checksum'ını doğruladığını test et.

### Aşama 3 — Geçişten 24 saat önce

1. `systemcel.app` DNS TTL değerini 300 saniyeye indir.
2. Kullanıcılara kısa bakım penceresini bildir.
3. Oracle disk, RAM ve container durumunu kontrol et.
4. Oracle `.env` içinde production domain, Clerk ve SMTP değerlerini son kez doğrula.
5. DigitalOcean ve Oracle saatlerinin UTC senkronunu doğrula.
6. Geri dönüş komutları ve eski DNS hedefi ayrı notta hazır bulunsun.
7. Ephemeral IP yerine reserved public IP ata ve DNS hedefini son kez doğrula.
8. A kaydının yanında eski veya kullanılmayan AAAA kaydı bulunmadığını doğrula.

### Aşama 4 — Nihai veri kesintisi ve taşıma

1. Kullanıcı trafiğini bakım sayfasına yönlendir; yeni yazmaları durdur.
2. Telegram polling, zamanlanmış bildirim ve abonelik hosted service'lerinin yalnız bir ortamda çalışacağından emin ol.
3. DigitalOcean appdata için son arşiv ve SHA-256 manifesti al; arşiv Oracle'a ulaştıktan sonra kaynağı durdur.
4. DigitalOcean PostgreSQL için son custom-format dump al.
5. Oracle'da app ve Caddy'yi durdur, veritabanı ve appdata'yı son yedekten geri yükle.
6. Oracle app'i tek replica olarak başlat; migration ve readiness loglarını kontrol et.
7. Kritik tablo sayıları, finansal toplamlar ve dosya hashlerini karşılaştır.

Bu aşamada herhangi bir karşılaştırma başarısızsa DNS değiştirilmez; DigitalOcean yeniden açılır.

### Aşama 5 — DNS ve kontrollü açılış

1. `systemcel.app` ve gerekli `www` kaydını `89.168.102.124` adresine çevir.
2. Caddy production sertifikasını alana kadar bakım sayfasını koru.
3. Dış ağdan HTTPS, HSTS, CSP ve readiness kontrolü yap.
4. Clerk e-posta girişi ve Google OAuth'u test et.
5. İki tenant ile izolasyon testi yap.
6. Bir kontrollü gelir/gider kaydı oluştur, oku ve silme yerine ters kayıt/uygun geri alma akışıyla doğrula.
7. SMTP ve Telegram'da tek bir kontrollü teslimat yap.
8. Tüm kapılar geçince bakım sayfasını kaldır ve yazmaları aç.

### Aşama 6 — İlk 24 saat

1. 15 dakika, 1 saat, 6 saat ve 24 saatte health, hata logu, RAM, swap, disk ve PostgreSQL bağlantılarını kontrol et.
2. İlk otomatik yedeğin oluştuğunu, checksum'unu ve sunucu dışı kopyasını doğrula.
3. Disk kullanımı %70, RAM sürekli %85 veya swap kullanımı sürekli artıyorsa geçişi başarısız kabul et ve kapasite kararını yeniden değerlendir.
4. DigitalOcean kaynaklarını hemen silme.

## 6. Geri dönüş

### Yazmalar açılmadan önce

DNS eski DigitalOcean hedefine alınır ve mevcut DigitalOcean app yeniden açılır. Oracle'daki test verisi atılabilir; kaynak gerçek veri hâlâ DigitalOcean'dır.

### Oracle'da yazmalar açıldıktan sonra

Basit DNS dönüşü veri kaybettirir. Önce bakım modu açılır, Oracle'dan son PostgreSQL dump ve appdata arşivi alınır, DigitalOcean'a ters yönde geri yüklenir, doğrulama tamamlandıktan sonra DNS geri çevrilir.

DigitalOcean app ve veritabanı en az 72 saat tutulmalıdır. Hesap askıya alınma tarihi 72 saatten kısaysa geçiş penceresi buna göre öne çekilmeli veya borç kapatılmalıdır.

Sorun yalnız uygulama sürümündeyse veriyi geri taşımak yerine Oracle üzerinde önceki doğrulanmış image'a dönmek ilk tercihtir. DNS ve veritabanı geri dönüşü yalnız altyapı/veri sorunu varsa uygulanır.

## 7. Riskler ve önlemler

| Risk | Önlem |
| --- | --- |
| App Platform yerel dosyaları deploy sırasında kaybolur | Otomatik deploy'u dondur; mevcut container'dan appdata arşivini önce çıkar |
| Dump alınırken yeni kayıt yazılır | Nihai dump öncesi kullanıcı ve background yazmalarını durdur |
| Clerk/Google OAuth origin uyuşmazlığı | Geçici alan adıyla prova et; production origin'i cutover öncesi kaldırma |
| Şifreleme anahtarı değişir | DigitalOcean'daki `SYSTEMCEL_SECRET_ENCRYPTION_KEY` değerini loglamadan ve değiştirmeden taşı |
| İki ortam Telegram/e-posta gönderir | Background servisleri tek ortamda etkin tut |
| Oracle VM veya disk kaybı | Günlük yedeği aynı VM dışında şifreli sakla ve geri yüklemeyi test et |
| Ephemeral public IP değişir | Instance'ı silme; DNS öncesi reserved IP seçeneğini değerlendir |
| ARM64 bağımlılık farkı | Image build geçti; barkod sunucu tarafı Windows özelliğinin Linux'ta kapalı olduğunu kabul et ve tarayıcı akışını test et |
| Tek VM uygulama ve DB'yi birlikte kaybeder | İzleme, sunucu dışı yedek ve belgelenmiş yeniden kurulum süresi koy |

## 8. Kapsam dışı

- PayTR'yi canlıya alma
- Uygulama özelliklerini veya fiyatlarını değiştirme
- Çok bölgeli yüksek erişilebilirlik
- DigitalOcean kaynaklarını geçiş anında silme
- Mevcut production verisini anonimleştirmeden üçüncü taraf test ortamlarına kopyalama

## 9. Açık sorular

Geçiş uygulamasına başlamadan önce yalnız şu bilgiler kesinleşmelidir:

1. DigitalOcean veritabanı ve `$SYSTEMCEL_APPDATA` gerçek boyutları nedir?
2. DNS hangi sağlayıcıda ve mevcut TTL kaç saniyedir?
3. Kabul edilen bakım penceresi kaç dakikadır?
4. DigitalOcean hesabının kesin askıya alınma tarihi nedir?
5. Sunucu dışı şifreli yedek için Oracle Object Storage mı, Cloudflare R2 mı kullanılacak?

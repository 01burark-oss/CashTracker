# Oracle Always Free dağıtım adayı

Bu klasör, mevcut DigitalOcean yayınını bozmadan Systemcel'i Oracle Cloud Always Free ARM64 sunucusunda hazırlamak için ayrıdır. DNS değişikliği ve canlı trafik geçişi bu paketin otomatik bir parçası değildir.

## Hazırlanan sunucu

- Ubuntu 24.04 ARM64
- 2 OCPU, 6 GB RAM, 46,6 GB disk ve 2 GB swap
- Reserved public IP: `89.168.102.124`
- OCI NSG ve UFW üzerinde yalnızca TCP `22`, `80`, `443`
- Docker, Compose, Fail2ban ve Git
- Çalışma dizini: `/opt/systemcel`

Public IP rezerve edilmiştir: `89.168.102.124`.

## Mimari

- `caddy`: TLS sonlandırma ve ters proxy
- `app`: Systemcel API ve derlenmiş web arayüzü
- `db`: internete kapalı PostgreSQL 18
- `app_data`: yüklemeler, sohbet ekleri ve çalışma zamanı dosyaları
- `postgres_data`: PostgreSQL verisi

Uygulama portu yalnızca sunucunun `127.0.0.1:8080` adresine bağlanır. PostgreSQL için host portu yayımlanmaz.
Container içindeki `HOME` ve `SYSTEMCEL_APPDATA` aynı kalıcı volume'e yönlendirilir; aylık rapor çıktıları da böylece geçici container katmanında kalmaz.

## İlk kurulum

```bash
cd /opt/systemcel/repo/deployment/oracle-free
cp .env.example .env
chmod 600 .env
```

`.env` içine mevcut production Clerk değerleri, sabit şifreleme anahtarı ve güçlü PostgreSQL parolası girilmelidir. PayTR onayı gelene kadar `SYSTEMCEL_PAYMENT_PROVIDER=Unconfigured` kalmalıdır.

İlk IP tabanlı doğrulamada `CADDY_SITE_ADDRESS=http://89.168.102.124` kullanılır. DNS geçişinde değer `systemcel.app` yapılır; Caddy alan adı sunucuya çözüldüğünde sertifikayı otomatik alır.

## Dağıtım ve doğrulama

```bash
chmod +x scripts/*.sh
./scripts/deploy.sh
curl --fail http://127.0.0.1:8080/api/health/ready
curl --fail https://systemcel.app/api/health/ready
./scripts/smoke.sh http://127.0.0.1:8080
```

Canlı geçiş yapılmadan önce mevcut DigitalOcean PostgreSQL verisi ve `/var/lib/systemcel` dosyaları kontrollü bakım penceresinde taşınmalı; kayıt sayıları, dosya hashleri, Clerk oturumu ve tenant izolasyonu doğrulanmalıdır.

## Yedekleme

Günlük systemd zamanlayıcısı sunucuda etkindir; 03:00 UTC (06:00 Türkiye), en fazla beş dakika rastgele gecikmeyle çalışır. İlk servis çalışması 2 Eylül 2026'da başarılı oldu. Kurulum ve kontrol:

```bash
sudo install -m 644 systemcel-backup.service systemcel-backup.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now systemcel-backup.timer
sudo systemctl start systemcel-backup.service
sudo systemctl list-timers systemcel-backup.timer
sudo journalctl -u systemcel-backup.service --no-pager -n 20
```

```bash
./scripts/backup.sh
```

Betik PostgreSQL özel-format dump, uygulama verisi arşivi ve bu iki dosyayı kapsayan SHA-256 manifesti üretir. Tamamlanmamış çıktıları yayınlamaz; dump listesini, arşivi ve checksum'ları oluşturma sırasında doğrular. Yerel diskteki 14 günden eski `systemcel-*` yedeklerini temizler. Kalıcı işletim için bu çıktılar ayrıca şifreli, sunucu dışı nesne depolamaya kopyalanmalıdır; aynı diskteki yedek tek başına felaket kurtarma sayılmaz.

Canlı geçişte son ve tutarlı kopyayı almak için uygulama yazmalarını kısa süreli durduran seçenek kullanılmalıdır:

```bash
./scripts/backup.sh --quiesce
```

Bu seçenek çalışıyorsa `app` ve `caddy` servislerini durdurur, yedek tamamlandığında yeniden başlatır. Normal periyodik yedek, kesinti oluşturmamak için parametresiz çalışır.

Geri yükleme betiği bilerek etkileşimli ve yıkıcı işlem uyarılıdır:

```bash
./scripts/restore.sh \
  /opt/systemcel/backups/systemcel-db-TARIH.dump \
  /opt/systemcel/backups/systemcel-appdata-TARIH.tar.gz \
  /opt/systemcel/backups/systemcel-TARIH.sha256
```

Üç dosya aynı klasörde ve aynı zaman damgasıyla bulunmalıdır. Betik yıkıcı işleme başlamadan önce manifesti, dump yapısını ve arşiv yollarını doğrular; ardından `app` ile `caddy` servislerini kapatır. PostgreSQL geri yüklemesi hata verirse web servisleri kapalı kalır ve bozuk/eksik veri trafik almaz. Başarılı işlem sonunda veritabanı sorgusu ve yerel smoke testi otomatik çalışır.

## Canlı geçiş kapıları

1. ARM64 image build ve readiness kontrolü geçmeli.
2. DigitalOcean'dan alınan PostgreSQL dump deneme geri yüklemesinde doğrulanmalı.
3. Uygulama veri klasörü dosya sayısı ve hash ile karşılaştırılmalı.
4. Clerk production giriş, OAuth callback ve tenant izolasyonu test edilmeli.
5. SMTP ve Telegram kontrollü test edilmeli.
6. DNS TTL düşürülmeli, son veri senkronu için kısa yazma kesintisi uygulanmalı.
7. `systemcel.app` Oracle IP'sine çevrilip HTTPS ve smoke testleri geçmeli.
8. Geri dönüş süresi boyunca DigitalOcean kaynağı silinmemeli.

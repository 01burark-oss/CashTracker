using CashTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public sealed class CashTrackerDbContext : DbContext
    {
        public CashTrackerDbContext(DbContextOptions<CashTrackerDbContext> options) : base(options) { }

        public DbSet<Kasa> Kasalar => Set<Kasa>();
        public DbSet<Isletme> Isletmeler => Set<Isletme>();
        public DbSet<KalemTanimi> KalemTanimlari => Set<KalemTanimi>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<CariKart> CariKartlari => Set<CariKart>();
        public DbSet<CariHareket> CariHareketleri => Set<CariHareket>();
        public DbSet<UrunHizmet> UrunHizmetleri => Set<UrunHizmet>();
        public DbSet<StokHareket> StokHareketleri => Set<StokHareket>();
        public DbSet<Fatura> Faturalar => Set<Fatura>();
        public DbSet<FaturaSatir> FaturaSatirlari => Set<FaturaSatir>();
        public DbSet<TahsilatOdeme> TahsilatOdemeleri => Set<TahsilatOdeme>();
        public DbSet<BelgeDosya> BelgeDosyalari => Set<BelgeDosya>();
        public DbSet<GibPortalAyar> GibPortalAyarlari => Set<GibPortalAyar>();
        public DbSet<GibPortalIslemLog> GibPortalIslemLoglari => Set<GibPortalIslemLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Kasa>(e =>
            {
                e.ToTable("Kasa");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.OdemeYontemi).IsRequired();
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tarih });
            });

            modelBuilder.Entity<Isletme>(e =>
            {
                e.ToTable("Isletme");
                e.HasKey(x => x.Id);
                e.Property(x => x.Ad).IsRequired();
                e.HasIndex(x => x.IsAktif);
            });

            modelBuilder.Entity<KalemTanimi>(e =>
            {
                e.ToTable("KalemTanimi");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Ad).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tip, x.Ad }).IsUnique();
            });

            modelBuilder.Entity<AppSetting>(e =>
            {
                e.ToTable("AppSetting");
                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired();
                e.Property(x => x.Value).IsRequired();
                e.HasIndex(x => x.Key).IsUnique();
            });

            modelBuilder.Entity<CariKart>(e =>
            {
                e.ToTable("CariKart");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Unvan).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Unvan });
                e.HasIndex(x => new { x.IsletmeId, x.VergiNoTc });
            });

            modelBuilder.Entity<CariHareket>(e =>
            {
                e.ToTable("CariHareket");
                e.HasKey(x => x.Id);
                e.Property(x => x.HareketTipi).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId, x.Tarih });
            });

            modelBuilder.Entity<UrunHizmet>(e =>
            {
                e.ToTable("UrunHizmet");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Ad).IsRequired();
                e.Property(x => x.Barkod).IsRequired();
                e.Property(x => x.Birim).IsRequired();
                e.Property(x => x.KdvOrani).HasColumnType("NUMERIC");
                e.Property(x => x.AlisFiyati).HasColumnType("NUMERIC");
                e.Property(x => x.SatisFiyati).HasColumnType("NUMERIC");
                e.Property(x => x.KritikStok).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Barkod });
            });

            modelBuilder.Entity<StokHareket>(e =>
            {
                e.ToTable("StokHareket");
                e.HasKey(x => x.Id);
                e.Property(x => x.Miktar).HasColumnType("NUMERIC");
                e.Property(x => x.HareketTipi).IsRequired();
                e.Property(x => x.Kaynak).IsRequired();
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.UrunHizmetId, x.Tarih });
            });

            modelBuilder.Entity<Fatura>(e =>
            {
                e.ToTable("Fatura");
                e.HasKey(x => x.Id);
                e.Property(x => x.FaturaTipi).IsRequired();
                e.Property(x => x.Durum).IsRequired();
                e.Property(x => x.AraToplam).HasColumnType("NUMERIC");
                e.Property(x => x.IskontoToplam).HasColumnType("NUMERIC");
                e.Property(x => x.KdvToplam).HasColumnType("NUMERIC");
                e.Property(x => x.GenelToplam).HasColumnType("NUMERIC");
                e.Property(x => x.OdenenTutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.Tarih });
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId });
            });

            modelBuilder.Entity<FaturaSatir>(e =>
            {
                e.ToTable("FaturaSatir");
                e.HasKey(x => x.Id);
                e.Property(x => x.Miktar).HasColumnType("NUMERIC");
                e.Property(x => x.BirimFiyat).HasColumnType("NUMERIC");
                e.Property(x => x.IskontoOrani).HasColumnType("NUMERIC");
                e.Property(x => x.IskontoTutar).HasColumnType("NUMERIC");
                e.Property(x => x.KdvOrani).HasColumnType("NUMERIC");
                e.Property(x => x.KdvTutar).HasColumnType("NUMERIC");
                e.Property(x => x.SatirNetTutar).HasColumnType("NUMERIC");
                e.Property(x => x.SatirToplam).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
            });

            modelBuilder.Entity<TahsilatOdeme>(e =>
            {
                e.ToTable("TahsilatOdeme");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
                e.HasIndex(x => new { x.IsletmeId, x.CariKartId, x.Tarih });
            });

            modelBuilder.Entity<BelgeDosya>(e =>
            {
                e.ToTable("BelgeDosya");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId });
            });

            modelBuilder.Entity<GibPortalAyar>(e =>
            {
                e.ToTable("GibPortalAyar");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsletmeId).IsUnique();
            });

            modelBuilder.Entity<GibPortalIslemLog>(e =>
            {
                e.ToTable("GibPortalIslemLog");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsletmeId);
                e.HasIndex(x => new { x.IsletmeId, x.FaturaId, x.Tarih });
            });
        }
    }
}

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Kasa>(e =>
            {
                e.ToTable("Kasa");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
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
        }
    }
}

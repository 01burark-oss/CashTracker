using CashTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public sealed class CashTrackerDbContext : DbContext
    {
        public CashTrackerDbContext(DbContextOptions<CashTrackerDbContext> options) : base(options) { }

        public DbSet<Kasa> Kasalar => Set<Kasa>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Kasa>(e =>
            {
                e.ToTable("Kasa");
                e.HasKey(x => x.Id);
                e.Property(x => x.Tip).IsRequired();
                e.Property(x => x.Tutar).HasColumnType("NUMERIC");
            });
        }
    }
}

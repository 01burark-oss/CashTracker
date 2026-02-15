using System.Threading;
using System.Threading.Tasks;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Tests.Support
{
    internal sealed class SingleDbContextFactory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;

        public SingleDbContextFactory(DbContextOptions<CashTrackerDbContext> options)
        {
            _options = options;
        }

        public CashTrackerDbContext CreateDbContext()
        {
            return new CashTrackerDbContext(_options);
        }

        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CashTrackerDbContext(_options));
        }
    }
}

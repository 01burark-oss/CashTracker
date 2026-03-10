using System.Collections.Generic;
using System.Linq;

namespace CashTracker.Core.Models
{
    public sealed class DatabasePaths
    {
        public string DbPath { get; }
        public IReadOnlyList<string> MirrorDbPaths { get; }

        public DatabasePaths(string dbPath, IEnumerable<string>? mirrorDbPaths = null)
        {
            DbPath = dbPath;
            MirrorDbPaths = (mirrorDbPaths ?? Enumerable.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}

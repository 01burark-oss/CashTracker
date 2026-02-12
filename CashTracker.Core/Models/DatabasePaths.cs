namespace CashTracker.Core.Models
{
    public sealed class DatabasePaths
    {
        public string DbPath { get; }

        public DatabasePaths(string dbPath)
        {
            DbPath = dbPath;
        }
    }
}

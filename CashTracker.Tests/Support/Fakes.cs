using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Tests.Support
{
    internal sealed class FakeKasaService : IKasaService
    {
        private readonly List<Kasa> _rows;

        public FakeKasaService(IEnumerable<Kasa>? seed = null)
        {
            _rows = seed?.ToList() ?? new List<Kasa>();
            NextId = _rows.Count == 0 ? 1 : _rows.Max(x => x.Id) + 1;
        }

        public int NextId { get; set; }
        public Kasa? LastCreated { get; private set; }

        public Task<List<Kasa>> GetAllAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _rows.AsEnumerable();

            if (from.HasValue)
                query = query.Where(x => x.Tarih >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.Tarih <= to.Value);

            return Task.FromResult(query.ToList());
        }

        public Task<Kasa?> GetByIdAsync(int id)
        {
            return Task.FromResult(_rows.FirstOrDefault(x => x.Id == id));
        }

        public Task<int> CreateAsync(Kasa kasa)
        {
            kasa.Id = NextId++;
            _rows.Add(kasa);
            LastCreated = kasa;
            return Task.FromResult(kasa.Id);
        }

        public Task UpdateAsync(Kasa kasa)
        {
            var existing = _rows.FindIndex(x => x.Id == kasa.Id);
            if (existing >= 0)
                _rows[existing] = kasa;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _rows.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeKalemTanimiService : IKalemTanimiService
    {
        private readonly List<KalemTanimi> _rows;

        public FakeKalemTanimiService(IEnumerable<KalemTanimi>? seed = null)
        {
            _rows = seed?.ToList() ?? new List<KalemTanimi>();
            NextId = _rows.Count == 0 ? 1 : _rows.Max(x => x.Id) + 1;
        }

        public int NextId { get; set; }

        public Task<List<KalemTanimi>> GetAllAsync()
        {
            return Task.FromResult(_rows.ToList());
        }

        public Task<List<KalemTanimi>> GetByTipAsync(string tip)
        {
            var rows = _rows
                .Where(x => string.Equals(x.Tip, tip, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(rows);
        }

        public Task<int> CreateAsync(string tip, string ad)
        {
            var row = new KalemTanimi
            {
                Id = NextId++,
                Tip = tip,
                Ad = ad
            };
            _rows.Add(row);
            return Task.FromResult(row.Id);
        }

        public Task UpdateAsync(int id, string ad)
        {
            var row = _rows.FirstOrDefault(x => x.Id == id);
            if (row != null)
                row.Ad = ad;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _rows.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeSummaryService : ISummaryService
    {
        public PeriodSummary SummaryToReturn { get; set; } = new PeriodSummary();

        public Task<PeriodSummary> GetSummaryAsync(DateTime from, DateTime to)
        {
            return Task.FromResult(new PeriodSummary
            {
                From = from,
                To = to,
                IncomeTotal = SummaryToReturn.IncomeTotal,
                ExpenseTotal = SummaryToReturn.ExpenseTotal,
                IncomeCount = SummaryToReturn.IncomeCount,
                ExpenseCount = SummaryToReturn.ExpenseCount
            });
        }

        public Task<PeriodSummary> GetMonthlySummaryAsync(int year, int month)
        {
            return Task.FromResult(SummaryToReturn);
        }
    }

    internal sealed class FakeIsletmeService : IIsletmeService
    {
        public Isletme Active { get; set; } = new Isletme { Id = 1, Ad = "Varsayilan", IsAktif = true };

        public Task<List<Isletme>> GetAllAsync()
        {
            return Task.FromResult(new List<Isletme> { Active });
        }

        public Task<Isletme?> GetByIdAsync(int id)
        {
            return Task.FromResult(id == Active.Id ? Active : null);
        }

        public Task<Isletme> GetActiveAsync()
        {
            return Task.FromResult(Active);
        }

        public Task<int> GetActiveIdAsync()
        {
            return Task.FromResult(Active.Id);
        }

        public Task<int> CreateAsync(string ad, bool makeActive = false)
        {
            var created = new Isletme
            {
                Id = Active.Id + 1,
                Ad = ad,
                IsAktif = makeActive
            };

            if (makeActive)
                Active = created;

            return Task.FromResult(created.Id);
        }

        public Task RenameAsync(int id, string ad)
        {
            if (id == Active.Id)
                Active.Ad = ad;

            return Task.CompletedTask;
        }

        public Task SetActiveAsync(int id)
        {
            Active.Id = id;
            Active.IsAktif = true;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeDailyReportService : IDailyReportService
    {
        public Task<DailyReport> GetDailyReportAsync(DateTime date)
        {
            return Task.FromResult(new DailyReport
            {
                Date = date,
                IncomeTotal = 0,
                ExpenseTotal = 0,
                IncomeCount = 0,
                ExpenseCount = 0
            });
        }
    }

    internal sealed class FakeAppSecurityService : IAppSecurityService
    {
        public string Pin { get; private set; } = "0000";

        public Task<string> GetPinAsync()
        {
            return Task.FromResult(Pin);
        }

        public Task SetPinAsync(string pin)
        {
            Pin = pin;
            return Task.CompletedTask;
        }
    }
}

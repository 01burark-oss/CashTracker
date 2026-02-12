using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramPollingService : IDisposable
    {
        private readonly TelegramSettings _settings;
        private readonly TelegramBotService _telegram;
        private readonly TelegramCommandService _commands;

        private readonly CancellationTokenSource _cts = new();
        private Task? _loopTask;
        private long? _offset;

        public TelegramPollingService(
            TelegramSettings settings,
            TelegramBotService telegram,
            TelegramCommandService commands)
        {
            _settings = settings;
            _telegram = telegram;
            _commands = commands;
        }

        public void Start()
        {
            if (!_settings.IsEnabled || !_settings.EnableCommands) return;
            if (_loopTask != null) return;

            _loopTask = Task.Run(() => RunAsync(_cts.Token));
        }

        private async Task RunAsync(CancellationToken ct)
        {
            await InitializeOffsetAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var updates = await _telegram.GetUpdatesAsync(
                        _offset,
                        _settings.GetSafePollTimeoutSeconds(),
                        ct);

                    foreach (var update in updates)
                    {
                        _offset = update.UpdateId + 1;
                        await _commands.ProcessUpdateAsync(update, ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"TelegramPollingService error: {ex}");
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }
        }

        private async Task InitializeOffsetAsync(CancellationToken ct)
        {
            try
            {
                var pending = await _telegram.GetUpdatesAsync(null, 0, ct);
                if (pending.Count > 0)
                {
                    _offset = pending.Max(x => x.UpdateId) + 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramPollingService init error: {ex}");
            }
        }

        public void Dispose()
        {
            if (_cts.IsCancellationRequested)
                return;

            _cts.Cancel();

            try
            {
                _loopTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TelegramPollingService dispose error: {ex}");
            }

            _cts.Dispose();
        }
    }
}

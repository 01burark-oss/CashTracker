using System;
using System.IO;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashTracker.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        EnvFileLoader.Load();

        var services = new ServiceCollection();

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CashTracker");

        Directory.CreateDirectory(appData);
        var dbPath = Path.Combine(appData, "cashtracker.db");

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var userSetup = UserTelegramSetupStore.Load(appData);

        var botToken = FirstNonEmpty(
            config["Telegram:BotToken"],
            userSetup.BotToken);

        var chatId = FirstNonEmpty(
            config["Telegram:ChatId"],
            userSetup.UserId);

        var allowedUserIds = FirstNonEmpty(
            config["Telegram:AllowedUserIds"],
            userSetup.UserId);

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
        {
            using var setupForm = new InitialSetupForm(botToken, userSetup.UserId);
            if (setupForm.ShowDialog() != DialogResult.OK)
                return;

            botToken = setupForm.BotToken;
            chatId = setupForm.UserId;
            allowedUserIds = setupForm.UserId;

            UserTelegramSetupStore.Save(appData, new UserTelegramSetup
            {
                BotToken = botToken,
                UserId = chatId
            });
        }

        var telegramSettings = new TelegramSettings
        {
            BotToken = botToken,
            ChatId = chatId,
            EnableCommands = !bool.TryParse(config["Telegram:EnableCommands"], out var ec) || ec,
            AllowedUserIds = allowedUserIds,
            PollTimeoutSeconds = int.TryParse(config["Telegram:PollTimeoutSeconds"], out var pts) ? pts : 20
        };

        var updateSettings = new UpdateSettings
        {
            RepoOwner = FirstNonEmpty(config["Update:RepoOwner"], "01burark-oss"),
            RepoName = FirstNonEmpty(config["Update:RepoName"], "CashTracker"),
            AssetName = FirstNonEmpty(config["Update:AssetName"], "CashTracker.exe"),
            ChecksumAssetName = FirstNonEmpty(config["Update:ChecksumAssetName"], "CashTracker.exe.sha256")
        };

        services.AddDbContextFactory<CashTrackerDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IIsletmeService, IsletmeService>();
        services.AddScoped<IKalemTanimiService, KalemTanimiService>();
        services.AddScoped<IKasaService, KasaService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddSingleton<IAppSecurityService, AppSecurityService>();

        services.AddSingleton(telegramSettings);
        services.AddSingleton(updateSettings);
        services.AddSingleton(new AppRuntimeOptions { AppDataPath = appData });
        services.AddSingleton(new DatabasePaths(dbPath));

        services.AddSingleton<HttpClient>();
        services.AddSingleton<GitHubUpdateService>();
        services.AddSingleton<TelegramBotService>(sp =>
            new TelegramBotService(sp.GetRequiredService<HttpClient>(), telegramSettings.BotToken));

        services.AddSingleton<IDailyReportService, DailyReportService>();
        services.AddSingleton<DatabaseBackupService>();
        services.AddSingleton<BackupReportService>();
        services.AddSingleton<TelegramCommandService>();
        services.AddSingleton<TelegramPollingService>();

        services.AddTransient<MainForm>();

        using var provider = services.BuildServiceProvider();

        using (var db = provider.GetRequiredService<IDbContextFactory<CashTrackerDbContext>>().CreateDbContext())
        {
            db.Database.EnsureCreated();
            SchemaMigrator.EnsureKasaSchema(db);
        }

        provider.GetRequiredService<TelegramPollingService>().Start();

        Application.Run(provider.GetRequiredService<MainForm>());
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }
}

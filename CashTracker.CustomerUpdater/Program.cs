using System.Text.Json;

namespace CashTracker.CustomerUpdater;

internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        var options = ParseArguments(args);

        if (options.Silent)
        {
            using var httpClient = new HttpClient();
            var service = new CustomerUpdateService(httpClient);
            var result = await service.RunAsync(options, progress: null, CancellationToken.None);
            WriteStatusFile(options.StatusFilePath, result);
            return result.Success ? 0 : 1;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();
        Application.Run(new CustomerUpdaterForm(options));
        return 0;
    }

    private static CustomerUpdaterOptions ParseArguments(string[] args)
    {
        var checkOnly = false;
        var silent = false;
        string statusFilePath = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--check-only", StringComparison.OrdinalIgnoreCase))
            {
                checkOnly = true;
                continue;
            }

            if (string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase))
            {
                silent = true;
                continue;
            }

            if (string.Equals(arg, "--status-file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                statusFilePath = args[++i];
        }

        return new CustomerUpdaterOptions
        {
            CheckOnly = checkOnly,
            Silent = silent,
            StatusFilePath = statusFilePath
        };
    }

    private static void WriteStatusFile(string statusFilePath, CustomerUpdaterResult result)
    {
        if (string.IsNullOrWhiteSpace(statusFilePath))
            return;

        try
        {
            var dir = Path.GetDirectoryName(statusFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(statusFilePath, json);
        }
        catch
        {
        }
    }
}

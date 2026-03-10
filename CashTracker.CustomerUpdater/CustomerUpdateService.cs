using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CashTracker.CustomerUpdater;

internal sealed class CustomerUpdateService
{
    private const string RepoOwner = "01burark-oss";
    private const string RepoName = "CashTracker";
    private const string InstallerBaseName = "CashTracker-Setup.exe";
    private const string DesktopShortcutName = "Cashtracker Fabesco";
    private const string LegacyDesktopShortcutName = "CashTracker";
    private const string DefaultUpdateManifestPublicKeyXml =
        "<RSAKeyValue><Modulus>ukz4hBxCeei00kiHJLs9ITV9xXi7eFB/HwYrHi6sf567d4y+IgKGWDqXGdAXHkHimDFYVZHwPNsSSaCoeGg6PamkgtjIn0Jzp+vCp3MFDEiLwGaqCf49uGnHDIBGjacoN2xWrC985E4nDBKwo2ZCqNmmMVyJaqrCa1t0uGhmZeelluh3amqTqijpd3kBNYZmvPLz7UVGGAiXIt827JxBYUvD6pEESAem2Vy9mYWr2spc7l5mEfMzBxtxPC8lI8Dg6vSP4kdpcnVb064sEvIMWSE31xCLOa9RaXk+vMm3XhRWyP+SkykhNXW0cOtSSIkHB2k5fhUZGAG5vu0kNmOlPQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;

    public CustomerUpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CustomerUpdaterResult> RunAsync(
        CustomerUpdaterOptions options,
        IProgress<CustomerUpdaterStatus>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            Report(progress, 5, "Son surum bilgisi aliniyor...");
            var latestPackage = await ResolveLatestPackageAsync(cancellationToken).ConfigureAwait(false);

            var workRoot = ResolveWritableWorkRoot();
            var downloadsDir = Path.Combine(workRoot, "downloads");
            Directory.CreateDirectory(downloadsDir);

            var packagePath = Path.Combine(downloadsDir, latestPackage.PackageFileName);
            if (!File.Exists(packagePath) || !PackageHashMatches(packagePath, latestPackage.Sha256))
            {
                Report(progress, 20, "Kurulum paketi indiriliyor...");
                await DownloadFileAsync(
                    latestPackage.PackageUrl,
                    packagePath,
                    progress,
                    20,
                    60,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Report(progress, 60, "Mevcut kurulum paketi yeniden kullaniliyor...");
            }

            VerifyPackageHash(packagePath, latestPackage.Sha256);

            if (options.CheckOnly)
            {
                var desktopShortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    $"{DesktopShortcutName}.lnk");

                return new CustomerUpdaterResult(
                    true,
                    $"Kontrol basarili. Son surum {latestPackage.LatestVersion} hazir.",
                    latestPackage.LatestVersion,
                    string.Empty,
                    desktopShortcutPath);
            }

            Report(progress, 68, "CashTracker kapatilmasi kontrol ediliyor...");
            await EnsureAppClosedAsync(cancellationToken).ConfigureAwait(false);

            Report(progress, 76, "Kurulum baslatiliyor...");
            await RunInstallerAsync(packagePath, cancellationToken).ConfigureAwait(false);

            var installedExePath = await WaitForInstalledExeAsync(cancellationToken).ConfigureAwait(false);

            Report(progress, 92, "Masaustu kisayolu olusturuluyor...");
            var shortcutPath = CreateDesktopShortcut(installedExePath);

            Report(progress, 100, "Guncelleme tamamlandi.");
            return new CustomerUpdaterResult(
                true,
                $"CashTracker {latestPackage.LatestVersion} kuruldu.",
                latestPackage.LatestVersion,
                installedExePath,
                shortcutPath);
        }
        catch (Exception ex)
        {
            return new CustomerUpdaterResult(false, ex.Message, string.Empty, string.Empty, string.Empty);
        }
    }

    private async Task<LatestPackageInfo> ResolveLatestPackageAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ResolveFromManifestAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return await ResolveFromReleaseApiAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<LatestPackageInfo> ResolveFromManifestAsync(CancellationToken cancellationToken)
    {
        var manifestUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases/latest/download/update-manifest.json";
        using var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
        request.Headers.UserAgent.ParseAdd("CashTrackerFabescoUpdater/1.0");
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Manifest alinamadi: {response.StatusCode}");

        var manifest = JsonSerializer.Deserialize<SignedUpdateManifest>(body, JsonOptions);
        if (manifest is null)
            throw new InvalidOperationException("Manifest okunamadi.");

        VerifyManifest(manifest);
        var packageFileName = GetFileNameFromUrl(manifest.PackageUrl);
        if (string.IsNullOrWhiteSpace(packageFileName))
            throw new InvalidOperationException("Manifest paket adini icermiyor.");

        return new LatestPackageInfo(
            NormalizeVersion(manifest.LatestVersion),
            manifest.PackageUrl,
            packageFileName,
            manifest.Sha256,
            $"https://github.com/{RepoOwner}/{RepoName}/releases/latest",
            manifest.ReleaseNotes);
    }

    private async Task<LatestPackageInfo> ResolveFromReleaseApiAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("CashTrackerFabescoUpdater/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub release bilgisi alinamadi: {response.StatusCode} - {body}");

        var release = JsonSerializer.Deserialize<GitHubReleaseResponse>(body, JsonOptions);
        if (release is null)
            throw new InvalidOperationException("GitHub release cevabi okunamadi.");

        var packageAsset = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, InstallerBaseName, StringComparison.OrdinalIgnoreCase)) ??
            release.Assets.FirstOrDefault(asset =>
                asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                asset.Name.Contains("setup", StringComparison.OrdinalIgnoreCase));

        if (packageAsset is null)
            throw new InvalidOperationException("Son release icinde kurulum paketi bulunamadi.");

        var checksumAsset = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, packageAsset.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
        if (checksumAsset is null)
            throw new InvalidOperationException("Son release icinde sha256 dosyasi bulunamadi.");

        var checksumText = await DownloadTextAsync(checksumAsset.BrowserDownloadUrl, cancellationToken).ConfigureAwait(false);
        var sha256 = ExtractSha256(checksumText);
        if (string.IsNullOrWhiteSpace(sha256))
            throw new InvalidOperationException("Sha256 dosyasi okunamadi.");

        return new LatestPackageInfo(
            NormalizeVersion(release.TagName),
            packageAsset.BrowserDownloadUrl,
            packageAsset.Name,
            sha256,
            release.HtmlUrl,
            release.Body);
    }

    private async Task DownloadFileAsync(
        string url,
        string targetPath,
        IProgress<CustomerUpdaterStatus>? progress,
        int startPercent,
        int endPercent,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("CashTrackerFabescoUpdater/1.0");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(targetPath);

        var buffer = new byte[81920];
        long written = 0;
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            written += read;

            var knownTotalBytes = totalBytes ?? 0;
            if (knownTotalBytes > 0)
            {
                var fraction = (double)written / knownTotalBytes;
                var percent = startPercent + (int)Math.Round((endPercent - startPercent) * fraction);
                Report(progress, percent, "Kurulum paketi indiriliyor...");
            }
        }
    }

    private static async Task EnsureAppClosedAsync(CancellationToken cancellationToken)
    {
        var processes = Process.GetProcessesByName("CashTracker");
        if (processes.Length == 0)
            return;

        foreach (var process in processes)
        {
            using (process)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                        process.CloseMainWindow();
                }
                catch
                {
                }
            }
        }

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = Process.GetProcessesByName("CashTracker");
            if (remaining.Length == 0)
                return;

            foreach (var process in remaining)
                process.Dispose();

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("CashTracker hala acik. Lutfen uygulamayi kapatip tekrar deneyin.");
    }

    private static async Task RunInstallerAsync(string installerPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true,
            Arguments = "/CURRENTUSER /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=desktopicon"
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            throw new InvalidOperationException("Kurulum baslatilamadi.");

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Kurulum beklenmedik sekilde sonlandi. Kod: {process.ExitCode}");
    }

    private static async Task<string> WaitForInstalledExeAsync(CancellationToken cancellationToken)
    {
        var installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "CashTracker");
        var targetPath = Path.Combine(installDir, "CashTracker.exe");

        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(targetPath))
                return targetPath;

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Kurulum tamamlandi ancak CashTracker.exe bulunamadi.");
    }

    private static string CreateDesktopShortcut(string targetPath)
    {
        if (!File.Exists(targetPath))
            throw new FileNotFoundException("Kisayol hedefi bulunamadi.", targetPath);

        var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopDir))
            throw new InvalidOperationException("Masaustu klasoru bulunamadi.");

        var shortcutPath = Path.Combine(desktopDir, $"{DesktopShortcutName}.lnk");
        var legacyShortcutPath = Path.Combine(desktopDir, $"{LegacyDesktopShortcutName}.lnk");
        TryDeleteFile(legacyShortcutPath);

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host kullanilamiyor.");

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType);
            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });

            if (shortcut is null)
                throw new InvalidOperationException("Kisayol nesnesi olusturulamadi.");

            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(targetPath)! });
            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, Array.Empty<object>());
            return shortcutPath;
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut))
                Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null && Marshal.IsComObject(shell))
                Marshal.FinalReleaseComObject(shell);
        }
    }

    private static void VerifyManifest(SignedUpdateManifest manifest)
    {
        var payload = new SignedUpdateManifestPayload
        {
            LatestVersion = manifest.LatestVersion,
            MinSupportedVersion = manifest.MinSupportedVersion,
            PackageUrl = manifest.PackageUrl,
            Sha256 = manifest.Sha256,
            ReleaseNotes = manifest.ReleaseNotes,
            IsMandatory = manifest.IsMandatory
        };

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var signatureBytes = DecodeBase64Url(manifest.Signature);

        using var rsa = RSA.Create();
        rsa.FromXmlString(DefaultUpdateManifestPublicKeyXml);
        if (!rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new InvalidOperationException("Update manifest imzasi gecersiz.");
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = (value ?? string.Empty).Trim().Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
        }

        return Convert.FromBase64String(normalized);
    }

    private static bool PackageHashMatches(string packagePath, string expectedSha256)
    {
        try
        {
            VerifyPackageHash(packagePath, expectedSha256);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void VerifyPackageHash(string packagePath, string expectedSha256)
    {
        using var stream = File.OpenRead(packagePath);
        var hashBytes = SHA256.HashData(stream);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var normalizedExpected = (expectedSha256 ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedExpected) ||
            !string.Equals(actualHash, normalizedExpected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Indirilen paket dogrulanamadi.");
        }
    }

    private async Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("CashTrackerFabescoUpdater/1.0");
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ExtractSha256(string raw)
    {
        var match = Regex.Match(raw ?? string.Empty, "[a-fA-F0-9]{64}");
        return match.Success ? match.Value.ToLowerInvariant() : string.Empty;
    }

    private static string GetFileNameFromUrl(string packageUrl)
    {
        if (Uri.TryCreate(packageUrl, UriKind.Absolute, out var uri))
            return Path.GetFileName(uri.LocalPath);

        return Path.GetFileName(packageUrl);
    }

    private static string NormalizeVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? trimmed[1..]
            : trimmed;
    }

    private static string ResolveWritableWorkRoot()
    {
        foreach (var candidate in GetWritableRootCandidates())
        {
            if (TryCreateProbe(candidate))
                return candidate;
        }

        throw new InvalidOperationException("Guncelleyici icin yazilabilir klasor bulunamadi.");
    }

    private static IEnumerable<string> GetWritableRootCandidates()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
            yield return Path.Combine(localAppData, "CashTrackerFabescoUpdater");

        yield return Path.Combine(AppContext.BaseDirectory, "AppData");
    }

    private static bool TryCreateProbe(string root)
    {
        try
        {
            Directory.CreateDirectory(root);
            var probePath = Path.Combine(root, ".write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(probePath, "ok", Encoding.ASCII);
            File.Delete(probePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static void Report(IProgress<CustomerUpdaterStatus>? progress, int percent, string message)
    {
        progress?.Report(new CustomerUpdaterStatus(percent, message));
    }
}

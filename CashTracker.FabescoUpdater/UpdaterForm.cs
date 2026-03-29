using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CashTracker.FabescoUpdater;

internal sealed class UpdaterForm : Form
{
    private const string RepoOwner = "01burark-oss";
    private const string RepoName = "CashTracker";
    private const string DesktopShortcutName = "Cashtracker Fabesco.lnk";
    private const string StartMenuShortcutName = "Cashtracker Fabesco.lnk";
    private static readonly Uri LatestReleaseUri = new($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");

    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _statusLabel;
    private readonly Label _releaseLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _installButton;
    private readonly Button _cancelButton;
    private readonly CheckBox _launchCheckBox;
    private bool _isBusy;

    public UpdaterForm()
    {
        Text = "CashTracker Fabesco Guncelleme";
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(620, 320);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 250, 252);

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CashTrackerFabescoUpdater", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        _titleLabel = new Label
        {
            AutoSize = false,
            Text = "CashTracker son surumu yukle",
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(24, 22),
            Size = new Size(560, 36)
        };

        _subtitleLabel = new Label
        {
            AutoSize = false,
            Text = "Bu arac GitHub latest release uzerindeki son surumu indirir, kurar ve masaustune Cashtracker Fabesco kisayolu olusturur.",
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 64),
            Size = new Size(572, 50)
        };

        _releaseLabel = new Label
        {
            AutoSize = false,
            Text = "Hazir",
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(24, 128),
            Size = new Size(560, 24)
        };

        _statusLabel = new Label
        {
            AutoSize = false,
            Text = "Baslat'a bastiginizda latest release kontrol edilir.",
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 160),
            Size = new Size(560, 42)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(24, 214),
            Size = new Size(572, 24),
            Style = ProgressBarStyle.Continuous
        };

        _launchCheckBox = new CheckBox
        {
            Text = "Kurulum bittiginde CashTracker'i ac",
            Checked = true,
            AutoSize = true,
            Location = new Point(24, 252),
            ForeColor = Color.FromArgb(51, 65, 85)
        };

        _installButton = new Button
        {
            Text = "Yukle",
            Location = new Point(366, 272),
            Size = new Size(110, 34),
            BackColor = Color.FromArgb(15, 118, 110),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _installButton.FlatAppearance.BorderSize = 0;
        _installButton.Click += async (_, _) => await InstallLatestAsync();

        _cancelButton = new Button
        {
            Text = "Kapat",
            Location = new Point(486, 272),
            Size = new Size(110, 34),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42),
            FlatStyle = FlatStyle.Flat
        };
        _cancelButton.Click += (_, _) =>
        {
            if (_isBusy)
            {
                _cancellation.Cancel();
            }

            Close();
        };

        Controls.AddRange(
        [
            _titleLabel,
            _subtitleLabel,
            _releaseLabel,
            _statusLabel,
            _progressBar,
            _launchCheckBox,
            _installButton,
            _cancelButton
        ]);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isBusy)
        {
            _cancellation.Cancel();
        }

        base.OnFormClosing(e);
    }

    private async Task InstallLatestAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        _installButton.Enabled = false;
        _progressBar.Value = 0;

        try
        {
            SetStatus("Son surum bilgisi aliniyor...", 5);
            var release = await GetLatestReleaseAsync(_cancellation.Token);
            _releaseLabel.Text = $"Bulunan surum: {release.TagName}";

            var asset = SelectPrimaryAsset(release);
            if (asset is null)
            {
                throw new InvalidOperationException("Latest release icinde desteklenen CashTracker asset'i bulunamadi.");
            }

            await EnsureAppClosedAsync();

            var tempRoot = Path.Combine(Path.GetTempPath(), "CashTrackerFabescoUpdater", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var assetPath = Path.Combine(tempRoot, asset.Name);

            SetStatus($"{asset.Name} indiriliyor...", 10);
            await DownloadFileAsync(asset.DownloadUrl, assetPath, _cancellation.Token);

            var hashAsset = release.Assets.FirstOrDefault(candidate =>
                candidate.Name.Equals(asset.Name + ".sha256", StringComparison.OrdinalIgnoreCase));

            if (hashAsset is not null)
            {
                SetStatus("Indirilen dosya dogrulaniyor...", 78);
                await VerifyHashAsync(assetPath, hashAsset, _cancellation.Token);
            }

            if (asset.Kind == ReleaseAssetKind.PortableExe)
            {
                SetStatus("Portable surum kuruluyor...", 86);
                InstallPortable(assetPath);
            }
            else
            {
                SetStatus("Kurulum sihirbazi calistiriliyor...", 86);
                await RunInstallerAsync(assetPath, _cancellation.Token);
            }

            CreateStartMenuShortcut(GetInstalledExecutablePath());
            SetStatus("Kurulum tamamlandi.", 100);

            if (_launchCheckBox.Checked)
            {
                LaunchInstalledApp();
            }

            MessageBox.Show(
                this,
                "CashTracker basariyla yuklendi.",
                "Kurulum Tamam",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, "Islem iptal edildi.", "Iptal", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Guncelleme Hatasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Kurulum basarisiz oldu.", 0);
        }
        finally
        {
            _isBusy = false;
            _installButton.Enabled = true;
        }
    }

    private async Task<GitHubRelease> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(LatestReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);
        return release ?? throw new InvalidOperationException("Latest release bilgisi okunamadi.");
    }

    private static ReleaseAssetSelection? SelectPrimaryAsset(GitHubRelease release)
    {
        var installer = release.Assets.FirstOrDefault(asset =>
            asset.Name.Equals("CashTracker-Setup.exe", StringComparison.OrdinalIgnoreCase));
        if (installer is not null)
        {
            return new ReleaseAssetSelection(installer.Name, installer.DownloadUrl, ReleaseAssetKind.SetupExe);
        }

        var portable = release.Assets.FirstOrDefault(asset =>
            asset.Name.Equals("CashTracker.exe", StringComparison.OrdinalIgnoreCase));
        if (portable is not null)
        {
            return new ReleaseAssetSelection(portable.Name, portable.DownloadUrl, ReleaseAssetKind.PortableExe);
        }

        return null;
    }

    private async Task EnsureAppClosedAsync()
    {
        var processes = Process.GetProcessesByName("CashTracker");
        if (processes.Length == 0)
        {
            return;
        }

        var choice = MessageBox.Show(
            this,
            "CashTracker su anda acik. Kuruluma devam etmek icin uygulama kapatilacak. Devam edilsin mi?",
            "Uygulama Acik",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (choice != DialogResult.Yes)
        {
            throw new OperationCanceledException();
        }

        foreach (var process in processes)
        {
            try
            {
                if (!process.CloseMainWindow())
                {
                    process.Kill(entireProcessTree: true);
                }
                else
                {
                    await process.WaitForExitAsync(_cancellation.Token);
                }
            }
            catch
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private async Task DownloadFileAsync(string downloadUrl, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                var percent = (int)Math.Round((double)totalRead / totalBytes.Value * 65d);
                SetStatus($"Dosya indiriliyor... %{Math.Clamp(percent, 0, 65)}", 10 + Math.Clamp(percent, 0, 65));
            }
        }
    }

    private async Task VerifyHashAsync(string assetPath, GitHubAsset hashAsset, CancellationToken cancellationToken)
    {
        var hashText = await _httpClient.GetStringAsync(hashAsset.DownloadUrl, cancellationToken);
        var expectedHash = ExtractHash(hashText);
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            throw new InvalidOperationException("SHA256 dosyasi okunamadi.");
        }

        await using var fileStream = File.OpenRead(assetPath);
        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(fileStream, cancellationToken)).ToLowerInvariant();
        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Indirilen dosyanin SHA256 dogrulamasi basarisiz oldu.");
        }
    }

    private static string ExtractHash(string hashText)
    {
        var trimmed = hashText.Trim();
        var firstToken = trimmed.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstToken?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static void InstallPortable(string assetPath)
    {
        var installDir = GetInstallDirectory();
        Directory.CreateDirectory(installDir);

        var targetPath = GetInstalledExecutablePath();
        File.Copy(assetPath, targetPath, overwrite: true);
    }

    private async Task RunInstallerAsync(string installerPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS",
            UseShellExecute = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Installer baslatilamadi.");
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Installer basarisiz oldu. Cikis kodu: {process.ExitCode}");
        }
    }

    private static void CreateStartMenuShortcut(string targetExePath)
    {
        if (!File.Exists(targetExePath))
        {
            throw new FileNotFoundException("Kurulan CashTracker.exe bulunamadi.", targetExePath);
        }

        RemoveDesktopShortcutIfPresent();

        var programsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs");
        Directory.CreateDirectory(programsDir);
        CreateShortcut(Path.Combine(programsDir, StartMenuShortcutName), targetExePath);
    }

    private static void RemoveDesktopShortcutIfPresent()
    {
        var desktopShortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            DesktopShortcutName);

        if (File.Exists(desktopShortcutPath))
        {
            File.Delete(desktopShortcutPath);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetExePath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell kullanilamiyor.");
        dynamic shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("Shortcut olusturulamadi.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetExePath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
        shortcut.Description = "CashTracker";
        shortcut.IconLocation = targetExePath;
        shortcut.Save();
    }

    private void LaunchInstalledApp()
    {
        var installedExePath = GetInstalledExecutablePath();
        if (!File.Exists(installedExePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = installedExePath,
            WorkingDirectory = Path.GetDirectoryName(installedExePath),
            UseShellExecute = true
        });
    }

    private void SetStatus(string text, int progress)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(text, progress));
            return;
        }

        _statusLabel.Text = text;
        _progressBar.Value = Math.Clamp(progress, 0, 100);
    }

    private static string GetInstallDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "CashTracker");
    }

    private static string GetInstalledExecutablePath()
    {
        return Path.Combine(GetInstallDirectory(), "CashTracker.exe");
    }

    private sealed record ReleaseAssetSelection(string Name, string DownloadUrl, ReleaseAssetKind Kind);

    private enum ReleaseAssetKind
    {
        PortableExe,
        SetupExe
    }

    internal sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    internal sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; } = string.Empty;
    }
}

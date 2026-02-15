# CashTracker

Download EXE (GitHub Release):

- Latest release page: https://github.com/01burark-oss/CashTracker/releases/latest
- Direct download (latest): https://github.com/01burark-oss/CashTracker/releases/latest/download/CashTracker.exe
- SHA256 (latest): https://github.com/01burark-oss/CashTracker/releases/latest/download/CashTracker.exe.sha256

How to use:

1. Download `CashTracker.exe` from the direct link above.
2. Move it to any folder (for example Desktop).
3. Double-click to run.

Create local release artifact:

```powershell
.\scripts\publish-release.ps1
```

Optional:

```powershell
.\scripts\publish-release.ps1 -Version 1.0.5
```

Publish a new GitHub release (automatic):

1. Update `<Version>` in `CashTracker.App/CashTracker.App.csproj`.
2. Commit and push `main`.
3. Create and push tag:

```powershell
git tag v1.0.5
git push origin v1.0.5
```

Tag push triggers `.github/workflows/release.yml` and uploads:

- `CashTracker.exe`
- `CashTracker.exe.sha256`

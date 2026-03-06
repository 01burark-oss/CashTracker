# Changelog

## Unreleased

### Added

- New `Print` flow with a dedicated preview window, template selection, date range selection, PDF export, and HTML export.
- Two distinct report templates: `Executive Summary` and `Accounting Report`.
- Extended summary range support including daily, weekly, last 30 days, current month, last 3 months, last 6 months, last 1 year, and custom date range.
- Update bootstrap flow extracted into `UpdateBootstrapService` with test coverage.

### Changed

- Print preview now refreshes automatically when template or date range changes.
- Executive summary layout was redesigned into a cleaner single-page report with centered heading, balanced table panels, and print-friendly white background styling.
- Accounting report keeps the detailed record and category breakdown structure for longer operational review.
- PDF and HTML outputs now follow the same report language and layout rules.

### Verified

- `dotnet build CashTracker.sln --no-restore`
- `dotnet test CashTracker.Tests\CashTracker.Tests.csproj --no-restore -v minimal`

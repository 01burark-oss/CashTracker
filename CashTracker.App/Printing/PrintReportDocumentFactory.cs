using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using CashTracker.App.UI;

namespace CashTracker.App.Printing
{
    internal static class PrintReportDocumentFactory
    {
        public static PrintDocument Create(PrintReportData report)
        {
            var document = new PrintDocument
            {
                DocumentName = $"{report.ReportTitle} - {report.BusinessName}",
                OriginAtMargins = false
            };

            PrintReportRenderer? renderer = null;
            document.BeginPrint += (_, __) => renderer = new PrintReportRenderer(report);
            document.PrintPage += (_, e) =>
            {
                renderer ??= new PrintReportRenderer(report);
                if (e.Graphics is null)
                {
                    e.HasMorePages = false;
                    return;
                }

                e.Graphics.PageUnit = GraphicsUnit.Point;
                e.HasMorePages = renderer.RenderPage(e.Graphics, ToPointRectangle(e.MarginBounds));
            };

            return document;
        }

        private static RectangleF ToPointRectangle(Rectangle bounds)
        {
            const float scale = 72f / 100f;
            return new RectangleF(
                bounds.Left * scale,
                bounds.Top * scale,
                bounds.Width * scale,
                bounds.Height * scale);
        }
    }

    internal sealed class PrintReportRenderer
    {
        private readonly PrintReportData _report;
        private readonly Font _headlineFont = new("Times New Roman", 16.2f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly Font _executiveMetricFont = new("Times New Roman", 15.2f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly Font _metaFont = new("Segoe UI", 8.1f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font _noteFont = new("Segoe UI", 8f, FontStyle.Italic, GraphicsUnit.Point);
        private readonly Font _headerFont = new("Segoe UI", 8.1f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly Font _cellFont = new("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font _summaryTitleFont = new("Times New Roman", 9.1f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly Font _footerFont = new("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Brush _pageBrush = new SolidBrush(Color.White);
        private readonly Brush _headerBrush = new SolidBrush(Color.FromArgb(220, 220, 220));
        private readonly Brush _accentBrush = new SolidBrush(Color.FromArgb(236, 236, 236));
        private readonly Brush _amountBrush = new SolidBrush(Color.FromArgb(245, 245, 245));
        private readonly Pen _gridPen = new(Color.FromArgb(118, 118, 118), 0.8f);
        private readonly Pen _framePen = new(Color.FromArgb(90, 90, 90), 1f);
        private readonly Pen _dividerPen = new(Color.FromArgb(75, 75, 75), 1.2f);
        private int _pageNumber;
        private int _recordIndex;
        private int _incomeCategoryIndex;
        private int _expenseCategoryIndex;
        private RenderStage _stage;

        private enum RenderStage
        {
            MainRecords,
            OverflowRecords,
            IncomeCategories,
            ExpenseCategories,
            Done
        }

        public PrintReportRenderer(PrintReportData report)
        {
            _report = report;
        }

        public bool RenderPage(Graphics graphics, RectangleF page)
        {
            _pageNumber++;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            graphics.FillRectangle(_pageBrush, page);
            graphics.DrawRectangle(_framePen, page.X, page.Y, page.Width, page.Height);

            if (_report.Template == PrintReportTemplate.ExecutiveSummary)
                return DrawExecutivePage(graphics, page);

            return _stage switch
            {
                RenderStage.MainRecords => DrawAccountingRecordsPage(graphics, page, reserveBottomSummary: true),
                RenderStage.OverflowRecords => DrawAccountingRecordsPage(graphics, page, reserveBottomSummary: false),
                RenderStage.IncomeCategories => DrawCategoryPage(graphics, page, AppLocalization.T("print.section.incomeCategories"), _report.IncomeCategories, ref _incomeCategoryIndex, RenderStage.ExpenseCategories),
                RenderStage.ExpenseCategories => DrawCategoryPage(graphics, page, AppLocalization.T("print.section.expenseCategories"), _report.ExpenseCategories, ref _expenseCategoryIndex, RenderStage.Done),
                _ => false
            };
        }

        private bool DrawExecutivePage(Graphics graphics, RectangleF page)
        {
            if (_pageNumber > 1)
                return false;

            var contentLeft = page.Left + 18f;
            var contentTop = page.Top + 18f;
            var contentWidth = page.Width - 36f;
            var contentBottom = page.Bottom - 18f;

            var headerBottom = DrawDocumentHeader(graphics, contentLeft, contentTop, contentWidth);
            var summaryTop = headerBottom + 12f;
            var summaryHeight = 90f;

            DrawExecutiveSummaryBand(graphics, new RectangleF(contentLeft, summaryTop, contentWidth, summaryHeight));

            var noteTop = summaryTop + summaryHeight + 10f;
            var noteHeight = 54f;
            DrawExecutiveNote(graphics, new RectangleF(contentLeft, noteTop, contentWidth, noteHeight));

            var lowerTop = noteTop + noteHeight + 10f;
            var lowerHeight = 162f;
            DrawExecutiveTableBand(graphics, new RectangleF(contentLeft, lowerTop, contentWidth, lowerHeight));

            var footerTop = lowerTop + lowerHeight + 8f;
            if (footerTop + 18f <= contentBottom)
            {
                DrawPreviewHint(graphics, new RectangleF(contentLeft, footerTop, contentWidth, 14f));
            }

            DrawFooter(graphics, page);
            return false;
        }

        private bool DrawAccountingRecordsPage(Graphics graphics, RectangleF page, bool reserveBottomSummary)
        {
            var contentLeft = page.Left + 18f;
            var contentTop = page.Top + 18f;
            var contentWidth = page.Width - 36f;
            var contentBottom = page.Bottom - 18f;

            var headerBottom = DrawDocumentHeader(graphics, contentLeft, contentTop, contentWidth);
            var tableTop = headerBottom + 10f;
            var summaryHeight = reserveBottomSummary ? 122f : 0f;
            var footerHeight = 14f;
            var availableBottom = contentBottom - footerHeight - summaryHeight;

            var recordArea = new RectangleF(contentLeft, tableTop, contentWidth, availableBottom - tableTop);
            DrawRecordTable(graphics, recordArea);

            if (reserveBottomSummary)
                DrawPaymentSummary(graphics, new RectangleF(contentLeft, availableBottom + 8f, contentWidth, 108f), compact: true);

            DrawFooter(graphics, page);

            if (_recordIndex < _report.Records.Count)
            {
                _stage = RenderStage.OverflowRecords;
                return true;
            }

            if (_report.IncludesDetailedSections)
            {
                _stage = RenderStage.IncomeCategories;
                return true;
            }

            _stage = RenderStage.Done;
            return false;
        }

        private float DrawDocumentHeader(Graphics graphics, float left, float top, float width)
        {
            var headerHeight = 50f;
            var title = BuildHeadline();

            DrawText(graphics, title, _headlineFont, left, top, width, 24f, ContentAlignment.MiddleCenter);
            DrawText(graphics, _report.BusinessName, _summaryTitleFont, left, top + 22f, width, 12f, ContentAlignment.MiddleCenter);

            var metaLine = string.Join("   |   ",
                $"{AppLocalization.T("print.meta.range")}: {_report.RangeDisplay}",
                $"{AppLocalization.T("print.meta.generatedAt")}: {_report.GeneratedAt:dd.MM.yyyy HH:mm}");
            DrawText(graphics, metaLine, _metaFont, left, top + 34f, width, 12f, ContentAlignment.MiddleCenter);

            var lineY = top + headerHeight + 6f;
            graphics.DrawLine(_dividerPen, left, lineY, left + width, lineY);
            return lineY + 4f;
        }

        private void DrawExecutiveSummaryBand(Graphics graphics, RectangleF area)
        {
            DrawSectionHeading(graphics, AppLocalization.T("print.section.summary"), area.Left, area.Top, area.Width);

            var cardsTop = area.Top + 20f;
            var gap = 8f;
            var cardWidth = (area.Width - (gap * 2f)) / 3f;
            var cardHeight = 54f;
            var net = _report.Summary.IncomeTotal - _report.Summary.ExpenseTotal;

            DrawMetricCard(graphics, new RectangleF(area.Left, cardsTop, cardWidth, cardHeight), AppLocalization.T("print.summary.totalIncome"), _report.Summary.IncomeTotal);
            DrawMetricCard(graphics, new RectangleF(area.Left + cardWidth + gap, cardsTop, cardWidth, cardHeight), AppLocalization.T("print.summary.totalExpense"), _report.Summary.ExpenseTotal);
            DrawMetricCard(graphics, new RectangleF(area.Left + (cardWidth + gap) * 2f, cardsTop, cardWidth, cardHeight), AppLocalization.T("main.daily.net"), net);

            var countsLine = $"{AppLocalization.F("print.metric.count", _report.VisibleRecordCount)}   |   {AppLocalization.F("print.metric.totalCount", _report.TotalRecordCount)}";
            DrawText(graphics, countsLine, _metaFont, area.Left, cardsTop + cardHeight + 6f, area.Width, 12f, ContentAlignment.MiddleCenter);
        }

        private void DrawMetricCard(Graphics graphics, RectangleF area, string label, decimal amount)
        {
            graphics.FillRectangle(_accentBrush, area);
            graphics.DrawRectangle(_framePen, area.X, area.Y, area.Width, area.Height);
            DrawText(graphics, label, _headerFont, area.Left + 6f, area.Top + 4f, area.Width - 12f, 12f, ContentAlignment.MiddleCenter);
            DrawText(graphics, amount.ToString("n2", AppLocalization.CurrentCulture), _executiveMetricFont, area.Left + 6f, area.Top + 20f, area.Width - 12f, 24f, ContentAlignment.MiddleCenter);
        }

        private void DrawExecutiveNote(Graphics graphics, RectangleF area)
        {
            DrawSectionHeading(graphics, AppLocalization.T("print.section.note"), area.Left, area.Top, area.Width);
            var panel = new RectangleF(area.Left, area.Top + 18f, area.Width, area.Height - 18f);
            graphics.DrawRectangle(_framePen, panel.X, panel.Y, panel.Width, panel.Height);
            DrawText(
                graphics,
                string.IsNullOrWhiteSpace(_report.Note) ? AppLocalization.T("print.note.placeholder") : _report.Note,
                _noteFont,
                panel.Left + 8f,
                panel.Top + 4f,
                panel.Width - 16f,
                panel.Height - 8f,
                ContentAlignment.TopLeft);
        }

        private void DrawExecutiveTableBand(Graphics graphics, RectangleF area)
        {
            var gap = 10f;
            var columnWidth = (area.Width - (gap * 2f)) / 3f;
            DrawPaymentSummary(graphics, new RectangleF(area.Left, area.Top, columnWidth, area.Height), compact: false, fixedBodyRows: 5);
            DrawCompactCategoryPanel(graphics, new RectangleF(area.Left + columnWidth + gap, area.Top, columnWidth, area.Height), AppLocalization.T("print.section.incomeCategories"), _report.IncomeCategories, fixedBodyRows: 5);
            DrawCompactCategoryPanel(graphics, new RectangleF(area.Left + ((columnWidth + gap) * 2f), area.Top, columnWidth, area.Height), AppLocalization.T("print.section.expenseCategories"), _report.ExpenseCategories, fixedBodyRows: 5);
        }

        private void DrawCompactCategoryPanel(Graphics graphics, RectangleF area, string title, System.Collections.Generic.IReadOnlyList<PrintCategorySummary> rows, int fixedBodyRows)
        {
            DrawSectionHeading(graphics, title, area.Left, area.Top, area.Width);
            var tableTop = area.Top + 18f;
            var columns = new[]
            {
                area.Width * 0.54f,
                area.Width * 0.16f,
                area.Width * 0.30f
            };
            const float headerHeight = 18f;
            const float rowHeight = 17f;
            var items = rows.Count == 0
                ? [new PrintCategorySummary { CategoryName = "-", Count = 0, Total = 0m }]
                : rows;
            var maxRows = Math.Min(fixedBodyRows, items.Count);

            DrawShadedHeader(graphics, area.Left, tableTop, area.Width, headerHeight);
            DrawGridHeader(
                graphics,
                area.Left,
                tableTop,
                area.Width,
                headerHeight,
                columns,
                [
                    AppLocalization.T("common.category"),
                    AppLocalization.T("print.column.count"),
                    AppLocalization.T("common.amount")
                ]);

            var y = tableTop + headerHeight;
            for (var i = 0; i < maxRows; i++)
            {
                var row = items[i];
                DrawSimpleSummaryRow(
                    graphics,
                    area.Left,
                    y,
                    area.Width,
                    rowHeight,
                    columns,
                    row.CategoryName,
                    row.Count.ToString(AppLocalization.CurrentCulture),
                    row.Total.ToString("n2", AppLocalization.CurrentCulture));
                y += rowHeight;
            }

            for (var i = maxRows; i < fixedBodyRows; i++)
            {
                DrawSimpleSummaryRow(
                    graphics,
                    area.Left,
                    y,
                    area.Width,
                    rowHeight,
                    columns,
                    string.Empty,
                    string.Empty,
                    string.Empty);
                y += rowHeight;
            }

            graphics.DrawRectangle(_framePen, area.Left, tableTop, area.Width, headerHeight + (fixedBodyRows * rowHeight));
        }

        private void DrawPreviewHint(Graphics graphics, RectangleF area)
        {
            if (!_report.IsPreview || !_report.RecordLimit.HasValue || _report.TotalRecordCount <= _report.VisibleRecordCount)
                return;

            DrawText(
                graphics,
                AppLocalization.F("print.preview.sampleNote", _report.VisibleRecordCount, _report.TotalRecordCount),
                _metaFont,
                area.Left,
                area.Top,
                area.Width,
                area.Height,
                ContentAlignment.MiddleCenter);
        }

        private void DrawRecordTable(Graphics graphics, RectangleF area)
        {
            const float headerHeight = 24f;
            const float rowHeight = 20f;
            const float totalHeight = 24f;

            var columns = new[]
            {
                area.Width * 0.16f,
                area.Width * 0.30f,
                area.Width * 0.20f,
                area.Width * 0.12f,
                area.Width * 0.12f,
                area.Width * 0.10f
            };

            DrawShadedHeader(graphics, area.Left, area.Top, area.Width, headerHeight);
            DrawGridHeader(
                graphics,
                area.Left,
                area.Top,
                area.Width,
                headerHeight,
                columns,
                [
                    AppLocalization.T("common.date"),
                    AppLocalization.T("common.description"),
                    AppLocalization.T("common.category"),
                    AppLocalization.T("tip.income"),
                    AppLocalization.T("tip.expense"),
                    AppLocalization.T("common.method")
                ]);

            var y = area.Top + headerHeight;
            while (_recordIndex < _report.Records.Count && y + rowHeight + totalHeight <= area.Bottom)
            {
                var row = _report.Records[_recordIndex];
                DrawRecordRow(graphics, area.Left, y, rowHeight, columns, row);
                y += rowHeight;
                _recordIndex++;
            }

            DrawTotalRow(graphics, area.Left, area.Bottom - totalHeight, area.Width, totalHeight, columns);
            graphics.DrawRectangle(_framePen, area.Left, area.Top, area.Width, area.Height);
        }

        private void DrawRecordRow(Graphics graphics, float x, float y, float height, float[] columns, PrintRecordRow row)
        {
            var currentX = x;
            for (var i = 0; i < columns.Length; i++)
            {
                var cellWidth = columns[i];
                if (i is 3 or 4)
                    graphics.FillRectangle(_amountBrush, currentX, y, cellWidth, height);

                graphics.DrawRectangle(_gridPen, currentX, y, cellWidth, height);
                currentX += cellWidth;
            }

            currentX = x;
            DrawText(graphics, row.Date.ToString("dd.MM.yyyy", AppLocalization.CurrentCulture), _cellFont, currentX + 4f, y + 1f, columns[0] - 8f, height - 2f, ContentAlignment.MiddleLeft);
            currentX += columns[0];
            DrawText(graphics, row.Description, _cellFont, currentX + 4f, y + 1f, columns[1] - 8f, height - 2f, ContentAlignment.MiddleLeft);
            currentX += columns[1];
            DrawText(graphics, row.CategoryDisplay, _cellFont, currentX + 4f, y + 1f, columns[2] - 8f, height - 2f, ContentAlignment.MiddleLeft);
            currentX += columns[2];
            DrawText(graphics, row.IsIncome ? row.Amount.ToString("n2", AppLocalization.CurrentCulture) : string.Empty, _cellFont, currentX + 4f, y + 1f, columns[3] - 8f, height - 2f, ContentAlignment.MiddleRight);
            currentX += columns[3];
            DrawText(graphics, row.IsIncome ? string.Empty : row.Amount.ToString("n2", AppLocalization.CurrentCulture), _cellFont, currentX + 4f, y + 1f, columns[4] - 8f, height - 2f, ContentAlignment.MiddleRight);
            currentX += columns[4];
            DrawText(graphics, row.MethodDisplay, _cellFont, currentX + 4f, y + 1f, columns[5] - 8f, height - 2f, ContentAlignment.MiddleCenter);
        }

        private void DrawTotalRow(Graphics graphics, float x, float y, float width, float height, float[] columns)
        {
            graphics.FillRectangle(_headerBrush, x, y, width, height);
            graphics.DrawRectangle(_framePen, x, y, width, height);

            var currentX = x;
            var labelWidth = columns[0] + columns[1] + columns[2];
            graphics.DrawRectangle(_gridPen, currentX, y, labelWidth, height);
            DrawText(graphics, AppLocalization.T("print.total.label"), _headerFont, currentX + 4f, y + 2f, labelWidth - 8f, height - 4f, ContentAlignment.MiddleCenter);
            currentX += labelWidth;

            DrawAmountTotalCell(graphics, currentX, y, columns[3], height, _report.Summary.IncomeTotal);
            currentX += columns[3];
            DrawAmountTotalCell(graphics, currentX, y, columns[4], height, _report.Summary.ExpenseTotal);
            currentX += columns[4];
            graphics.DrawRectangle(_gridPen, currentX, y, columns[5], height);
        }

        private void DrawPaymentSummary(Graphics graphics, RectangleF area, bool compact, int? fixedBodyRows = null)
        {
            DrawSectionHeading(graphics, AppLocalization.T("print.section.paymentMethodsCompact"), area.Left, area.Top, area.Width);
            var tableTop = area.Top + 18f;
            var columns = new[]
            {
                area.Width * 0.46f,
                area.Width * 0.27f,
                area.Width * 0.27f
            };
            var headerHeight = compact ? 21f : 19f;
            var rowHeight = compact ? 19f : 17f;
            var totalWidth = area.Width;

            DrawShadedHeader(graphics, area.Left, tableTop, totalWidth, headerHeight);
            DrawGridHeader(
                graphics,
                area.Left,
                tableTop,
                totalWidth,
                headerHeight,
                columns,
                [
                    AppLocalization.T("common.method"),
                    AppLocalization.T("print.summary.totalIncome"),
                    AppLocalization.T("print.summary.totalExpense")
                ]);

            var y = tableTop + headerHeight;
            var renderedRows = 0;
            var allowedRows = fixedBodyRows.HasValue ? Math.Max(0, fixedBodyRows.Value - 1) : _report.PaymentMethods.Count;

            foreach (var row in _report.PaymentMethods.Take(allowedRows))
            {
                DrawSimpleSummaryRow(
                    graphics,
                    area.Left,
                    y,
                    totalWidth,
                    rowHeight,
                    columns,
                    row.DisplayName,
                    row.Income.ToString("n2", AppLocalization.CurrentCulture),
                    row.Expense.ToString("n2", AppLocalization.CurrentCulture));
                y += rowHeight;
                renderedRows++;
            }

            if (fixedBodyRows.HasValue)
            {
                for (var i = renderedRows; i < allowedRows; i++)
                {
                    DrawSimpleSummaryRow(
                        graphics,
                        area.Left,
                        y,
                        totalWidth,
                        rowHeight,
                        columns,
                        string.Empty,
                        string.Empty,
                        string.Empty);
                    y += rowHeight;
                }
            }

            graphics.FillRectangle(_headerBrush, area.Left, y, totalWidth, rowHeight);
            DrawSimpleSummaryRow(
                graphics,
                area.Left,
                y,
                totalWidth,
                rowHeight,
                columns,
                AppLocalization.T("print.total.general"),
                _report.Summary.IncomeTotal.ToString("n2", AppLocalization.CurrentCulture),
                _report.Summary.ExpenseTotal.ToString("n2", AppLocalization.CurrentCulture),
                bold: true);
            graphics.DrawRectangle(_framePen, area.Left, tableTop, totalWidth, headerHeight + ((fixedBodyRows ?? (_report.PaymentMethods.Count + 1)) * rowHeight));
        }

        private bool DrawCategoryPage(
            Graphics graphics,
            RectangleF page,
            string title,
            System.Collections.Generic.IReadOnlyList<PrintCategorySummary> rows,
            ref int rowIndex,
            RenderStage nextStage)
        {
            var left = page.Left + 22f;
            var top = page.Top + 24f;
            var width = page.Width - 44f;
            var bottom = page.Bottom - 28f;

            DrawText(graphics, title, _headlineFont, left, top, width, 26f, ContentAlignment.MiddleCenter);
            var tableTop = top + 40f;
            var columns = new[]
            {
                width * 0.58f,
                width * 0.18f,
                width * 0.24f
            };
            const float headerHeight = 22f;
            const float rowHeight = 20f;

            DrawShadedHeader(graphics, left, tableTop, width, headerHeight);
            DrawGridHeader(
                graphics,
                left,
                tableTop,
                width,
                headerHeight,
                columns,
                [
                    AppLocalization.T("common.category"),
                    AppLocalization.T("print.column.count"),
                    AppLocalization.T("common.amount")
                ]);

            var y = tableTop + headerHeight;
            var items = rows.Count == 0
                ? [new PrintCategorySummary { CategoryName = "-", Count = 0, Total = 0m }]
                : rows;

            while (rowIndex < items.Count && y + rowHeight + 16f <= bottom)
            {
                var row = items[rowIndex];
                DrawSimpleSummaryRow(
                    graphics,
                    left,
                    y,
                    width,
                    rowHeight,
                    columns,
                    row.CategoryName,
                    row.Count.ToString(AppLocalization.CurrentCulture),
                    row.Total.ToString("n2", AppLocalization.CurrentCulture));
                y += rowHeight;
                rowIndex++;
            }

            graphics.DrawRectangle(_framePen, left, tableTop, width, y - tableTop);
            DrawFooter(graphics, page);

            if (rowIndex < items.Count)
                return true;

            _stage = nextStage;
            return nextStage != RenderStage.Done;
        }

        private void DrawSimpleSummaryRow(
            Graphics graphics,
            float x,
            float y,
            float width,
            float height,
            float[] columns,
            string first,
            string second,
            string third,
            bool bold = false)
        {
            var currentX = x;
            for (var i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                    graphics.FillRectangle(_amountBrush, currentX, y, columns[i], height);

                graphics.DrawRectangle(_gridPen, currentX, y, columns[i], height);
                currentX += columns[i];
            }

            var font = bold ? _headerFont : _cellFont;
            currentX = x;
            DrawText(graphics, first, font, currentX + 4f, y + 1f, columns[0] - 8f, height - 2f, ContentAlignment.MiddleLeft);
            currentX += columns[0];
            DrawText(graphics, second, font, currentX + 4f, y + 1f, columns[1] - 8f, height - 2f, ContentAlignment.MiddleRight);
            currentX += columns[1];
            DrawText(graphics, third, font, currentX + 4f, y + 1f, columns[2] - 8f, height - 2f, ContentAlignment.MiddleRight);
        }

        private void DrawAmountTotalCell(Graphics graphics, float x, float y, float width, float height, decimal amount)
        {
            graphics.FillRectangle(_accentBrush, x, y, width, height);
            graphics.DrawRectangle(_gridPen, x, y, width, height);
            DrawText(graphics, amount.ToString("n2", AppLocalization.CurrentCulture), _headerFont, x + 4f, y + 2f, width - 8f, height - 4f, ContentAlignment.MiddleRight);
        }

        private void DrawGridHeader(Graphics graphics, float x, float y, float width, float height, float[] columns, string[] values)
        {
            var currentX = x;
            for (var i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                    graphics.DrawLine(_gridPen, currentX, y, currentX, y + height);

                DrawText(graphics, values[i], _headerFont, currentX + 4f, y + 2f, columns[i] - 8f, height - 4f, ContentAlignment.MiddleCenter);
                currentX += columns[i];
            }

            graphics.DrawRectangle(_framePen, x, y, width, height);
        }

        private void DrawSectionHeading(Graphics graphics, string text, float x, float y, float width)
        {
            DrawText(graphics, text, _summaryTitleFont, x, y, width, 14f, ContentAlignment.MiddleCenter);
        }

        private void DrawShadedHeader(Graphics graphics, float x, float y, float width, float height)
        {
            graphics.FillRectangle(_headerBrush, x, y, width, height);
        }

        private void DrawFooter(Graphics graphics, RectangleF page)
        {
            DrawText(
                graphics,
                AppLocalization.F("print.footer.page", _pageNumber),
                _footerFont,
                page.Left + 18f,
                page.Bottom - 14f,
                page.Width - 36f,
                10f,
                ContentAlignment.MiddleRight);
        }

        private string BuildHeadline()
        {
            var from = _report.Summary.From == default ? _report.GeneratedAt.Date : _report.Summary.From.Date;
            var to = _report.Summary.To == default ? _report.GeneratedAt.Date : _report.Summary.To.Date;
            var suffix = AppLocalization.T("print.headline.suffix");

            if (from.Year == to.Year && from.Month == to.Month)
            {
                var monthName = AppLocalization.CurrentCulture.DateTimeFormat.GetMonthName(from.Month).ToUpper(AppLocalization.CurrentCulture);
                return $"{monthName} {from.Year} {suffix}";
            }

            return $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy} {suffix}";
        }

        private static void DrawText(Graphics graphics, string text, Font font, float x, float y, float width, float height, ContentAlignment alignment)
        {
            using var brush = new SolidBrush(Color.Black);
            using var format = new StringFormat
            {
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.LineLimit
            };

            format.Alignment = alignment switch
            {
                ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter => StringAlignment.Center,
                ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            format.LineAlignment = alignment switch
            {
                ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => StringAlignment.Near,
                ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => StringAlignment.Far,
                _ => StringAlignment.Center
            };

            graphics.DrawString(text, font, brush, new RectangleF(x, y, width, height), format);
        }
    }
}

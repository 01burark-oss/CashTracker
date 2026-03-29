using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Controls
{
    internal sealed class PinCodeInputControl : Control
    {
        private string _value = string.Empty;

        public PinCodeInputControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);

            TabStop = true;
            BackColor = Color.White;
            ForeColor = BrandTheme.Heading;
            Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold);
            Size = new Size(244, 58);
            Cursor = Cursors.IBeam;
        }

        public int PinLength { get; set; } = 4;

        public bool UsePasswordMask { get; set; } = true;

        [AllowNull]
        public override string Text
        {
            get => _value;
            set => SetValue(value ?? string.Empty);
        }

        public void ClearPin()
        {
            Text = string.Empty;
        }

        public void AppendDigit(char digit)
        {
            if (!char.IsDigit(digit) || _value.Length >= PinLength)
                return;

            SetValue(_value + digit);
        }

        public void RemoveLastDigit()
        {
            if (_value.Length == 0)
                return;

            SetValue(_value[..^1]);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData is Keys.Back or Keys.Delete or Keys.Left or Keys.Right
                ? true
                : base.IsInputKey(keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (char.IsControl(e.KeyChar))
            {
                if (e.KeyChar == '\b' && _value.Length > 0)
                {
                    _value = _value[..^1];
                    OnTextChanged(EventArgs.Empty);
                    Invalidate();
                }

                e.Handled = true;
                return;
            }

            if (!char.IsDigit(e.KeyChar) || _value.Length >= PinLength)
            {
                e.Handled = true;
                return;
            }

            _value += e.KeyChar;
            OnTextChanged(EventArgs.Empty);
            Invalidate();
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control && e.KeyCode == Keys.V)
            {
                try
                {
                    if (Clipboard.ContainsText())
                        AppendDigits(Clipboard.GetText());
                }
                catch
                {
                }

                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode is Keys.Delete or Keys.Back)
            {
                if (_value.Length > 0)
                {
                    _value = _value[..^1];
                    OnTextChanged(EventArgs.Empty);
                    Invalidate();
                }

                e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

            var boxCount = Math.Max(PinLength, 1);
            const int gap = 12;
            const int outerPadding = 4;
            var totalGap = gap * (boxCount - 1);
            var availableWidth = Math.Max((ClientSize.Width - (outerPadding * 2)) - totalGap, boxCount * 40);
            var boxWidth = Math.Max(40, availableWidth / boxCount);
            var totalWidth = (boxWidth * boxCount) + totalGap;
            var boxHeight = Math.Max(ClientSize.Height - 6, 42);
            var startX = outerPadding + Math.Max(((ClientSize.Width - (outerPadding * 2)) - totalWidth) / 2, 0);
            var y = Math.Max((ClientSize.Height - boxHeight) / 2, 2);

            for (var i = 0; i < boxCount; i++)
            {
                var rect = new Rectangle(startX + (i * (boxWidth + gap)), y, boxWidth, boxHeight);
                var isFilled = i < _value.Length;
                var isActive = Focused && i == Math.Min(_value.Length, boxCount - 1);
                DrawSlot(e.Graphics, rect, isFilled, isActive);

                var content = isFilled
                    ? (UsePasswordMask ? "\u2022" : _value[i].ToString())
                    : string.Empty;

                if (!string.IsNullOrEmpty(content))
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        content,
                        Font,
                        rect,
                        ForeColor,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.NoPadding);
                }
            }
        }

        private void SetValue(string? raw)
        {
            var next = new string((raw ?? string.Empty).Where(char.IsDigit).Take(PinLength).ToArray());
            if (string.Equals(_value, next, StringComparison.Ordinal))
                return;

            _value = next;
            OnTextChanged(EventArgs.Empty);
            Invalidate();
        }

        private void AppendDigits(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;

            var next = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(next))
                return;

            SetValue(_value + next);
        }

        private static void DrawSlot(Graphics graphics, Rectangle rect, bool isFilled, bool isActive)
        {
            var backColor = isFilled
                ? Color.FromArgb(246, 250, 255)
                : Color.White;
            var borderColor = isActive
                ? BrandTheme.Teal
                : Color.FromArgb(196, 207, 221);

            using var path = CreateRoundedPath(rect, 10);
            using var backBrush = new SolidBrush(backColor);
            using var borderPen = new Pen(borderColor, isActive ? 2f : 1.2f);
            graphics.FillPath(backBrush, path);
            graphics.DrawPath(borderPen, path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

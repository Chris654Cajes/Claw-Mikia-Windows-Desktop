using System;
using System.Drawing;
using System.Windows.Forms;
using MusicVault.Theme;

namespace MusicVault.Controls
{
    /// <summary>
    /// Custom button with neon styling and glow effects
    /// </summary>
    public class NeonButton : Button
    {
        private bool _isHovered;
        private Color _neonColor;
        private Color _backgroundColor;

        public Color NeonColor
        {
            get => _neonColor;
            set
            {
                _neonColor = value;
                Invalidate();
            }
        }

        public NeonButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | 
                    ControlStyles.ResizeRedraw, true);
            
            _neonColor = ThemeColors.NeonCyan;
            _backgroundColor = ThemeColors.BgSurface;
            BackColor = _backgroundColor;
            ForeColor = ThemeColors.TextPrimary;
            Font = new Font("Segoe UI", FontSizes.BodySize, FontStyle.Bold);
            Height = Sizes.ButtonHeight;
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            _backgroundColor = ThemeColors.BgHover;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _backgroundColor = ThemeColors.BgSurface;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(_backgroundColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw border with neon color
            using (var pen = new Pen(_neonColor, 2))
            {
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }

            // Draw glow effect when hovered
            if (_isHovered)
            {
                using (var glowPen = new Pen(_neonColor, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Solid })
                {
                    var glowRect = new Rectangle(1, 1, Width - 3, Height - 3);
                    e.Graphics.DrawRectangle(glowPen, glowRect);
                }
            }

            // Draw text centered
            var textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using (var brush = new SolidBrush(ForeColor))
            {
                e.Graphics.DrawString(Text, Font, brush, ClientRectangle, textFormat);
            }
        }
    }
}

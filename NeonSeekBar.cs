using System;
using System.Drawing;
using System.Windows.Forms;
using MusicVault.Theme;

namespace MusicVault.Controls
{
    /// <summary>
    /// Custom seekbar for music playback with neon pink progress
    /// </summary>
    public class NeonSeekBar : Control
    {
        private long _currentPosition;
        private long _duration;
        private bool _isDragging;

        public event EventHandler? PositionChanged;

        public long CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (value >= 0 && value <= _duration)
                {
                    _currentPosition = value;
                    Invalidate();
                }
            }
        }

        public long Duration
        {
            get => _duration;
            set
            {
                if (value >= 0)
                {
                    _duration = value;
                    Invalidate();
                }
            }
        }

        public NeonSeekBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | 
                    ControlStyles.ResizeRedraw, true);
            
            BackColor = ThemeColors.BgSurface;
            Height = 6;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isDragging = true;
            UpdatePosition(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                UpdatePosition(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        private void UpdatePosition(int x)
        {
            if (Width <= 0 || _duration <= 0) return;

            double percentage = Math.Max(0, Math.Min(1, (double)x / Width));
            _currentPosition = (long)(percentage * _duration);

            Invalidate();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int barY = Height / 2 - 2;
            int barHeight = 4;

            // Draw background bar
            using (var brush = new SolidBrush(ThemeColors.SeekbarBg))
            {
                e.Graphics.FillRectangle(brush, 0, barY, Width, barHeight);
            }

            // Draw progress bar
            if (_duration > 0)
            {
                double percentage = (double)_currentPosition / _duration;
                int progressWidth = (int)(Width * percentage);

                using (var brush = new SolidBrush(ThemeColors.NeonPink))
                {
                    e.Graphics.FillRectangle(brush, 0, barY, progressWidth, barHeight);
                }

                // Draw progress handle
                int handleX = progressWidth - 6;
                using (var brush = new SolidBrush(ThemeColors.NeonPink))
                {
                    e.Graphics.FillEllipse(brush, handleX, Height / 2 - 6, 12, 12);
                }
            }
        }
    }
}

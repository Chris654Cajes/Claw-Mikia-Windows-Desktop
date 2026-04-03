using System;
using System.Drawing;
using System.Windows.Forms;
using MusicVault.Controls;
using MusicVault.Models;
using MusicVault.Theme;

namespace MusicVault.Controls
{
    /// <summary>
    /// Collapsible mini player panel with playback controls and seekbar
    /// </summary>
    public class MiniPlayerPanel : Panel
    {
        private bool _isExpanded = true;
        private Song? _currentSong;
        private bool _isPlaying;

        // Controls
        private Label _titleLabel = null!;
        private Label _artistLabel = null!;
        private Label _timeLabel = null!;
        private NeonButton _playButton = null!;
        private NeonButton _prevButton = null!;
        private NeonButton _nextButton = null!;
        private Button _toggleButton = null!;
        private NeonSeekBar _seekBar = null!;

        public event EventHandler? PlayPauseClicked;
        public event EventHandler? PreviousClicked;
        public event EventHandler? NextClicked;
        public event EventHandler<long>? SeekRequested;

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                _playButton.Text = value ? "⏸" : "▶";
            }
        }

        public MiniPlayerPanel()
        {
            BackColor = ThemeColors.BgSurface;
            BorderStyle = BorderStyle.FixedSingle;
            Height = Sizes.MiniPlayerHeight;
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Seekbar
            _seekBar = new NeonSeekBar
            {
                Top = Spacing.Small,
                Left = Spacing.Small,
                Width = Width - Spacing.Small * 2,
                Height = 4,
                Duration = 0,
                BackColor = ThemeColors.BgSurface
            };
            _seekBar.PositionChanged += (s, e) => SeekRequested?.Invoke(this, _seekBar.CurrentPosition);
            Controls.Add(_seekBar);

            // Title label
            _titleLabel = new Label
            {
                Top = Spacing.Medium + 8,
                Left = Spacing.Small,
                Width = Width - Spacing.Small * 2,
                Height = 20,
                Text = "No song selected",
                ForeColor = ThemeColors.TextPrimary,
                Font = new Font("Segoe UI", FontSizes.HeadingSize, FontStyle.Bold),
                AutoEllipsis = true,
                BackColor = ThemeColors.BgSurface
            };
            Controls.Add(_titleLabel);

            // Artist label
            _artistLabel = new Label
            {
                Top = _titleLabel.Top + 22,
                Left = Spacing.Small,
                Width = Width - Spacing.Small * 2,
                Height = 16,
                Text = "Unknown Artist",
                ForeColor = ThemeColors.TextSecondary,
                Font = new Font("Segoe UI", FontSizes.BodySize),
                AutoEllipsis = true,
                BackColor = ThemeColors.BgSurface
            };
            Controls.Add(_artistLabel);

            // Time label
            _timeLabel = new Label
            {
                Top = _artistLabel.Top + 18,
                Left = Spacing.Small,
                Width = Width - Spacing.Small * 2,
                Height = 14,
                Text = "0:00 / 0:00",
                ForeColor = ThemeColors.NeonCyan,
                Font = new Font("Segoe UI", FontSizes.SmallSize),
                BackColor = ThemeColors.BgSurface
            };
            Controls.Add(_timeLabel);

            // Playback buttons
            int buttonWidth = 48;
            int buttonStartX = (Width - (buttonWidth * 3 + Spacing.Medium * 2)) / 2;

            _prevButton = new NeonButton
            {
                Top = _timeLabel.Top + 20,
                Left = buttonStartX,
                Width = buttonWidth,
                Height = Sizes.ButtonHeight,
                Text = "⏮",
                NeonColor = ThemeColors.NeonPink
            };
            _prevButton.Click += (s, e) => PreviousClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(_prevButton);

            _playButton = new NeonButton
            {
                Top = _prevButton.Top,
                Left = buttonStartX + buttonWidth + Spacing.Medium,
                Width = buttonWidth,
                Height = Sizes.ButtonHeight,
                Text = "▶",
                NeonColor = ThemeColors.NeonPink
            };
            _playButton.Click += (s, e) =>
            {
                _isPlaying = !_isPlaying;
                _playButton.Text = _isPlaying ? "⏸" : "▶";
                PlayPauseClicked?.Invoke(this, EventArgs.Empty);
            };
            Controls.Add(_playButton);

            _nextButton = new NeonButton
            {
                Top = _prevButton.Top,
                Left = buttonStartX + (buttonWidth + Spacing.Medium) * 2,
                Width = buttonWidth,
                Height = Sizes.ButtonHeight,
                Text = "⏭",
                NeonColor = ThemeColors.NeonPink
            };
            _nextButton.Click += (s, e) => NextClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(_nextButton);

            // Toggle button
            _toggleButton = new Button
            {
                Top = Spacing.Small,
                Left = Width - Spacing.Medium - 24,
                Width = 24,
                Height = 24,
                Text = "▼",
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeColors.BgSurface,
                ForeColor = ThemeColors.NeonPink,
                Font = new Font("Segoe UI", FontSizes.BodySize),
                Cursor = Cursors.Hand
            };
            _toggleButton.FlatAppearance.BorderColor = ThemeColors.NeonPink;
            _toggleButton.FlatAppearance.BorderSize = 1;
            _toggleButton.Click += (s, e) => ToggleExpanded();
            Controls.Add(_toggleButton);
        }

        public void SetSong(Song? song, long currentPosition = 0)
        {
            _currentSong = song;
            if (song != null)
            {
                _titleLabel.Text = song.Title;
                _artistLabel.Text = song.Artist;
                _seekBar.Duration = song.Duration;
                _seekBar.CurrentPosition = currentPosition;
                UpdateTimeLabel(currentPosition, song.Duration);
            }
            else
            {
                _titleLabel.Text = "No song selected";
                _artistLabel.Text = "Unknown Artist";
                _timeLabel.Text = "0:00 / 0:00";
                _seekBar.Duration = 0;
                _seekBar.CurrentPosition = 0;
            }
        }

        public void UpdateProgress(long currentMs)
        {
            if (_currentSong != null)
            {
                _seekBar.CurrentPosition = currentMs;
                UpdateTimeLabel(currentMs, _currentSong.Duration);
            }
        }

        private void UpdateTimeLabel(long currentMs, long durationMs)
        {
            string current = FormatTime(currentMs);
            string total = FormatTime(durationMs);
            _timeLabel.Text = $"{current} / {total}";
        }

        private string FormatTime(long milliseconds)
        {
            var ts = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            Height = _isExpanded ? Sizes.MiniPlayerHeight : 40;
            _toggleButton.Text = _isExpanded ? "▼" : "▶";

            foreach (Control control in Controls)
            {
                if (control != _toggleButton)
                {
                    control.Visible = _isExpanded;
                }
            }

            Parent?.PerformLayout();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MusicVault.Models;
using MusicVault.Services;

namespace MusicVault
{
    public class MainForm : Form
    {
        private List<Song> songs = new();
        private Song? current;
        private AudioService audio = new();

        // Neon Colors
        private static readonly Color BG = Color.FromArgb(20, 20, 20);           // #141414
        private static readonly Color ACCENT_PINK = Color.FromArgb(255, 0, 115);   // #ff0073
        private static readonly Color ACCENT_PURPLE = Color.FromArgb(174, 0, 255); // #ae00ff
        private static readonly Color ACCENT_BLUE = Color.FromArgb(13, 0, 255);    // #0d00ff
        private static readonly Color ACCENT_CYAN = Color.FromArgb(0, 208, 255);   // #00d0ff
        private static readonly Color ACCENT_GREEN = Color.FromArgb(38, 255, 0);   // #26ff00
        private static readonly Color ACCENT_YELLOW = Color.FromArgb(255, 230, 0); // #ffe600
        private static readonly Color ACCENT_ORANGE = Color.FromArgb(255, 106, 0); // #ff6a00
        private static readonly Color ACCENT_RED = Color.FromArgb(255, 0, 0);      // #ff0000

        private CustomListBox songList;
        private Label songTitle, albumLabel, timeLabel, statusLabel;
        private PictureBox albumArt;

        private NeonButton playPauseBtn, loopBtn, addFolderBtn, resetBtn;
        private CustomTrackBar seekBar, volumeBar, pitchBar;

        private Timer timer;

        private enum PlayMode
        {
            Normal,
            RepeatOne,
            RepeatAll,
            NoAutoNext
        }

        private PlayMode mode = PlayMode.Normal;

        public MainForm()
        {
            Text = "MusicVault PRO";
            WindowState = FormWindowState.Maximized;
            BackColor = BG;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;

            BuildUI();
            LoadSavedSongs();

            timer = new Timer { Interval = 300 };
            timer.Tick += UpdateUI;
            timer.Start();

            audio.PlaybackStopped += HandlePlaybackFinished;
        }

        private void BuildUI()
        {
            // ROOT LAYOUT
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            root.BackColor = BG;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Controls.Add(root);

            // SIDEBAR
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG
            };

            sidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(ACCENT_CYAN, 2))
                {
                    e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
                }
            };

            var sidebarFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                BackColor = BG,
                Padding = new Padding(15),
                AutoScroll = true
            };

            var logoLabel = new Label
            {
                Text = "🎵 VAULT",
                Font = new Font("Arial Black", 14, FontStyle.Bold),
                ForeColor = ACCENT_PINK,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            addFolderBtn = CreateNeonButton("📁 Add Folder", ACCENT_PURPLE);
            addFolderBtn.Click += AddFolderClick;
            addFolderBtn.Margin = new Padding(0, 10, 0, 10);

            resetBtn = CreateNeonButton("🗑 Reset", ACCENT_RED);
            resetBtn.Click += ResetClick;
            resetBtn.Margin = new Padding(0, 10, 0, 10);

            statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 9),
                ForeColor = ACCENT_GREEN,
                AutoSize = true,
                Margin = new Padding(0, 30, 0, 0)
            };

            sidebarFlow.Controls.Add(logoLabel);
            sidebarFlow.Controls.Add(addFolderBtn);
            sidebarFlow.Controls.Add(resetBtn);
            sidebarFlow.Controls.Add(statusLabel);

            sidebar.Controls.Add(sidebarFlow);

            // MAIN CONTENT
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                BackColor = BG
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));

            // SONG LIST
            songList = new CustomListBox
            {
                Dock = DockStyle.Fill
            };
            songList.DoubleClick += PlaySelected;

            mainPanel.Controls.Add(songList, 0, 0);

            // PLAYER PANEL
            var player = BuildPlayer();
            mainPanel.Controls.Add(player, 0, 1);

            root.Controls.Add(sidebar, 0, 0);
            root.Controls.Add(mainPanel, 1, 0);
        }

        private Panel BuildPlayer()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(ACCENT_CYAN, 2))
                {
                    e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
                }
            };

            // Album Art + Now Playing Info
            var infoPanel = new Panel
            {
                Left = 10,
                Top = 10,
                Width = 220,
                Height = 220,
                BackColor = BG
            };

            albumArt = new PictureBox
            {
                Left = 0,
                Top = 0,
                Width = 220,
                Height = 220,
                BackgroundImage = CreatePlaceholderAlbumArt(220, 220),
                BackgroundImageLayout = ImageLayout.Stretch,
                BorderStyle = BorderStyle.None
            };

            infoPanel.Controls.Add(albumArt);
            panel.Controls.Add(infoPanel);

            // Title and Album
            songTitle = new Label
            {
                Left = 240,
                Top = 15,
                Width = 400,
                Height = 40,
                Text = "No song playing",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = ACCENT_PINK,
                AutoEllipsis = true
            };

            albumLabel = new Label
            {
                Left = 240,
                Top = 55,
                Width = 400,
                Height = 25,
                Text = "Unknown Album",
                Font = new Font("Segoe UI", 11),
                ForeColor = ACCENT_CYAN,
                AutoEllipsis = true
            };

            // TIME LABEL
            timeLabel = new Label
            {
                Left = 240,
                Top = 85,
                Width = 200,
                Height = 20,
                Text = "00:00 / 00:00",
                Font = new Font("Courier New", 10),
                ForeColor = ACCENT_GREEN
            };

            // SEEK BAR
            seekBar = new CustomTrackBar
            {
                Left = 240,
                Top = 110,
                Width = 500
            };
            seekBar.Scroll += (s, e) => audio.Seek(seekBar.Value);

            // PLAYBACK BUTTONS
            playPauseBtn = CreateNeonButton("▶ PLAY", ACCENT_GREEN, 50, 150);
            playPauseBtn.Click += PlayPauseClick;

            loopBtn = CreateNeonButton("NORMAL", ACCENT_YELLOW, 110, 150);
            loopBtn.Click += LoopClick;

            // VOLUME
            var volLabel = new Label
            {
                Left = 620,
                Top = 150,
                Width = 60,
                Height = 20,
                Text = "VOL",
                Font = new Font("Segoe UI", 9),
                ForeColor = ACCENT_ORANGE
            };

            volumeBar = new CustomTrackBar
            {
                Left = 620,
                Top = 170,
                Width = 100,
                Minimum = 0,
                Maximum = 100,
                Value = 100
            };
            volumeBar.Scroll += (s, e) =>
            {
                audio.Volume = volumeBar.Value / 100f;
            };

            // PITCH
            var pitchLabel = new Label
            {
                Left = 750,
                Top = 150,
                Width = 60,
                Height = 20,
                Text = "PITCH",
                Font = new Font("Segoe UI", 9),
                ForeColor = ACCENT_BLUE
            };

            pitchBar = new CustomTrackBar
            {
                Left = 750,
                Top = 170,
                Width = 100,
                Minimum = -6,
                Maximum = 6,
                Value = 0
            };
            pitchBar.Scroll += (s, e) => audio.SetPitch(pitchBar.Value);

            panel.Controls.Add(songTitle);
            panel.Controls.Add(albumLabel);
            panel.Controls.Add(timeLabel);
            panel.Controls.Add(seekBar);
            panel.Controls.Add(playPauseBtn);
            panel.Controls.Add(loopBtn);
            panel.Controls.Add(volLabel);
            panel.Controls.Add(volumeBar);
            panel.Controls.Add(pitchLabel);
            panel.Controls.Add(pitchBar);

            return panel;
        }

        private NeonButton CreateNeonButton(string text, Color color, int x = 0, int y = 0)
        {
            var btn = new NeonButton
            {
                Text = text,
                NeonColor = color,
                BackColor = BG,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Width = 160,
                Height = 40,
                Left = x,
                Top = y
            };

            return btn;
        }

        private Bitmap CreatePlaceholderAlbumArt(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(30, 30, 30));

                using (var brush = new LinearGradientBrush(
                    new Point(0, 0),
                    new Point(width, height),
                    ACCENT_PURPLE,
                    ACCENT_CYAN))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }

                using (var font = new Font("Arial Black", 36, FontStyle.Bold))
                {
                    var textSize = g.MeasureString("♪", font);
                    var x = (width - textSize.Width) / 2;
                    var y = (height - textSize.Height) / 2;
                    g.DrawString("♪", font, new SolidBrush(BG), x, y);
                }
            }

            return bmp;
        }

        // LOGIC
        private void LoadSavedSongs()
        {
            songs = StorageService.Load();
            foreach (var s in songs)
                songList.Items.Add(s);
        }

        private void SaveSongs() => StorageService.Save(songs);

        private void AddFolderClick(object? s, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var files = System.IO.Directory.GetFiles(dialog.SelectedPath, "*.*", System.IO.SearchOption.AllDirectories);

                foreach (var f in files)
                {
                    if (f.EndsWith(".mp3") || f.EndsWith(".wav"))
                    {
                        var song = new Song
                        {
                            Title = System.IO.Path.GetFileNameWithoutExtension(f),
                            FilePath = f
                        };

                        songs.Add(song);
                        songList.Items.Add(song);
                    }
                }

                SaveSongs();
                statusLabel.Text = $"Added {files.Length} songs";
            }
        }

        private void ResetClick(object? s, EventArgs e)
        {
            var result = MessageBox.Show(
                "Delete all songs?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes) return;

            songs.Clear();
            songList.Items.Clear();
            StorageService.Clear();
            statusLabel.Text = "Library cleared";
        }

        private void PlaySelected(object? s, EventArgs e)
        {
            if (songList.SelectedIndex < 0) return;

            audio.Stop();
            current = songs[songList.SelectedIndex];

            audio.Play(current.FilePath, pitchBar.Value, 0, -1);

            seekBar.Maximum = (int)audio.Duration;
            songTitle.Text = current.Title;
            albumLabel.Text = current.Album ?? "Unknown Album";
            playPauseBtn.Text = "⏸ PAUSE";
            statusLabel.Text = "Now Playing";
            songList.Invalidate();
        }

        private void PlayPauseClick(object? s, EventArgs e)
        {
            if (audio.IsPlaying)
            {
                audio.Pause();
                playPauseBtn.Text = "▶ PLAY";
                statusLabel.Text = "Paused";
            }
            else
            {
                audio.Resume();
                playPauseBtn.Text = "⏸ PAUSE";
                statusLabel.Text = "Playing";
            }
        }

        private void LoopClick(object? s, EventArgs e)
        {
            mode = (PlayMode)(((int)mode + 1) % 4);

            loopBtn.Text = mode switch
            {
                PlayMode.Normal => "NORMAL",
                PlayMode.RepeatOne => "LOOP 1",
                PlayMode.RepeatAll => "LOOP ALL",
                PlayMode.NoAutoNext => "NO AUTO",
                _ => "MODE"
            };
        }

        private void UpdateUI(object? s, EventArgs e)
        {
            if (current == null) return;

            seekBar.Value = Math.Min(seekBar.Maximum, (int)audio.Position);

            var cur = TimeSpan.FromMilliseconds(audio.Position);
            var total = TimeSpan.FromMilliseconds(audio.Duration);

            timeLabel.Text = $"{cur:mm\\:ss} / {total:mm\\:ss}";

            audio.Update();
        }

        private void HandlePlaybackFinished()
        {
            if (current == null) return;

            if (mode == PlayMode.RepeatOne)
            {
                PlaySelected(null, EventArgs.Empty);
                return;
            }

            if (mode == PlayMode.NoAutoNext)
                return;

            int i = songList.SelectedIndex;

            if (i < songList.Items.Count - 1)
            {
                songList.SelectedIndex++;
                PlaySelected(null, EventArgs.Empty);
            }
            else if (mode == PlayMode.RepeatAll)
            {
                songList.SelectedIndex = 0;
                PlaySelected(null, EventArgs.Empty);
            }
        }

        protected override void Dispose(bool disposing)
        {
            audio.Dispose();
            base.Dispose(disposing);
        }
    }

    // CUSTOM CONTROLS
    public class NeonButton : Button
    {
        public Color NeonColor { get; set; } = Color.Cyan;

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Border glow
            using (var pen = new Pen(NeonColor, 2))
            {
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }

            // Inner glow
            using (var pen = new Pen(Color.FromArgb(100, NeonColor.R, NeonColor.G, NeonColor.B), 1))
            {
                var rect = new Rectangle(2, 2, Width - 5, Height - 5);
                e.Graphics.DrawRectangle(pen, rect);
            }

            // Text
            using (var brush = new SolidBrush(NeonColor))
            {
                var textSize = e.Graphics.MeasureString(Text, Font);
                var x = (Width - textSize.Width) / 2;
                var y = (Height - textSize.Height) / 2;
                e.Graphics.DrawString(Text, Font, brush, x, y);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            BackColor = Color.FromArgb(40, 40, 40);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = Color.FromArgb(20, 20, 20);
            Invalidate();
        }
    }

    public class CustomTrackBar : TrackBar
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Track
            using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
            {
                e.Graphics.FillRectangle(brush, 0, Height / 2 - 3, Width, 6);
            }

            // Progress
            var progress = (float)(Value - Minimum) / (Maximum - Minimum) * Width;
            using (var brush = new SolidBrush(Color.FromArgb(0, 208, 255)))
            {
                e.Graphics.FillRectangle(brush, 0, Height / 2 - 3, progress, 6);
            }

            // Thumb
            var thumbX = progress - 6;
            using (var brush = new SolidBrush(Color.FromArgb(0, 208, 255)))
            {
                e.Graphics.FillEllipse(brush, thumbX, Height / 2 - 8, 12, 12);
            }
        }
    }

    public class CustomListBox : ListBox
    {
        public CustomListBox()
        {
            BackColor = Color.FromArgb(20, 20, 20);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11);
            BorderStyle = BorderStyle.None;
            DrawMode = DrawMode.OwnerDrawVariable;
            ItemHeight = 80;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var song = Items[e.Index] as Song;
            if (song == null) return;

            // Background
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, 50, 100)), e.Bounds);
                using (var pen = new Pen(Color.FromArgb(0, 208, 255), 2))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds);
                }
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 20)), e.Bounds);
                using (var pen = new Pen(Color.FromArgb(40, 40, 40), 1))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds);
                }
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Album Art Placeholder
            var albumRect = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top + 5, 70, 70);
            using (var brush = new LinearGradientBrush(
                albumRect,
                Color.FromArgb(174, 0, 255),
                Color.FromArgb(0, 208, 255),
                45))
            {
                e.Graphics.FillRectangle(brush, albumRect);
            }

            using (var font = new Font("Arial Black", 20, FontStyle.Bold))
            {
                var textSize = e.Graphics.MeasureString("♪", font);
                var x = albumRect.Left + (albumRect.Width - textSize.Width) / 2;
                var y = albumRect.Top + (albumRect.Height - textSize.Height) / 2;
                e.Graphics.DrawString("♪", font, new SolidBrush(Color.FromArgb(20, 20, 20)), x, y);
            }

            // Song Info
            var titleRect = new Rectangle(e.Bounds.Left + 90, e.Bounds.Top + 10, e.Bounds.Width - 110, 25);
            using (var font = new Font("Arial", 12, FontStyle.Bold))
            {
                e.Graphics.DrawString(song.Title, font, new SolidBrush(Color.FromArgb(255, 0, 115)), titleRect);
            }

            var albumRect2 = new Rectangle(e.Bounds.Left + 90, e.Bounds.Top + 38, e.Bounds.Width - 110, 20);
            using (var font = new Font("Segoe UI", 10))
            {
                e.Graphics.DrawString(song.Album ?? "Unknown Album", font, new SolidBrush(Color.FromArgb(0, 208, 255)), albumRect2);
            }

            var durationText = "2:45"; // Placeholder
            var durationSize = e.Graphics.MeasureString(durationText, Font);
            e.Graphics.DrawString(durationText, Font, new SolidBrush(Color.FromArgb(38, 255, 0)), 
                e.Bounds.Right - 40, e.Bounds.Top + 30);
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            e.ItemHeight = 80;
        }
    }
}
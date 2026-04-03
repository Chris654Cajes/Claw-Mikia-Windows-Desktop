using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MusicVault.Models;
using MusicVault.Services;

namespace MusicVault
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

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
        private RoundedPictureBox albumArt;

        private Button playPauseBtn, loopBtn;
        private NeonButton addFolderBtn, resetBtn;
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
            BackColor = BG;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            BuildUI();

            this.Load += (s, e) =>
            {
                var working = Screen.FromHandle(this.Handle).WorkingArea;
                this.MaximizedBounds = working;
                this.WindowState = FormWindowState.Maximized;
            };

            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.Bounds = Screen.FromHandle(this.Handle).WorkingArea;
                    this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                }
            };
            LoadSavedSongs();

            timer = new Timer { Interval = 300 };
            timer.Tick += UpdateUI;
            timer.Start();

            audio.PlaybackStopped += HandlePlaybackFinished;
        }

        private void BuildUI()
        {
            // CUSTOM TITLE BAR
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = BG
            };

            var titleLabel = new Label
            {
                Text = "🎵 MusicVault PRO",
                ForeColor = ACCENT_PINK,
                Font = new Font("Arial", 12, FontStyle.Bold),
                AutoSize = true,
                Left = 10,
                Top = 5
            };

            var minimizeBtn = new Button
            {
                Text = "—",
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = BG,
                ForeColor = Color.Magenta,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            var maximizeBtn = new Button
            {
                Text = "□",
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = BG,
                ForeColor = Color.Magenta,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            maximizeBtn.FlatAppearance.BorderSize = 0;
            maximizeBtn.Click += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                else
                {
                    this.MaximizedBounds = Screen.GetWorkingArea(this);
                    this.WindowState = FormWindowState.Maximized;
                }
                UpdateMaximizeButton(maximizeBtn);
            };

            var closeBtn = new Button
            {
                Text = "✕",
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = BG,
                ForeColor = Color.Magenta,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => this.Close();

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(minimizeBtn);
            titleBar.Controls.Add(maximizeBtn);
            titleBar.Controls.Add(closeBtn);

            // Position buttons on resize
            titleBar.Resize += (s, e) =>
            {
                minimizeBtn.Left = titleBar.Width - 120;
                maximizeBtn.Left = titleBar.Width - 80;
                closeBtn.Left = titleBar.Width - 40;
                UpdateMaximizeButton(maximizeBtn);
            };

            // Make title bar draggable (except on buttons)
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && !minimizeBtn.Bounds.Contains(e.Location) && !maximizeBtn.Bounds.Contains(e.Location) && !closeBtn.Bounds.Contains(e.Location))
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            Controls.Add(titleBar);

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

            // Search Box
            var searchLabel = new Label
            {
                Text = "Search Songs",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ACCENT_CYAN,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };

            var searchBox = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Width = 250,
                Height = 30,
                Margin = new Padding(0, 0, 0, 10)
            };
            searchBox.TextChanged += SearchSongs;

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
            sidebarFlow.Controls.Add(searchLabel);
            sidebarFlow.Controls.Add(searchBox);
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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));

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

        private void UpdateMaximizeButton(Button maximizeBtn)
        {
            maximizeBtn.Text = this.WindowState == FormWindowState.Maximized ? "❐" : "□";
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

            // Now Playing Section - Organized Layout
            var nowPlayingPanel = new TableLayoutPanel
            {
                Left = 10,
                Top = 25,
                Width = panel.Width - 20,
                Height = 150,
                BackColor = BG,
                ColumnCount = 2,
                RowCount = 2
            };

            // Configure columns: Left (30%), Right (70%)
            nowPlayingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            nowPlayingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            
            // Configure rows: Top (60%), Bottom (40%)
            nowPlayingPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            nowPlayingPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            // Album Art Section (Top Left)
            var albumPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(10)
            };

            albumArt = new RoundedPictureBox
            {
                Dock = DockStyle.Fill,
                BackgroundImage = CreatePlaceholderAlbumArt(150, 150),
                BackgroundImageLayout = ImageLayout.Stretch,
                BorderStyle = BorderStyle.None,
                CornerRadius = 15
            };

            albumPanel.Controls.Add(albumArt);
            nowPlayingPanel.Controls.Add(albumPanel, 0, 0);

            // Info Section (Top Right)
            var infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };

            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            // Title
            songTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No song playing",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = ACCENT_PINK,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Album
            albumLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Unknown Album",
                Font = new Font("Segoe UI", 11),
                ForeColor = ACCENT_CYAN,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Time Label
            timeLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "00:00 / 00:00",
                Font = new Font("Courier New", 10),
                ForeColor = ACCENT_GREEN,
                TextAlign = ContentAlignment.MiddleLeft
            };

            infoPanel.Controls.Add(songTitle, 0, 0);
            infoPanel.Controls.Add(albumLabel, 0, 1);
            infoPanel.Controls.Add(timeLabel, 0, 2);

            nowPlayingPanel.Controls.Add(infoPanel, 1, 0);

            // Seek Bar Section (Bottom - spans both columns)
            var seekPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(10)
            };

            seekBar = new CustomTrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            seekBar.Scroll += (s, e) => audio.Seek(seekBar.Value);

            seekPanel.Controls.Add(seekBar);
            nowPlayingPanel.Controls.Add(seekPanel, 0, 1);
            nowPlayingPanel.SetColumnSpan(seekPanel, 2);

            panel.Controls.Add(nowPlayingPanel);

            // Playback Controls Section
            var controlsSection = new TableLayoutPanel
            {
                Left = 10,
                Top = 185,
                Width = panel.Width - 20,
                Height = 120,
                BackColor = BG,
                ColumnCount = 3,
                RowCount = 2
            };

            // Configure columns: Playback buttons (60%), Volume (20%), Pitch (20%)
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            controlsSection.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            controlsSection.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // Playback Buttons Panel
            var playbackPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = BG,
                Padding = new Padding(10),
                WrapContents = false,
                AutoSize = false
            };

            var previousBtn = new CircularButton
            {
                Icon = "⏮",
                NeonColor = ACCENT_CYAN,
                BackColor = BG,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Width = 60,
                Height = 60,
                FlatStyle = FlatStyle.Flat
            };
            previousBtn.FlatAppearance.BorderSize = 0;
            previousBtn.Click += PreviousSongClick;
            playbackPanel.Controls.Add(previousBtn);

            var rewindBtn = new CircularButton
            {
                Icon = "⏪",
                NeonColor = ACCENT_BLUE,
                BackColor = BG,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Width = 60,
                Height = 60,
                FlatStyle = FlatStyle.Flat
            };
            rewindBtn.FlatAppearance.BorderSize = 0;
            rewindBtn.Click += RewindClick;
            playbackPanel.Controls.Add(rewindBtn);

            playPauseBtn = new CircularButton
            {
                Icon = "▶",
                NeonColor = ACCENT_GREEN,
                BackColor = BG,
                Font = new Font("Arial", 16, FontStyle.Bold),
                Width = 60,
                Height = 60,
                FlatStyle = FlatStyle.Flat
            };
            playPauseBtn.FlatAppearance.BorderSize = 0;
            playPauseBtn.Click += PlayPauseClick;
            playbackPanel.Controls.Add(playPauseBtn);

            var fastforwardBtn = new CircularButton
            {
                Icon = "⏩",
                NeonColor = ACCENT_BLUE,
                BackColor = BG,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Width = 60,
                Height = 60,
                FlatStyle = FlatStyle.Flat
            };
            fastforwardBtn.FlatAppearance.BorderSize = 0;
            fastforwardBtn.Click += FastForwardClick;
            playbackPanel.Controls.Add(fastforwardBtn);

            var nextBtn = new CircularButton
            {
                Icon = "⏭",
                NeonColor = ACCENT_CYAN,
                BackColor = BG,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Width = 60,
                Height = 60,
                FlatStyle = FlatStyle.Flat
            };
            nextBtn.FlatAppearance.BorderSize = 0;
            nextBtn.Click += NextSongClick;
            playbackPanel.Controls.Add(nextBtn);

            loopBtn = new CircularButton
            {
                Icon = "⟲",
                NeonColor = ACCENT_YELLOW,
                BackColor = BG,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Width = 60,
                Height = 60,
                FlatStyle = FlatStyle.Flat
            };
            loopBtn.FlatAppearance.BorderSize = 0;
            loopBtn.Click += LoopClick;
            playbackPanel.Controls.Add(loopBtn);

            controlsSection.Controls.Add(playbackPanel, 0, 0);

            // Volume Panel
            var volumePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            var volLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "VOLUME",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ACCENT_ORANGE,
                TextAlign = ContentAlignment.MiddleCenter
            };

            volumeBar = new CustomTrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value = 100
            };
            volumeBar.Scroll += (s, e) =>
            {
                audio.Volume = volumeBar.Value / 100f;
            };

            volumePanel.Controls.Add(volLabel, 0, 0);
            volumePanel.Controls.Add(volumeBar, 0, 1);

            controlsSection.Controls.Add(volumePanel, 1, 0);

            // Pitch Panel
            var pitchPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            var pitchLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "PITCH",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ACCENT_PURPLE,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pitchBar = new CustomTrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = -6,
                Maximum = 6,
                Value = 0
            };
            pitchBar.Scroll += (s, e) => audio.SetPitch(pitchBar.Value);

            pitchPanel.Controls.Add(pitchLabel, 0, 0);
            pitchPanel.Controls.Add(pitchBar, 0, 1);

            controlsSection.Controls.Add(pitchPanel, 2, 0);

            panel.Controls.Add(controlsSection);

            // Handle resize to adjust element sizes
            panel.Resize += (s, e) =>
            {
                nowPlayingPanel.Width = panel.Width - 20;
                controlsSection.Width = panel.Width - 20;
            };

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
            ((CircularButton)playPauseBtn).Icon = "⏸";
            playPauseBtn.Invalidate();
            statusLabel.Text = "Now Playing";
            songList.Invalidate();
        }

        private void PlayPauseClick(object? s, EventArgs e)
        {
            if (audio.IsPlaying)
            {
                audio.Pause();
                ((CircularButton)playPauseBtn).Icon = "▶";
                playPauseBtn.Invalidate();
                statusLabel.Text = "Paused";
            }
            else
            {
                audio.Resume();
                ((CircularButton)playPauseBtn).Icon = "⏸";
                playPauseBtn.Invalidate();
                statusLabel.Text = "Playing";
            }
        }

        private void LoopClick(object? s, EventArgs e)
        {
            mode = (PlayMode)(((int)mode + 1) % 4);

            var circularBtn = (CircularButton)loopBtn;
            circularBtn.Icon = mode switch
            {
                PlayMode.Normal => "⟲",
                PlayMode.RepeatOne => "①",
                PlayMode.RepeatAll => "🔁",
                PlayMode.NoAutoNext => "⊘",
                _ => "⟲"
            };
            loopBtn.Invalidate();
        }

        private void RewindClick(object? s, EventArgs e)
        {
            if (current != null)
            {
                long newPosition = Math.Max(0, (long)audio.Position - 5000);
                audio.Seek(newPosition);
            }
        }

        private void FastForwardClick(object? s, EventArgs e)
        {
            if (current != null)
            {
                long newPosition = Math.Min((long)audio.Duration, (long)audio.Position + 5000);
                audio.Seek(newPosition);
            }
        }

        private void PreviousSongClick(object? s, EventArgs e)
        {
            if (songList.SelectedIndex > 0)
            {
                songList.SelectedIndex--;
                PlaySelected(null, EventArgs.Empty);
            }
        }

        private void NextSongClick(object? s, EventArgs e)
        {
            if (songList.SelectedIndex < songList.Items.Count - 1)
            {
                songList.SelectedIndex++;
                PlaySelected(null, EventArgs.Empty);
            }
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

        private void SearchSongs(object? sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var searchText = textBox.Text.ToLower();
            
            // Clear current list
            songList.Items.Clear();
            
            // Filter and add songs
            foreach (var song in songs)
            {
                if (string.IsNullOrEmpty(searchText) || 
                    song.Title.ToLower().Contains(searchText) ||
                    (song.Album?.ToLower().Contains(searchText) ?? false))
                {
                    songList.Items.Add(song);
                }
            }
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

    public class CircularButton : Button
    {
        public Color NeonColor { get; set; } = Color.Cyan;
        public string Icon { get; set; } = "▶";

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height);
            int x = (Width - size) / 2;
            int y = (Height - size) / 2;

            // Draw circle border
            using (var pen = new Pen(NeonColor, 2))
            {
                e.Graphics.DrawEllipse(pen, x, y, size - 1, size - 1);
            }

            // Draw inner glow
            using (var pen = new Pen(Color.FromArgb(100, NeonColor.R, NeonColor.G, NeonColor.B), 1))
            {
                e.Graphics.DrawEllipse(pen, x + 2, y + 2, size - 5, size - 5);
            }

            // Draw icon
            using (var brush = new SolidBrush(NeonColor))
            {
                var textSize = e.Graphics.MeasureString(Icon, Font);
                var iconX = (Width - textSize.Width) / 2;
                var iconY = (Height - textSize.Height) / 2;
                e.Graphics.DrawString(Icon, Font, brush, iconX, iconY);
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

    public class RoundedPictureBox : PictureBox
    {
        public int CornerRadius { get; set; } = 10;

        protected override void OnPaint(PaintEventArgs e)
        {
            var gp = new GraphicsPath();
            gp.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90);
            gp.AddArc(Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90);
            gp.AddArc(Width - CornerRadius, Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
            gp.AddArc(0, Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
            gp.CloseFigure();

            this.Region = new Region(gp);

            if (BackgroundImage != null)
            {
                e.Graphics.DrawImage(BackgroundImage, 0, 0, Width, Height);
            }
            else
            {
                e.Graphics.Clear(BackColor);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
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
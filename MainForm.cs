using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MusicVault.Models;
using MusicVault.Services;

namespace MusicVault
{
    public class MainForm : Form
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

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
        private CustomTrackBar trimStartBar, trimEndBar;
        private Label trimStartLabel, trimEndLabel;

        private Timer timer;

        private enum PlayMode
        {
            Normal,
            RepeatOne,
            RepeatAll,
            NoAutoNext
        }

        private PlayMode mode = PlayMode.Normal;
        private bool isChangingSong = false;

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 0x1;
            const int HTCAPTION = 0x2;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    Point cursor = this.PointToClient(Cursor.Position);

                    // Resize border thickness
                    int border = 8;

                    if (this.WindowState != FormWindowState.Maximized)
                    {
                        if (cursor.X <= border && cursor.Y <= border) m.Result = (IntPtr)HTTOPLEFT;
                        else if (cursor.X >= this.Width - border && cursor.Y <= border) m.Result = (IntPtr)HTTOPRIGHT;
                        else if (cursor.X <= border && cursor.Y >= this.Height - border) m.Result = (IntPtr)HTBOTTOMLEFT;
                        else if (cursor.X >= this.Width - border && cursor.Y >= this.Height - border) m.Result = (IntPtr)HTBOTTOMRIGHT;
                        else if (cursor.X <= border) m.Result = (IntPtr)HTLEFT;
                        else if (cursor.X >= this.Width - border) m.Result = (IntPtr)HTRIGHT;
                        else if (cursor.Y <= border) m.Result = (IntPtr)HTTOP;
                        else if (cursor.Y >= this.Height - border) m.Result = (IntPtr)HTBOTTOM;
                        else if (cursor.Y <= 40) m.Result = (IntPtr)HTCAPTION;
                    }
                    else
                    {
                        if (cursor.Y <= 40) m.Result = (IntPtr)HTCAPTION;
                    }
                }
                return;
            }

            base.WndProc(ref m);
        }

        public MainForm()
        {
            Text = "Claw Mikia PRO";
            BackColor = BG;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            
            BuildUI();

            this.Load += (s, e) =>
            {
                var working = Screen.FromHandle(this.Handle).WorkingArea;
                this.MaximizedBounds = working;
                this.WindowState = FormWindowState.Maximized;
                
                // Apply dark theme to title bar and controls
                ApplyDarkTheme(this.Handle);
                ApplyThemeToControls(this);
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

        private void ApplyDarkTheme(IntPtr handle)
        {
            int darkMode = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }

        private void ApplyThemeToControls(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Apply dark theme to common controls that support it via uxtheme
                if (ctrl is ListBox || ctrl is TextBox || ctrl is ListView || ctrl is TreeView || ctrl is ComboBox)
                {
                    SetWindowTheme(ctrl.Handle, "DarkMode_Explorer", null);
                }
                
                // Panels and FlowLayoutPanels can also have scrollbars
                if (ctrl is Panel || ctrl is FlowLayoutPanel || ctrl is ContainerControl)
                {
                    SetWindowTheme(ctrl.Handle, "DarkMode_Explorer", null);
                }
                
                if (ctrl.HasChildren)
                {
                    ApplyThemeToControls(ctrl);
                }
            }
        }

        private void BuildUI()
        {
            // CUSTOM TITLE BAR
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = BG
            };

            var titleLabel = new Label
            {
                Text = "🎵 Claw Mikia PRO",
                ForeColor = ACCENT_PINK,
                Font = new Font("Arial", 12, FontStyle.Bold),
                AutoSize = true,
                Left = 10,
                Top = 10
            };

            var minimizeBtn = new Button
            {
                Text = "—",
                Width = 45,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = BG,
                ForeColor = Color.Magenta,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Margin = Padding.Empty
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            var closeBtn = new Button
            {
                Text = "✕",
                Width = 45,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = BG,
                ForeColor = Color.Magenta,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Margin = Padding.Empty
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => Application.Exit();

            var controlBox = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 100,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5, 5, 0, 0),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            controlBox.Controls.Add(minimizeBtn);
            controlBox.Controls.Add(closeBtn);

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(controlBox);

            // Make title bar draggable (except on controls)
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && !controlBox.Bounds.Contains(e.Location))
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            Controls.Add(titleBar);
            titleBar.BringToFront(); // Ensure title bar is on top

            // ROOT LAYOUT
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 40, 0, 0) // Leave space for title bar
            };

            root.BackColor = BG;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Controls.Add(root);

            // SIDEBAR
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                MinimumSize = new Size(200, 0)
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
                AutoScroll = true,
                WrapContents = false // Ensure it stays as a column
            };
            sidebarFlow.Resize += (s, e) => {
                foreach (Control ctrl in sidebarFlow.Controls)
                {
                    ctrl.Width = sidebarFlow.ClientSize.Width - sidebarFlow.Padding.Horizontal - 5;
                }
            };

            var logoLabel = new Label
            {
                Text = "🎵 Libraries",
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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); // More compact player

            // SONG LIST
            songList = new CustomListBox
            {
                Dock = DockStyle.Fill
            };
            songList.DoubleClick += PlaySelected;
            songList.SelectedIndexChanged += (s, e) => songList.Invalidate();

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

            // Main Player Container - Album on Left, everything else on Right
            var playerContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 10)
            };

            playerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Fixed width for album art
            playerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Flexible width for controls

            // Album Art Section (Left)
            var albumPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(0)
            };

            albumArt = new RoundedPictureBox
            {
                Width = 140,
                Height = 140,
                Anchor = AnchorStyles.None, // Center the album art
                BackgroundImage = CreatePlaceholderAlbumArt(140, 140),
                BackgroundImageLayout = ImageLayout.Stretch,
                BorderStyle = BorderStyle.None,
                CornerRadius = 15
            };
            
            // Re-center albumArt when albumPanel resizes
            albumPanel.Resize += (s, e) => {
                albumArt.Left = (albumPanel.Width - albumArt.Width) / 2;
                albumArt.Top = (albumPanel.Height - albumArt.Height) / 2;
            };

            albumPanel.Controls.Add(albumArt);
            playerContainer.Controls.Add(albumPanel, 0, 0);

            // Right Stack (Info -> Seekbar -> Controls)
            var rightStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 3
            };

            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); // Info
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 20)); // Seekbar
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 35)); // Controls

            // Info Section
            var infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10, 0, 10, 0)
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
            rightStack.Controls.Add(infoPanel, 0, 0);

            // Seek Bar Section (Middle)
            var seekPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(10, 0, 10, 0)
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
            rightStack.Controls.Add(seekPanel, 0, 1);

            // Playback Controls Section (Bottom)
            var controlsSection = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0)
            };

            // Configure columns: Buttons (35%), Volume (15%), Pitch (15%), TrimStart (17.5%), TrimEnd (17.5%)
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5f));
            controlsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5f));

            // Playback Buttons Panel
            var playbackPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = BG,
                Padding = new Padding(5, 0, 5, 0),
                WrapContents = false,
                AutoSize = false
            };
            
            // Re-center playback controls on resize
            playbackPanel.Resize += (s, e) => {
                int totalWidth = playbackPanel.Controls.Count * 45; // Approx width
                int paddingLeft = Math.Max(0, (playbackPanel.Width - totalWidth) / 2);
                playbackPanel.Padding = new Padding(paddingLeft, 0, 0, 0);
            };

            var btnSize = 40;
            var btnFont = new Font("Arial", 11, FontStyle.Bold);

            var previousBtn = new CircularButton
            {
                Icon = "⏮",
                NeonColor = ACCENT_CYAN,
                BackColor = BG,
                Font = btnFont,
                Width = btnSize,
                Height = btnSize,
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
                Font = btnFont,
                Width = btnSize,
                Height = btnSize,
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
                Font = new Font("Arial", 12, FontStyle.Bold),
                Width = btnSize,
                Height = btnSize,
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
                Font = btnFont,
                Width = btnSize,
                Height = btnSize,
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
                Font = btnFont,
                Width = btnSize,
                Height = btnSize,
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
                Font = btnFont,
                Width = btnSize,
                Height = btnSize,
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
                Padding = new Padding(2)
            };

            var volLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "VOL",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
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
            volumeBar.Scroll += (s, e) => audio.Volume = volumeBar.Value / 100f;

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
                Padding = new Padding(2)
            };

            var pitchLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "PITCH",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
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

            // Trim Start Panel
            var trimStartPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(2)
            };

            trimStartLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "START",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = ACCENT_ORANGE,
                TextAlign = ContentAlignment.MiddleCenter
            };

            trimStartBar = new CustomTrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            trimStartBar.Scroll += (s, e) => {
                if (current != null) {
                    current.TrimStart = (long)((trimStartBar.Value / 100f) * audio.Duration);
                    trimStartLabel.Text = $"S: {FormatTime(current.TrimStart)}";
                    SaveSongs();
                }
            };

            trimStartPanel.Controls.Add(trimStartLabel, 0, 0);
            trimStartPanel.Controls.Add(trimStartBar, 0, 1);
            controlsSection.Controls.Add(trimStartPanel, 3, 0);

            // Trim End Panel
            var trimEndPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(2)
            };

            trimEndLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "END",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = ACCENT_PURPLE,
                TextAlign = ContentAlignment.MiddleCenter
            };

            trimEndBar = new CustomTrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value = 100
            };
            trimEndBar.Scroll += (s, e) => {
                if (current != null) {
                    current.TrimEnd = (long)((trimEndBar.Value / 100f) * audio.Duration);
                    trimEndLabel.Text = $"E: {FormatTime(current.TrimEnd <= 0 ? audio.Duration : current.TrimEnd)}";
                    SaveSongs();
                }
            };

            trimEndPanel.Controls.Add(trimEndLabel, 0, 0);
            trimEndPanel.Controls.Add(trimEndBar, 0, 1);
            controlsSection.Controls.Add(trimEndPanel, 4, 0);

            rightStack.Controls.Add(controlsSection, 0, 2);
            playerContainer.Controls.Add(rightStack, 1, 0);

            panel.Controls.Add(playerContainer);

            return panel;
        }

        private string FormatTime(long milliseconds)
        {
            var ts = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }

        private NeonButton CreateNeonButton(string text, Color color, int x = 0, int y = 0)
        {
            var btn = new NeonButton
            {
                Text = text,
                NeonColor = color,
                BackColor = BG,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Width = 250,
                Height = 45,
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
            if (songList.SelectedIndex < 0 || isChangingSong) return;

            try 
            {
                isChangingSong = true;
                
                // Stop any existing playback first
                audio.Stop();

                // Get the song from the ListBox items (it might be filtered)
                var selectedSong = songList.Items[songList.SelectedIndex] as Song;
                if (selectedSong == null) return;
                
                current = selectedSong;

                // Initialize trim values if not set
                if (current.TrimEnd <= 0) current.TrimEnd = -1;

                audio.Play(current.FilePath, pitchBar.Value, current.TrimStart, current.TrimEnd);

                seekBar.Maximum = (int)audio.Duration;
                
                // Update trim controls
                if (audio.Duration > 0)
                {
                    trimStartBar.Value = (int)((current.TrimStart / (float)audio.Duration) * 100);
                    long actualEnd = current.TrimEnd <= 0 ? audio.Duration : current.TrimEnd;
                    trimEndBar.Value = (int)((actualEnd / (float)audio.Duration) * 100);
                }
                trimStartLabel.Text = $"START: {FormatTime(current.TrimStart)}";
                trimEndLabel.Text = $"END: {FormatTime(current.TrimEnd <= 0 ? audio.Duration : current.TrimEnd)}";

                songTitle.Text = current.Title;
                albumLabel.Text = current.Album ?? "Unknown Album";
                ((CircularButton)playPauseBtn).Icon = "⏸";
                playPauseBtn.Invalidate();
                statusLabel.Text = "Now Playing";
                songList.Invalidate();
            }
            finally
            {
                // Small delay before allowing another song change to prevent rapid skipping
                Timer resetTimer = new Timer { Interval = 200 };
                resetTimer.Tick += (sender, args) => {
                    isChangingSong = false;
                    resetTimer.Stop();
                    resetTimer.Dispose();
                };
                resetTimer.Start();
            }
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
                this.Invoke((MethodInvoker)delegate {
                    songList.SelectedIndex++;
                    PlaySelected(null, EventArgs.Empty);
                });
            }
            else if (mode == PlayMode.RepeatAll)
            {
                this.Invoke((MethodInvoker)delegate {
                    songList.SelectedIndex = 0;
                    PlaySelected(null, EventArgs.Empty);
                });
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
            
            // Filter the master list
            var filteredSongs = songs.Where(song => 
                string.IsNullOrEmpty(searchText) || 
                song.Title.ToLower().Contains(searchText) ||
                (song.Album?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            // Only update if the list actually changed to prevent flickering
            songList.BeginUpdate();
            songList.Items.Clear();
            foreach (var song in filteredSongs)
            {
                songList.Items.Add(song);
            }
            songList.EndUpdate();
        }
    }

    // CUSTOM CONTROLS
    public class NeonButton : Button
    {
        public Color NeonColor { get; set; } = Color.Cyan;
        private bool isHovered = false;

        public NeonButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(15, 0, 0, 0);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Background - Always clear first to prevent artifacts
            e.Graphics.Clear(BackColor);

            if (isHovered)
            {
                using (var brush = new SolidBrush(Color.FromArgb(40, NeonColor.R, NeonColor.G, NeonColor.B)))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Text
            using (var brush = new SolidBrush(NeonColor))
            {
                var textSize = e.Graphics.MeasureString(Text, Font);
                var x = Padding.Left;
                var y = (Height - textSize.Height) / 2;
                e.Graphics.DrawString(Text, Font, brush, x, y);
            }

            // Left accent line (optional, for menu look)
            if (isHovered)
            {
                using (var pen = new Pen(NeonColor, 3))
                {
                    e.Graphics.DrawLine(pen, 0, 0, 0, Height);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
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
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int padding = 4;
            int size = Math.Min(Width, Height) - (padding * 2);
            int x = (Width - size) / 2;
            int y = (Height - size) / 2;

            var rect = new Rectangle(x, y, size, size);

            // Draw circle border
            using (var pen = new Pen(NeonColor, 2.5f))
            {
                e.Graphics.DrawEllipse(pen, rect);
            }

            // Draw inner glow
            using (var pen = new Pen(Color.FromArgb(80, NeonColor.R, NeonColor.G, NeonColor.B), 1.5f))
            {
                var innerRect = rect;
                innerRect.Inflate(-2, -2);
                e.Graphics.DrawEllipse(pen, innerRect);
            }

            // Draw icon - use a more precise centering method
            using (var brush = new SolidBrush(NeonColor))
            {
                using (var sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    
                    // Slightly adjust Y for better visual centering of symbols
                    var textRect = new RectangleF(0, 1, Width, Height);
                    e.Graphics.DrawString(Icon, Font, brush, textRect, sf);
                }
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
            DoubleBuffered = true;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            var song = Items[e.Index] as Song;
            if (song == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Use SelectedIndex directly for more reliable highlighting
            bool isSelected = (e.Index == SelectedIndex);
            
            // Background
            Color bgColor = isSelected ? Color.FromArgb(80, 255, 0, 115) : Color.FromArgb(20, 20, 20);
            using (var bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, e.Bounds);
            }

            if (isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(255, 0, 115), 3)) // Thicker neon border
                {
                    var borderRect = e.Bounds;
                    borderRect.Inflate(-1, -1);
                    g.DrawRectangle(pen, borderRect);
                }
            }
            else
            {
                using (var pen = new Pen(Color.FromArgb(40, 40, 40), 1))
                {
                    g.DrawRectangle(pen, e.Bounds);
                }
            }

            // Album Art Placeholder
            var albumRect = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top + 5, 70, 70);
            using (var brush = new LinearGradientBrush(
                albumRect,
                Color.FromArgb(174, 0, 255),
                Color.FromArgb(0, 208, 255),
                45))
            {
                g.FillRectangle(brush, albumRect);
            }

            using (var font = new Font("Arial Black", 20, FontStyle.Bold))
            {
                var textSize = g.MeasureString("♪", font);
                var x = albumRect.Left + (albumRect.Width - textSize.Width) / 2;
                var y = albumRect.Top + (albumRect.Height - textSize.Height) / 2;
                g.DrawString("♪", font, new SolidBrush(Color.FromArgb(20, 20, 20)), x, y);
            }

            // Song Info
            var titleRect = new Rectangle(e.Bounds.Left + 90, e.Bounds.Top + 10, e.Bounds.Width - 110, 25);
            using (var font = new Font("Arial", 12, FontStyle.Bold))
            {
                g.DrawString(song.Title, font, new SolidBrush(Color.FromArgb(255, 0, 115)), titleRect);
            }

            var albumRect2 = new Rectangle(e.Bounds.Left + 90, e.Bounds.Top + 38, e.Bounds.Width - 110, 20);
            using (var font = new Font("Segoe UI", 10))
            {
                g.DrawString(song.Album ?? "Unknown Album", font, new SolidBrush(Color.FromArgb(0, 208, 255)), albumRect2);
            }

            var durationText = "2:45"; // Placeholder
            g.DrawString(durationText, Font, new SolidBrush(Color.FromArgb(38, 255, 0)), 
                e.Bounds.Right - 40, e.Bounds.Top + 30);
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            e.ItemHeight = 80;
        }
    }
}
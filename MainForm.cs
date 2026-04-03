using System;
using System.Collections.Generic;
using System.Drawing;
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

        private ListBox songList;
        private PictureBox albumArt;
        private Label songTitle, timeLabel;

        private Button playPauseBtn, loopBtn, addFolderBtn, resetBtn;
        private TrackBar seekBar, volumeBar, pitchBar;

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
            Text = "MusicVault Spotify UI";
            Size = new Size(1100, 700);
            BackColor = Color.FromArgb(18, 18, 18);

            BuildLayout();

            LoadSavedSongs();

            timer = new Timer { Interval = 300 };
            timer.Tick += UpdateUI;
            timer.Start();

            audio.PlaybackStopped += HandlePlaybackFinished;
        }

        // 🔥 FIXED LAYOUT (NO OVERLAP EVER)
        private void BuildLayout()
        {
            // Sidebar FIRST
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            addFolderBtn = new Button
            {
                Text = "Add Folder",
                Dock = DockStyle.Top,
                Height = 50
            };
            addFolderBtn.Click += AddFolderClick;

            resetBtn = new Button
            {
                Text = "Reset Library",
                Dock = DockStyle.Top,
                Height = 50
            };
            resetBtn.Click += ResetClick;

            sidebar.Controls.Add(resetBtn);
            sidebar.Controls.Add(addFolderBtn);

            // MAIN PANEL SECOND
            var main = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            songList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            songList.DoubleClick += PlaySelected;
            main.Controls.Add(songList);

            // PLAYER LAST
            var player = BuildPlayer();

            // IMPORTANT ORDER
            Controls.Add(main);
            Controls.Add(player);
            Controls.Add(sidebar);
        }

        private Panel BuildPlayer()
        {
            var player = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            albumArt = new PictureBox
            {
                Size = new Size(80, 80),
                Left = 10,
                Top = 20,
                BackColor = Color.Gray
            };

            songTitle = new Label
            {
                Left = 100,
                Top = 20,
                Width = 300,
                ForeColor = Color.White,
                Text = "No song playing"
            };

            playPauseBtn = new Button
            {
                Text = "▶",
                Width = 50,
                Height = 50,
                Left = 450,
                Top = 30
            };
            playPauseBtn.Click += PlayPauseClick;

            loopBtn = new Button
            {
                Text = "Normal",
                Left = 510,
                Top = 40,
                Width = 100
            };
            loopBtn.Click += LoopClick;

            seekBar = new TrackBar
            {
                Left = 300,
                Top = 10,
                Width = 500
            };
            seekBar.Scroll += (s, e) => audio.Seek(seekBar.Value);

            timeLabel = new Label
            {
                Left = 810,
                Top = 10,
                Width = 200,
                ForeColor = Color.White
            };

            volumeBar = new TrackBar
            {
                Left = 650,
                Top = 60,
                Width = 150,
                Minimum = 0,
                Maximum = 100,
                Value = 100
            };
            volumeBar.Scroll += (s, e) =>
            {
                audio.Volume = volumeBar.Value / 100f;
            };

            pitchBar = new TrackBar
            {
                Left = 820,
                Top = 60,
                Width = 150,
                Minimum = -6,
                Maximum = 6
            };
            pitchBar.Scroll += (s, e) => audio.SetPitch(pitchBar.Value);

            player.Controls.Add(albumArt);
            player.Controls.Add(songTitle);
            player.Controls.Add(playPauseBtn);
            player.Controls.Add(loopBtn);
            player.Controls.Add(seekBar);
            player.Controls.Add(timeLabel);
            player.Controls.Add(volumeBar);
            player.Controls.Add(pitchBar);

            return player;
        }

        private void LoadSavedSongs()
        {
            songs = StorageService.Load();

            foreach (var s in songs)
                songList.Items.Add(s.DisplayText);
        }

        private void SaveSongs()
        {
            StorageService.Save(songs);
        }

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
                        songList.Items.Add(song.DisplayText);
                    }
                }

                SaveSongs();
            }
        }

        // 🔥 CONFIRMATION ADDED
        private void ResetClick(object? s, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete all songs?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes) return;

            songs.Clear();
            songList.Items.Clear();
            StorageService.Clear();
        }

        private void PlaySelected(object? s, EventArgs e)
        {
            if (songList.SelectedIndex < 0) return;

            audio.Stop();

            current = songs[songList.SelectedIndex];

            audio.Play(current.FilePath, pitchBar.Value, 0, -1);

            seekBar.Maximum = (int)audio.Duration;

            songTitle.Text = current.DisplayText;
            playPauseBtn.Text = "⏸";
        }

        private void PlayPauseClick(object? s, EventArgs e)
        {
            if (audio.IsPlaying)
            {
                audio.Pause();
                playPauseBtn.Text = "▶";
            }
            else
            {
                audio.Resume();
                playPauseBtn.Text = "⏸";
            }
        }

        private void LoopClick(object? s, EventArgs e)
        {
            mode = (PlayMode)(((int)mode + 1) % 4);

            loopBtn.Text = mode switch
            {
                PlayMode.Normal => "Normal",
                PlayMode.RepeatOne => "Repeat 1",
                PlayMode.RepeatAll => "Repeat All",
                PlayMode.NoAutoNext => "No Auto",
                _ => "Mode"
            };
        }

        private void UpdateUI(object? s, EventArgs e)
        {
            if (current == null) return;

            seekBar.Value = Math.Min(seekBar.Maximum, (int)audio.Position);

            TimeSpan cur = TimeSpan.FromMilliseconds(audio.Position);
            TimeSpan total = TimeSpan.FromMilliseconds(audio.Duration);

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
}
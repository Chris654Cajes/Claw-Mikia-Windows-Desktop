using System;

namespace MusicVault.Models
{
    /// <summary>
    /// Represents a song in the music library
    /// </summary>
    public class Song
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        
        // Duration in milliseconds
        public long Duration { get; set; }
        
        // File size in bytes
        public long FileSize { get; set; }
        
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // User customizations
        public int PitchSemitones { get; set; }  // -6 to +6
        public long TrimStart { get; set; }      // milliseconds
        public long TrimEnd { get; set; }        // milliseconds, -1 = full duration
        
        // Favorites and stats
        public bool IsFavorite { get; set; }
        public int PlayCount { get; set; }
        public DateTime? LastPlayed { get; set; }

        // Album art
        public string AlbumArtUrl { get; set; } = string.Empty;
        public bool MetadataFetched { get; set; }

        /// <summary>
        /// Gets the display duration as MM:SS format
        /// </summary>
        public string DurationDisplay
        {
            get
            {
                var ts = TimeSpan.FromMilliseconds(Duration);
                return $"{ts.Minutes}:{ts.Seconds:D2}";
            }
        }

        /// <summary>
        /// Gets the full display text for this song
        /// </summary>
        public string DisplayText => $"{Title} - {Artist}";

        public override string ToString() => DisplayText;
    }
}

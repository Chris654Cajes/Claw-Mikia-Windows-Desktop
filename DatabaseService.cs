using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MusicVault.Models;

namespace MusicVault.Services
{
    /// <summary>
    /// Handles all database operations for Music Vault
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private const string DbPath = "MusicVault.db";

        public DatabaseService()
        {
            _connectionString = $"Data Source={DbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Create Songs table
                    string createSongsTable = @"
                        CREATE TABLE IF NOT EXISTS Songs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            Artist TEXT,
                            Album TEXT,
                            FilePath TEXT UNIQUE NOT NULL,
                            FolderPath TEXT,
                            FolderName TEXT,
                            Duration INTEGER DEFAULT 0,
                            FileSize INTEGER DEFAULT 0,
                            DateAdded DATETIME DEFAULT CURRENT_TIMESTAMP,
                            PitchSemitones INTEGER DEFAULT 0,
                            TrimStart INTEGER DEFAULT 0,
                            TrimEnd INTEGER DEFAULT -1,
                            IsFavorite INTEGER DEFAULT 0,
                            PlayCount INTEGER DEFAULT 0,
                            LastPlayed DATETIME,
                            AlbumArtUrl TEXT,
                            MetadataFetched INTEGER DEFAULT 0
                        )";

                    using (var cmd = new SQLiteCommand(createSongsTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create index for faster queries
                    string createIndex = @"CREATE INDEX IF NOT EXISTS idx_songs_title ON Songs(Title)";
                    using (var cmd = new SQLiteCommand(createIndex, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    createIndex = @"CREATE INDEX IF NOT EXISTS idx_songs_artist ON Songs(Artist)";
                    using (var cmd = new SQLiteCommand(createIndex, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        public void AddSong(Song song)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        INSERT OR REPLACE INTO Songs 
                        (Title, Artist, Album, FilePath, FolderPath, FolderName, Duration, 
                         FileSize, DateAdded, IsFavorite, PlayCount)
                        VALUES (@title, @artist, @album, @filePath, @folderPath, @folderName, 
                               @duration, @fileSize, @dateAdded, @isFavorite, @playCount)";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@title", song.Title ?? string.Empty);
                        cmd.Parameters.AddWithValue("@artist", song.Artist ?? string.Empty);
                        cmd.Parameters.AddWithValue("@album", song.Album ?? string.Empty);
                        cmd.Parameters.AddWithValue("@filePath", song.FilePath ?? string.Empty);
                        cmd.Parameters.AddWithValue("@folderPath", song.FolderPath ?? string.Empty);
                        cmd.Parameters.AddWithValue("@folderName", song.FolderName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@duration", song.Duration);
                        cmd.Parameters.AddWithValue("@fileSize", song.FileSize);
                        cmd.Parameters.AddWithValue("@dateAdded", song.DateAdded);
                        cmd.Parameters.AddWithValue("@isFavorite", song.IsFavorite ? 1 : 0);
                        cmd.Parameters.AddWithValue("@playCount", song.PlayCount);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddSong error: {ex.Message}");
            }
        }

        public List<Song> GetAllSongs()
        {
            var songs = new List<Song>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Songs ORDER BY Title ASC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(MapReaderToSong(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllSongs error: {ex.Message}");
            }

            return songs;
        }

        public List<Song> GetFavoriteSongs()
        {
            var songs = new List<Song>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "SELECT * FROM Songs WHERE IsFavorite = 1 ORDER BY Title ASC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(MapReaderToSong(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetFavoriteSongs error: {ex.Message}");
            }

            return songs;
        }

        public List<Song> SearchSongs(string query)
        {
            var songs = new List<Song>();
            string searchPattern = $"%{query}%";

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string sql = @"
                        SELECT * FROM Songs 
                        WHERE Title LIKE @query OR Artist LIKE @query OR Album LIKE @query
                        ORDER BY Title ASC";

                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@query", searchPattern);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                songs.Add(MapReaderToSong(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchSongs error: {ex.Message}");
            }

            return songs;
        }

        public void UpdateSongFavorite(long songId, bool isFavorite)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = "UPDATE Songs SET IsFavorite = @isFavorite WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@isFavorite", isFavorite ? 1 : 0);
                        cmd.Parameters.AddWithValue("@id", songId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSongFavorite error: {ex.Message}");
            }
        }

        public void UpdatePlayCount(long songId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    const string query = @"
                        UPDATE Songs 
                        SET PlayCount = PlayCount + 1, LastPlayed = @lastPlayed 
                        WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@lastPlayed", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", songId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePlayCount error: {ex.Message}");
            }
        }

        public int GetTotalSongCount()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Songs", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTotalSongCount error: {ex.Message}");
                return 0;
            }
        }

        public void ClearLibrary()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM Songs", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearLibrary error: {ex.Message}");
            }
        }

        private Song MapReaderToSong(SQLiteDataReader reader)
        {
            return new Song
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? string.Empty : reader.GetString(reader.GetOrdinal("Title")),
                Artist = reader.IsDBNull(reader.GetOrdinal("Artist")) ? string.Empty : reader.GetString(reader.GetOrdinal("Artist")),
                Album = reader.IsDBNull(reader.GetOrdinal("Album")) ? string.Empty : reader.GetString(reader.GetOrdinal("Album")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FolderPath = reader.IsDBNull(reader.GetOrdinal("FolderPath")) ? string.Empty : reader.GetString(reader.GetOrdinal("FolderPath")),
                FolderName = reader.IsDBNull(reader.GetOrdinal("FolderName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FolderName")),
                Duration = reader.GetInt64(reader.GetOrdinal("Duration")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                DateAdded = reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                IsFavorite = reader.GetInt32(reader.GetOrdinal("IsFavorite")) == 1,
                PlayCount = reader.GetInt32(reader.GetOrdinal("PlayCount")),
                MetadataFetched = reader.GetInt32(reader.GetOrdinal("MetadataFetched")) == 1
            };
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}

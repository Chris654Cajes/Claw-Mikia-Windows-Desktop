using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicVault.Models;

namespace MusicVault.Services
{
    /// <summary>
    /// Handles music library management, folder scanning, and metadata extraction
    /// </summary>
    public class LibraryService
    {
        private readonly DatabaseService _databaseService;
        private readonly string[] _supportedExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" };

        public event EventHandler<string>? ScanProgress;
        public event EventHandler? ScanComplete;

        public LibraryService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public void ScanFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                System.Diagnostics.Debug.WriteLine($"Folder does not exist: {folderPath}");
                return;
            }

            try
            {
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                int processed = 0;

                foreach (var filePath in files)
                {
                    try
                    {
                        var song = ExtractMetadata(filePath);
                        _databaseService.AddSong(song);
                        processed++;
                        ScanProgress?.Invoke(this, $"Scanned {processed}/{files.Count} files...");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error scanning {filePath}: {ex.Message}");
                    }
                }

                ScanComplete?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ScanFolder error: {ex.Message}");
            }
        }

        private Song ExtractMetadata(string filePath)
        {
            var song = new Song
            {
                FilePath = filePath,
                FolderPath = Path.GetDirectoryName(filePath) ?? string.Empty,
                FolderName = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? string.Empty).Name,
                DateAdded = DateTime.Now
            };

            try
            {
                var file = TagLib.File.Create(filePath);
                song.Title = !string.IsNullOrEmpty(file.Tag.Title) 
                    ? file.Tag.Title 
                    : Path.GetFileNameWithoutExtension(filePath);
                    
                song.Artist = !string.IsNullOrEmpty(file.Tag.FirstPerformer) 
                    ? file.Tag.FirstPerformer 
                    : "Unknown Artist";
                    
                song.Album = !string.IsNullOrEmpty(file.Tag.Album) 
                    ? file.Tag.Album 
                    : "Unknown Album";
                    
                song.Duration = (long)file.Properties.Duration.TotalMilliseconds;
                song.MetadataFetched = true;
            }
            catch
            {
                song.Title = Path.GetFileNameWithoutExtension(filePath);
                song.Artist = "Unknown Artist";
                song.Album = "Unknown Album";
                song.Duration = 0;
            }

            var fileInfo = new FileInfo(filePath);
            song.FileSize = fileInfo.Length;

            return song;
        }

        public List<string> GetFoldersList()
        {
            var songs = _databaseService.GetAllSongs();
            return songs
                .Where(s => !string.IsNullOrEmpty(s.FolderPath))
                .Select(s => s.FolderPath)
                .Distinct()
                .OrderBy(f => f)
                .ToList();
        }

        public List<Song> GetSongsInFolder(string folderPath)
        {
            return _databaseService.GetAllSongs()
                .Where(s => s.FolderPath == folderPath)
                .OrderBy(s => s.Title)
                .ToList();
        }

        public int GetFolderSongCount(string folderPath)
        {
            return GetSongsInFolder(folderPath).Count;
        }
    }
}

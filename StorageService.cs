using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MusicVault.Models;

namespace MusicVault.Services
{
    public static class StorageService
    {
        private static readonly string FilePath = "songs.json";

        public static void Save(List<Song> songs)
        {
            var json = JsonSerializer.Serialize(songs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }

        public static List<Song> Load()
        {
            if (!File.Exists(FilePath))
                return new List<Song>();

            var json = File.ReadAllText(FilePath);

            return JsonSerializer.Deserialize<List<Song>>(json)
                   ?? new List<Song>();
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
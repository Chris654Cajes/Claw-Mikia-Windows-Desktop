using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using MusicVault.Models;
using System.Linq;

namespace MusicVault.Services
{
    /// <summary>
    /// Service to fetch album metadata and artwork from the internet
    /// </summary>
    public class MetadataService
    {
        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Checks if an internet connection is available
        /// </summary>
        public static bool IsInternetAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Automatically updates song metadata if connected to the internet
        /// </summary>
        public async Task<bool> UpdateAlbumMetadataAsync(Song song)
        {
            if (!IsInternetAvailable()) return false;
            if (string.IsNullOrEmpty(song.Artist) || song.Artist == "Unknown Artist") return false;

            try
            {
                // Search for the album using iTunes Search API
                string searchTerm = Uri.EscapeDataString($"{song.Artist} {song.Album}");
                string url = $"https://itunes.apple.com/search?term={searchTerm}&entity=album&limit=1";

                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root.TryGetProperty("resultCount", out var count) && count.GetInt32() > 0)
                {
                    var result = root.GetProperty("results")[0];

                    // Update album info if it was unknown
                    if (song.Album == "Unknown Album" && result.TryGetProperty("collectionName", out var albumName))
                    {
                        song.Album = albumName.GetString() ?? song.Album;
                    }

                    // Fetch high-quality artwork (iTunes provides 100x100, we want higher)
                    if (result.TryGetProperty("artworkUrl100", out var artUrl))
                    {
                        string url100 = artUrl.GetString() ?? string.Empty;
                        // Replace 100x100 with 600x600 for better quality
                        song.AlbumArtUrl = url100.Replace("100x100bb.jpg", "600x600bb.jpg");
                        song.MetadataFetched = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Metadata update error for {song.Title}: {ex.Message}");
            }

            return false;
        }
    }
}

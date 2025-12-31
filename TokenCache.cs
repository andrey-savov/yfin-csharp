using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YahooFinanceDownloader;

/// <summary>
/// Manages caching of Yahoo Finance authentication tokens to avoid repeated authentication
/// </summary>
public class TokenCache
{
    private const string CacheFileName = ".yahoo_token_cache.json";
    private static readonly string CacheFilePath = GetCachePath();

    private static string GetCachePath()
    {
        // Try to find project root by looking for .csproj file
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = currentDir;

        // Search up to 3 levels up for project root
        for (int i = 0; i < 3; i++)
        {
            if (Directory.GetFiles(searchDir, "*.csproj").Length > 0 ||
                Directory.GetFiles(searchDir, "*.sln").Length > 0)
            {
                return Path.Combine(searchDir, CacheFileName);
            }

            var parent = Directory.GetParent(searchDir);
            if (parent == null) break;
            searchDir = parent.FullName;
        }

        // Fallback to current directory
        return Path.Combine(currentDir, CacheFileName);
    }

    /// <summary>
    /// Represents cached authentication data
    /// </summary>
    private class CachedAuth
    {
        [JsonPropertyName("crumb")]
        public string Crumb { get; set; } = "";

        [JsonPropertyName("cookies")]
        public List<CachedCookie> Cookies { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }

    private class CachedCookie
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = "";

        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("expires")]
        public DateTime? Expires { get; set; }
    }

    /// <summary>
    /// Save authentication data to cache file
    /// </summary>
    public static void Save(string crumb, CookieContainer cookieContainer, TimeSpan validFor)
    {
        try
        {
            var cachedAuth = new CachedAuth
            {
                Crumb = crumb,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(validFor),
                Cookies = new List<CachedCookie>()
            };

            // Extract cookies from container
            var cookies = cookieContainer.GetAllCookies();
            foreach (Cookie cookie in cookies)
            {
                cachedAuth.Cookies.Add(new CachedCookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Expires = cookie.Expires == DateTime.MinValue ? null : cookie.Expires
                });
            }

            var json = JsonSerializer.Serialize(cachedAuth, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(CacheFilePath, json);
            Console.WriteLine($"  ✓ Authentication cached (valid for {validFor.TotalHours:F1} hours)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Failed to cache authentication: {ex.Message}");
            // Don't fail if caching doesn't work
        }
    }

    /// <summary>
    /// Try to load cached authentication data
    /// Returns true if valid cached data was found and loaded
    /// </summary>
    public static bool TryLoad(out string? crumb, out CookieContainer? cookieContainer)
    {
        crumb = null;
        cookieContainer = null;

        try
        {
            if (!File.Exists(CacheFilePath))
            {
                Console.WriteLine("  - No cached authentication found");
                return false;
            }

            var json = File.ReadAllText(CacheFilePath);
            var cachedAuth = JsonSerializer.Deserialize<CachedAuth>(json);

            if (cachedAuth == null)
            {
                Console.WriteLine("  - Invalid cache file");
                return false;
            }

            // Check if cache is still valid
            if (DateTime.UtcNow >= cachedAuth.ExpiresAt)
            {
                Console.WriteLine($"  - Cached authentication expired at {cachedAuth.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                Delete(); // Clean up expired cache
                return false;
            }

            // Restore cookies
            cookieContainer = new CookieContainer();
            foreach (var cachedCookie in cachedAuth.Cookies)
            {
                var cookie = new Cookie(
                    cachedCookie.Name,
                    cachedCookie.Value,
                    cachedCookie.Path,
                    cachedCookie.Domain);

                if (cachedCookie.Expires.HasValue)
                {
                    cookie.Expires = cachedCookie.Expires.Value;
                }

                cookieContainer.Add(cookie);
            }

            crumb = cachedAuth.Crumb;

            var timeRemaining = cachedAuth.ExpiresAt - DateTime.UtcNow;
            Console.WriteLine($"  ✓ Loaded cached authentication (valid for {timeRemaining.TotalHours:F1} more hours)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Failed to load cache: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete the cache file
    /// </summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                File.Delete(CacheFilePath);
                Console.WriteLine("  - Cache deleted");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Failed to delete cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if cache exists and is valid
    /// </summary>
    public static bool IsValid()
    {
        try
        {
            if (!File.Exists(CacheFilePath))
            {
                return false;
            }

            var json = File.ReadAllText(CacheFilePath);
            var cachedAuth = JsonSerializer.Deserialize<CachedAuth>(json);

            return cachedAuth != null && DateTime.UtcNow < cachedAuth.ExpiresAt;
        }
        catch
        {
            return false;
        }
    }
}

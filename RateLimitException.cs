namespace YahooFinanceDownloader;

/// <summary>
/// Custom exception for rate limiting
/// </summary>
public class RateLimitException : Exception
{
    public RateLimitException(string message) : base(message) { }
}

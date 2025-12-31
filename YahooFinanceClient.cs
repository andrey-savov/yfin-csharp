using System.Net;
using System.Text.Json;
using HtmlAgilityPack;

namespace YahooFinanceDownloader;

public class YahooFinanceClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private string? _crumb;

    public YahooFinanceClient()
    {
        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Authenticate using Selenium WebDriver (bypasses TLS fingerprinting)
    /// Uses cached tokens if available and valid
    /// </summary>
    public async Task<bool> AuthenticateAsync(bool headless = true, bool useCache = true)
    {
        // Try to use cached authentication first
        if (useCache && TokenCache.TryLoad(out var cachedCrumb, out var cachedCookies))
        {
            _crumb = cachedCrumb;
            if (cachedCookies != null)
            {
                _cookieContainer.Add(cachedCookies.GetAllCookies());
                return true;
            }
        }

        try
        {
            using var seleniumAuth = new SeleniumAuthenticator();
            var (cookies, crumb) = await seleniumAuth.AuthenticateAsync(headless);

            // Transfer cookies from Selenium to HttpClient
            _cookieContainer.Add(cookies.GetAllCookies());
            _crumb = crumb;

            Console.WriteLine("  ✓ Cookies transferred to HTTP client");

            // Cache the authentication for 12 hours
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(cookies.GetAllCookies());
            TokenCache.Save(crumb, cookieContainer, TimeSpan.FromHours(12));

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Selenium authentication failed: {ex.Message}");
            Console.WriteLine("\nTroubleshooting:");
            Console.WriteLine("  1. Ensure Chrome browser is installed");
            Console.WriteLine("  2. ChromeDriver will be downloaded automatically");
            Console.WriteLine("  3. Check your internet connection");
            return false;
        }
    }

    /// <summary>
    /// Fallback authentication using CSRF strategy
    /// </summary>
    private async Task<bool> AuthenticateCsrfAsync()
    {
        try
        {
            Console.WriteLine("  - Using CSRF authentication strategy...");

            // Step 1: Get consent form
            Console.WriteLine("  - Fetching consent form...");
            var consentResponse = await _httpClient.GetAsync("https://guce.yahoo.com/consent");
            var html = await consentResponse.Content.ReadAsStringAsync();

            // Parse HTML to extract csrfToken and sessionId
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var csrfTokenInput = doc.DocumentNode.SelectSingleNode("//input[@name='csrfToken']");
            var sessionIdInput = doc.DocumentNode.SelectSingleNode("//input[@name='sessionId']");

            if (csrfTokenInput == null || sessionIdInput == null)
            {
                Console.WriteLine("  - Failed to parse consent form");
                return false;
            }

            var csrfToken = csrfTokenInput.GetAttributeValue("value", "");
            var sessionId = sessionIdInput.GetAttributeValue("value", "");

            Console.WriteLine($"  - Extracted sessionId: {sessionId}");

            // Step 2: Submit consent form
            Console.WriteLine("  - Submitting consent...");
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "agree", "agree" },
                { "consentUUID", "default" },
                { "sessionId", sessionId },
                { "csrfToken", csrfToken },
                { "originalDoneUrl", "https://finance.yahoo.com/" },
                { "namespace", "yahoo" }
            });

            await _httpClient.PostAsync(
                $"https://consent.yahoo.com/v2/collectConsent?sessionId={sessionId}",
                formData);

            // Step 3: Copy consent
            Console.WriteLine("  - Copying consent...");
            await _httpClient.GetAsync(
                $"https://guce.yahoo.com/copyConsent?sessionId={sessionId}");

            // Step 4: Get crumb
            Console.WriteLine("  - Fetching crumb...");
            var crumbResponse = await _httpClient.GetAsync(
                "https://query2.finance.yahoo.com/v1/test/getcrumb");

            _crumb = await crumbResponse.Content.ReadAsStringAsync();

            var success = !_crumb.Contains("<html>");
            if (success)
            {
                Console.WriteLine($"  - CSRF authentication successful! Crumb: {_crumb}");
            }
            else
            {
                Console.WriteLine("  - CSRF authentication failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CSRF authentication failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fetch historical price data for a ticker with automatic retry on rate limit
    /// </summary>
    public async Task<List<PriceBar>> GetHistoricalPricesAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate,
        string interval = "1d",
        int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await GetHistoricalPricesInternalAsync(ticker, startDate, endDate, interval);
            }
            catch (RateLimitException)
            {
                if (attempt == maxRetries)
                {
                    throw;
                }

                var delaySeconds = (int)Math.Pow(2, attempt) * 30; // 60s, 120s, 240s
                Console.WriteLine($"Rate limited. Waiting {delaySeconds} seconds before retry {attempt + 1}/{maxRetries}...");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Unauthorized - re-authenticating...");
                await AuthenticateAsync();
            }
        }

        throw new Exception($"Failed after {maxRetries} attempts");
    }

    /// <summary>
    /// Internal method to fetch historical price data for a ticker
    /// </summary>
    private async Task<List<PriceBar>> GetHistoricalPricesInternalAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate,
        string interval = "1d")
    {
        if (string.IsNullOrEmpty(_crumb))
        {
            var authSuccess = await AuthenticateAsync();
            if (!authSuccess)
            {
                throw new Exception("Authentication failed");
            }
        }

        // Convert dates to Unix timestamps (seconds)
        var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeSeconds();
        var endTimestamp = new DateTimeOffset(endDate).ToUnixTimeSeconds();

        // Build URL with query parameters
        var url = $"https://query2.finance.yahoo.com/v8/finance/chart/{ticker}" +
                  $"?period1={startTimestamp}" +
                  $"&period2={endTimestamp}" +
                  $"&interval={interval}" +
                  $"&includePrePost=false" +
                  $"&events=div,splits,capitalGains" +
                  $"&crumb={_crumb}";

        Console.WriteLine($"\nFetching data from Yahoo Finance...");
        Console.WriteLine($"  Ticker: {ticker}");
        Console.WriteLine($"  Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        Console.WriteLine($"  Interval: {interval}");

        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new RateLimitException("Yahoo Finance rate limit exceeded");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return ParseHistoricalData(json);
    }

    /// <summary>
    /// Parse JSON response into list of PriceBar objects
    /// </summary>
    private List<PriceBar> ParseHistoricalData(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Check for errors
        var chartElement = root.GetProperty("chart");
        var errorElement = chartElement.GetProperty("error");

        if (errorElement.ValueKind != JsonValueKind.Null)
        {
            var errorMsg = errorElement.GetProperty("description").GetString();
            throw new Exception($"Yahoo Finance error: {errorMsg}");
        }

        // Get result object
        var resultArray = chartElement.GetProperty("result");
        if (resultArray.GetArrayLength() == 0)
        {
            throw new Exception("No data returned from Yahoo Finance");
        }

        var result = resultArray[0];

        // Extract timestamps
        var timestamps = result.GetProperty("timestamp")
            .EnumerateArray()
            .Select(t => DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).DateTime)
            .ToArray();

        Console.WriteLine($"  Received {timestamps.Length} data points");

        // Extract quote data (OHLCV)
        var quote = result.GetProperty("indicators")
                         .GetProperty("quote")[0];

        var opens = GetDecimalArray(quote, "open");
        var highs = GetDecimalArray(quote, "high");
        var lows = GetDecimalArray(quote, "low");
        var closes = GetDecimalArray(quote, "close");
        var volumes = GetLongArray(quote, "volume");

        // Extract adjusted close prices
        var adjCloses = GetDecimalArray(
            result.GetProperty("indicators").GetProperty("adjclose")[0],
            "adjclose");

        // Combine into PriceBar objects
        var priceBars = new List<PriceBar>();
        for (int i = 0; i < timestamps.Length; i++)
        {
            priceBars.Add(new PriceBar
            {
                Date = timestamps[i],
                Open = opens[i],
                High = highs[i],
                Low = lows[i],
                Close = closes[i],
                AdjustedClose = adjCloses[i],
                Volume = volumes[i]
            });
        }

        return priceBars;
    }

    // Helper methods for parsing JSON arrays with null handling
    private decimal?[] GetDecimalArray(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName)
            .EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Null ? (decimal?)null : e.GetDecimal())
            .ToArray();
    }

    private long?[] GetLongArray(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName)
            .EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Null ? (long?)null : e.GetInt64())
            .ToArray();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

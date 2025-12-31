using YahooFinanceDownloader;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Yahoo Finance Historical Price Downloader");
        Console.WriteLine("=========================================\n");

        // Parse command-line arguments
        if (!TryParseArguments(args, out var ticker, out var startDate, out var endDate, out var interval, out var outputFile))
        {
            PrintUsage();
            return 1;
        }

        try
        {
            using var client = new YahooFinanceClient();

            // Authenticate
            var authSuccess = await client.AuthenticateAsync();
            if (!authSuccess)
            {
                Console.WriteLine("\nERROR: Authentication failed!");
                Console.WriteLine("This may be due to Yahoo's bot detection.");
                Console.WriteLine("Try running the program again in a few minutes.");
                return 1;
            }

            // Fetch historical prices
            var prices = await client.GetHistoricalPricesAsync(ticker, startDate, endDate, interval);

            // Display results
            Console.WriteLine($"\n{'=',-60}");
            Console.WriteLine($"Successfully retrieved {prices.Count} price bars");
            Console.WriteLine($"{'=',-60}\n");

            if (prices.Count > 0)
            {
                // Show first 10 and last 5 entries
                Console.WriteLine("First 10 entries:");
                foreach (var price in prices.Take(10))
                {
                    Console.WriteLine($"  {price}");
                }

                if (prices.Count > 15)
                {
                    Console.WriteLine($"  ... ({prices.Count - 15} more entries) ...");
                    Console.WriteLine("\nLast 5 entries:");
                    foreach (var price in prices.Skip(prices.Count - 5))
                    {
                        Console.WriteLine($"  {price}");
                    }
                }

                // Calculate and display statistics
                DisplayStatistics(ticker, prices);

                // Save to file if specified
                if (!string.IsNullOrEmpty(outputFile))
                {
                    SaveToCsv(prices, outputFile);
                    Console.WriteLine($"\n✓ Data saved to: {outputFile}");
                }
            }
            else
            {
                Console.WriteLine("No price data returned.");
            }

            return 0;
        }
        catch (RateLimitException ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            Console.WriteLine("Please wait a few minutes and try again.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Details: {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    static bool TryParseArguments(
        string[] args,
        out string ticker,
        out DateTime startDate,
        out DateTime endDate,
        out string interval,
        out string? outputFile)
    {
        ticker = "";
        startDate = DateTime.MinValue;
        endDate = DateTime.Now;
        interval = "1d";
        outputFile = null;

        // Parse named arguments
        var argDict = new Dictionary<string, string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") && i + 1 < args.Length)
            {
                argDict[args[i]] = args[i + 1];
                i++; // Skip next arg
            }
        }

        // Required: ticker
        if (!argDict.TryGetValue("--ticker", out var tickerValue) || string.IsNullOrWhiteSpace(tickerValue))
        {
            Console.WriteLine("ERROR: --ticker is required");
            return false;
        }

        ticker = tickerValue.ToUpper();

        // Optional: start date (default: 1 year ago)
        if (argDict.TryGetValue("--start", out var startStr))
        {
            if (!DateTime.TryParse(startStr, out startDate))
            {
                Console.WriteLine($"ERROR: Invalid start date format: {startStr}");
                Console.WriteLine("Use format: YYYY-MM-DD");
                return false;
            }
        }
        else
        {
            startDate = DateTime.Now.AddYears(-1);
        }

        // Optional: end date (default: today)
        if (argDict.TryGetValue("--end", out var endStr))
        {
            if (!DateTime.TryParse(endStr, out endDate))
            {
                Console.WriteLine($"ERROR: Invalid end date format: {endStr}");
                Console.WriteLine("Use format: YYYY-MM-DD");
                return false;
            }
        }

        // Optional: interval (default: 1d)
        if (argDict.TryGetValue("--interval", out var intervalStr))
        {
            interval = intervalStr.ToLower();
            var validIntervals = new[] { "1m", "2m", "5m", "15m", "30m", "60m", "1h", "90m", "1d", "5d", "1wk", "1mo", "3mo" };
            if (!validIntervals.Contains(interval))
            {
                Console.WriteLine($"ERROR: Invalid interval: {interval}");
                Console.WriteLine($"Valid intervals: {string.Join(", ", validIntervals)}");
                return false;
            }
        }

        // Optional: output file
        if (argDict.TryGetValue("--output", out outputFile))
        {
            // Ensure it has .csv extension
            if (!outputFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                outputFile += ".csv";
            }
        }
        else
        {
            // Default output file name
            outputFile = $"{ticker}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
        }

        // Validate date range
        if (startDate >= endDate)
        {
            Console.WriteLine("ERROR: Start date must be before end date");
            return false;
        }

        return true;
    }

    static void PrintUsage()
    {
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  YahooFinanceDownloader --ticker <SYMBOL> [OPTIONS]\n");
        Console.WriteLine("Required Arguments:");
        Console.WriteLine("  --ticker <SYMBOL>      Stock ticker symbol (e.g., AAPL, MSFT, ^GSPC)\n");
        Console.WriteLine("Optional Arguments:");
        Console.WriteLine("  --start <DATE>         Start date in YYYY-MM-DD format (default: 1 year ago)");
        Console.WriteLine("  --end <DATE>           End date in YYYY-MM-DD format (default: today)");
        Console.WriteLine("  --interval <INTERVAL>  Time interval (default: 1d)");
        Console.WriteLine("                         Valid: 1m, 2m, 5m, 15m, 30m, 60m, 1h, 90m, 1d, 5d, 1wk, 1mo, 3mo");
        Console.WriteLine("  --output <FILE>        Output CSV file (default: <ticker>_<start>_<end>.csv)\n");
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Download 1 year of daily data for Apple");
        Console.WriteLine("  YahooFinanceDownloader --ticker AAPL\n");
        Console.WriteLine("  # Download specific date range");
        Console.WriteLine("  YahooFinanceDownloader --ticker MSFT --start 2023-01-01 --end 2024-01-01\n");
        Console.WriteLine("  # Download hourly data for the last 30 days");
        Console.WriteLine("  YahooFinanceDownloader --ticker GOOGL --start 2024-12-01 --interval 1h\n");
        Console.WriteLine("  # Download and save to specific file");
        Console.WriteLine("  YahooFinanceDownloader --ticker TSLA --output tesla_prices.csv\n");
    }

    static void DisplayStatistics(string ticker, List<PriceBar> prices)
    {
        var validPrices = prices.Where(p => p.High.HasValue && p.Low.HasValue && p.Volume.HasValue).ToList();

        if (validPrices.Count == 0)
        {
            Console.WriteLine("\nNo valid data for statistics calculation.");
            return;
        }

        var avgVolume = validPrices.Average(p => p.Volume!.Value);
        var maxPrice = validPrices.Max(p => p.High!.Value);
        var minPrice = validPrices.Min(p => p.Low!.Value);
        var startPrice = validPrices.First().Close ?? validPrices.First().AdjustedClose;
        var endPrice = validPrices.Last().Close ?? validPrices.Last().AdjustedClose;

        Console.WriteLine($"\n{'=',-60}");
        Console.WriteLine($"Statistics for {ticker}");
        Console.WriteLine($"{'=',-60}");

        if (startPrice.HasValue && endPrice.HasValue)
        {
            var change = endPrice.Value - startPrice.Value;
            var changePercent = (change / startPrice.Value) * 100;
            Console.WriteLine($"  Period Return:       {change:+0.00;-0.00} ({changePercent:+0.00;-0.00}%)");
            Console.WriteLine($"  Start Price:         ${startPrice:F2}");
            Console.WriteLine($"  End Price:           ${endPrice:F2}");
        }

        Console.WriteLine($"  Period High:         ${maxPrice:F2}");
        Console.WriteLine($"  Period Low:          ${minPrice:F2}");
        Console.WriteLine($"  Price Range:         ${maxPrice - minPrice:F2}");
        Console.WriteLine($"  Average Volume:      {avgVolume:N0}");
        Console.WriteLine($"  Total Trading Days:  {validPrices.Count:N0}");
    }

    static void SaveToCsv(List<PriceBar> prices, string filename)
    {
        using var writer = new StreamWriter(filename);

        // Write header
        writer.WriteLine(PriceBar.CsvHeader());

        // Write data rows
        foreach (var price in prices)
        {
            writer.WriteLine(price.ToCsv());
        }
    }
}

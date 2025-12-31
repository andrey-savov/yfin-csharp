namespace YahooFinanceDownloader;

/// <summary>
/// Represents a single price bar (OHLCV data point)
/// </summary>
public class PriceBar
{
    public DateTime Date { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Close { get; set; }
    public decimal? AdjustedClose { get; set; }
    public long? Volume { get; set; }

    public override string ToString()
    {
        return $"{Date:yyyy-MM-dd} | " +
               $"O:{(Open?.ToString("F2") ?? "N/A")} | " +
               $"H:{(High?.ToString("F2") ?? "N/A")} | " +
               $"L:{(Low?.ToString("F2") ?? "N/A")} | " +
               $"C:{(Close?.ToString("F2") ?? "N/A")} | " +
               $"Adj:{(AdjustedClose?.ToString("F2") ?? "N/A")} | " +
               $"Vol:{(Volume?.ToString("N0") ?? "N/A")}";
    }

    public string ToCsv()
    {
        return $"{Date:yyyy-MM-dd},{Open},{High},{Low},{Close},{AdjustedClose},{Volume}";
    }

    public static string CsvHeader()
    {
        return "Date,Open,High,Low,Close,AdjustedClose,Volume";
    }
}

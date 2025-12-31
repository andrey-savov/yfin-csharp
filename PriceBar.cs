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
        return $"{Date:yyyy-MM-dd} | O:{Open:F2} | H:{High:F2} | L:{Low:F2} | C:{Close:F2} | Adj:{AdjustedClose:F2} | Vol:{Volume:N0}";
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

# Yahoo Finance Historical Price Downloader

A .NET Core console application that downloads historical stock prices from Yahoo Finance without requiring an API key.

## Features

- Download historical OHLCV (Open, High, Low, Close, Volume) data
- Support for multiple time intervals (1m, 1h, 1d, 1wk, 1mo, etc.)
- Automatic authentication with Yahoo Finance (cookie + crumb mechanism)
- Automatic retry with exponential backoff on rate limiting
- CSV export functionality
- Statistical summary of price data

## Prerequisites

- .NET 8.0 SDK or later
- Internet connection

## Building the Application

```bash
# If .NET SDK is installed
dotnet build

# Or use Visual Studio to open the .csproj file
```

## Usage

### Basic Usage

Download 1 year of daily data for a ticker:

```bash
YahooFinanceDownloader --ticker AAPL
```

### Specify Date Range

```bash
YahooFinanceDownloader --ticker MSFT --start 2023-01-01 --end 2024-01-01
```

### Download Intraday Data

```bash
# Hourly data for the last 30 days
YahooFinanceDownloader --ticker GOOGL --start 2024-12-01 --interval 1h

# 5-minute data for the last 7 days
YahooFinanceDownloader --ticker TSLA --start 2024-12-24 --interval 5m
```

### Specify Output File

```bash
YahooFinanceDownloader --ticker AAPL --output apple_prices.csv
```

## Command-Line Arguments

### Required

- `--ticker <SYMBOL>` - Stock ticker symbol (e.g., AAPL, MSFT, ^GSPC for S&P 500)

### Optional

- `--start <DATE>` - Start date in YYYY-MM-DD format (default: 1 year ago)
- `--end <DATE>` - End date in YYYY-MM-DD format (default: today)
- `--interval <INTERVAL>` - Time interval (default: 1d)
  - Valid intervals: `1m`, `2m`, `5m`, `15m`, `30m`, `60m`, `1h`, `90m`, `1d`, `5d`, `1wk`, `1mo`, `3mo`
- `--output <FILE>` - Output CSV file (default: `<ticker>_<start>_<end>.csv`)

## Output Format

The application outputs a CSV file with the following columns:

```
Date,Open,High,Low,Close,AdjustedClose,Volume
2024-01-02,185.64,186.10,184.00,185.64,184.89,50985600
2024-01-03,184.35,186.40,183.00,184.25,183.51,58414400
...
```

## Features Details

### Authentication

The application automatically handles Yahoo Finance authentication using two strategies:

1. **Basic Strategy** (default): Fetches session cookie from `fc.yahoo.com` and crumb token
2. **CSRF Strategy** (fallback): Uses Yahoo's consent form mechanism

### Rate Limiting

Yahoo Finance enforces rate limits. The application automatically:

- Detects rate limiting (HTTP 429 responses)
- Implements exponential backoff (60s, 120s, 240s delays)
- Retries failed requests up to 3 times

### Error Handling

The application handles common errors:

- Invalid ticker symbols
- Authentication failures
- Network errors
- Rate limiting
- Missing data points (null values)

## Important Notes

### Data Availability

- **Intraday data** (1m, 2m, 5m, etc.) is limited to the last 60 days
- **Hourly data** (1h) is available for ~730 days
- **Daily data** (1d) is available for ~99 years
- Some tickers may have gaps or missing data

### Yahoo Finance Limitations

- No official API documentation
- Rate limits are not publicly documented (~2000 requests/hour estimated)
- Data is for personal, non-commercial use only
- The service may change without notice

### Known Issues

- **TLS Fingerprinting**: Yahoo may detect and block standard HTTP clients. This implementation works for most cases but may occasionally fail due to bot detection.
- **30-minute interval bug**: Yahoo has a known bug with the `30m` interval. Use `15m` instead.

## Technical Details

This application implements the Yahoo Finance API methods documented in `~/yfinance-methods.md`:

- Cookie-based authentication (no API key required)
- Historical prices endpoint: `/v8/finance/chart/{ticker}`
- JSON response parsing with null-safe handling
- Retry logic with exponential backoff
- CSV export functionality

## Dependencies

- **HtmlAgilityPack** (1.11.54) - For HTML parsing in CSRF authentication
- **.NET 8.0** - Target framework

## Troubleshooting

### "Authentication failed"

Yahoo's bot detection may be blocking requests. Try:

1. Wait a few minutes and retry
2. Use a different network connection
3. Ensure no other programs are making excessive Yahoo Finance requests

### "Rate limit exceeded"

You've made too many requests. Wait 1-2 hours before retrying.

### "No data found"

- Verify the ticker symbol is correct
- Check that you're using a valid date range for the interval
- Some tickers may be delisted or have limited data

### "Invalid ticker symbol"

Use the correct format:
- Stocks: `AAPL`, `MSFT`, `GOOGL`
- Indices: `^GSPC` (S&P 500), `^DJI` (Dow Jones)
- ETFs: `SPY`, `QQQ`
- Crypto: `BTC-USD`, `ETH-USD`

## Legal Disclaimer

This tool is for **educational and personal use only**. Yahoo Finance data is subject to Yahoo's Terms of Service. For commercial use, please use official data providers or Yahoo's paid API services.

## License

This project is provided as-is for educational purposes.

## References

- Based on the methods documented in `~/yfinance-methods.md`
- Inspired by the yfinance Python library

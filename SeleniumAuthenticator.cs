using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net;

namespace YahooFinanceDownloader;

/// <summary>
/// Uses Selenium WebDriver to authenticate with Yahoo Finance via a real browser
/// This bypasses TLS fingerprinting detection
/// </summary>
public class SeleniumAuthenticator : IDisposable
{
    private IWebDriver? _driver;
    private bool _disposed;

    /// <summary>
    /// Authenticate with Yahoo Finance using Selenium Chrome WebDriver
    /// Returns cookies and crumb token
    /// </summary>
    public async Task<(CookieContainer cookies, string crumb)> AuthenticateAsync(bool headless = true)
    {
        Console.WriteLine("Starting Selenium WebDriver authentication...");

        try
        {
            // Configure Chrome options
            var options = new ChromeOptions();

            if (headless)
            {
                options.AddArgument("--headless=new");
                Console.WriteLine("  - Running in headless mode");
            }

            // Standard options to appear more like a normal browser
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

            // Exclude automation flags
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            Console.WriteLine("  - Initializing Chrome WebDriver...");
            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);

            // Step 1: Navigate to Yahoo Finance to get session cookie
            Console.WriteLine("  - Navigating to Yahoo Finance...");
            _driver.Navigate().GoToUrl("https://finance.yahoo.com");

            // Wait a moment for page to load
            await Task.Delay(2000);

            // Check if we need to handle consent form
            await HandleConsentFormIfPresent();

            // Step 2: Navigate to a ticker page to ensure cookies are set
            Console.WriteLine("  - Loading ticker page to establish session...");
            _driver.Navigate().GoToUrl("https://finance.yahoo.com/quote/AAPL");
            await Task.Delay(1500);

            // Step 3: Get cookies from Selenium
            Console.WriteLine("  - Extracting cookies from browser...");
            var cookieContainer = new CookieContainer();
            var seleniumCookies = _driver.Manage().Cookies.AllCookies;

            foreach (var cookie in seleniumCookies)
            {
                try
                {
                    var netCookie = new System.Net.Cookie(
                        cookie.Name,
                        cookie.Value,
                        cookie.Path,
                        cookie.Domain
                    );

                    if (cookie.Expiry.HasValue)
                    {
                        netCookie.Expires = cookie.Expiry.Value;
                    }

                    cookieContainer.Add(netCookie);
                    Console.WriteLine($"    • {cookie.Name}: {cookie.Value.Substring(0, Math.Min(20, cookie.Value.Length))}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Warning: Failed to add cookie {cookie.Name}: {ex.Message}");
                }
            }

            // Step 4: Get crumb token using JavaScript
            Console.WriteLine("  - Fetching crumb token...");
            _driver.Navigate().GoToUrl("https://query1.finance.yahoo.com/v1/test/getcrumb");
            await Task.Delay(1000);

            // Get the crumb from the page body
            var bodyElement = _driver.FindElement(By.TagName("body"));
            var crumb = bodyElement.Text.Trim();

            if (string.IsNullOrEmpty(crumb) || crumb.Contains("<html>") || crumb.Contains("<!DOCTYPE"))
            {
                throw new Exception("Failed to retrieve valid crumb token");
            }

            Console.WriteLine($"  ✓ Successfully authenticated! Crumb: {crumb}");

            return (cookieContainer, crumb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Selenium authentication failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Handle Yahoo's consent form if it appears
    /// </summary>
    private async Task HandleConsentFormIfPresent()
    {
        if (_driver == null) return;

        try
        {
            // Check if consent form is present
            var consentButtons = _driver.FindElements(By.CssSelector("button[name='agree'], button.accept-all, button[type='submit']"));

            if (consentButtons.Count > 0)
            {
                Console.WriteLine("  - Handling consent form...");

                // Try to find and click the accept button
                foreach (var button in consentButtons)
                {
                    try
                    {
                        if (button.Displayed && button.Enabled)
                        {
                            button.Click();
                            Console.WriteLine("  - Consent accepted");
                            await Task.Delay(2000);
                            break;
                        }
                    }
                    catch
                    {
                        // Continue to next button if this one fails
                        continue;
                    }
                }
            }
        }
        catch (NoSuchElementException)
        {
            // No consent form present, continue
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  - Warning: Could not handle consent form: {ex.Message}");
            // Continue anyway, might not be critical
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch
            {
                // Ignore errors during cleanup
            }

            _disposed = true;
        }
    }
}

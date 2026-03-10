using Microsoft.Playwright;

namespace ExpenseManagementPdfGenerator.Services
{
    /// <summary>
    /// Static browser instance shared across requests to avoid per-request Chromium spin-up
    /// and reduce memory/cold-start on constrained environments (e.g. Render 512MB).
    /// </summary>
    public static class SharedPlaywrightBrowser
    {
        private static IPlaywright? _playwright;
        private static IBrowser? _browser;
        private static readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Ensures the browser is initialized (lazy init on first request or after explicit InitializeAsync).
        /// </summary>
        public static async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser != null && _browser.IsConnected)
            {
                return _browser;
            }

            await _initLock.WaitAsync();
            try
            {
                if (_browser != null && _browser.IsConnected)
                {
                    return _browser;
                }

                await CloseAsync();
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage"
                    }
                });
                return _browser;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Call at startup to warm the browser (optional; otherwise first request will initialize).
        /// </summary>
        public static async Task InitializeAsync()
        {
            _ = await GetBrowserAsync();
        }

        /// <summary>
        /// Closes browser and Playwright. Call on application shutdown.
        /// </summary>
        public static async Task CloseAsync()
        {
            if (_browser != null)
            {
                await _browser.DisposeAsync();
                _browser = null;
            }

            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }
        }
    }
}

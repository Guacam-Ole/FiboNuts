using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Globalization;
using System.Reflection;
using BalatroPoker;
using BalatroPoker.Services;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure logging for Blazor WebAssembly
// Note: WebAssembly runs in browser and has limitations:
// - Cannot directly connect to external services like Loki
// - Console output is limited to browser developer tools
// - For production logging, consider sending logs via HTTP API to your backend
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

// Add localization services
builder.Services.AddLocalization();
builder.Services.AddSingleton<LocalizationService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register the HTTP-based game service instead of the localStorage one
builder.Services.AddScoped<HttpGameService>();

var app = builder.Build();

// Initialize culture from URL or localStorage before the app starts
await InitializeCultureAsync(app.Services);

await app.RunAsync();

static async Task InitializeCultureAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var jsRuntime = services.GetRequiredService<IJSRuntime>();
        
        // Get language from URL parameter or localStorage
        var urlLang = await jsRuntime.InvokeAsync<string>("eval", "new URLSearchParams(window.location.search).get('lang')");
        var storedLang = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "balatro-poker-language");
        
        var selectedLang = !string.IsNullOrEmpty(urlLang) ? urlLang : storedLang ?? "en";

        // Set culture before app initialization
        if (new[] { "en", "de", "pt", "fr", "it", "es" }.Contains(selectedLang))
        {
            var culture = new CultureInfo(selectedLang);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            logger.LogInformation("Culture initialized to: {SelectedLanguage}", selectedLang);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Culture initialization failed");
    }
}

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Globalization;
using BalatroPoker;
using BalatroPoker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add localization services
builder.Services.AddLocalization();
builder.Services.AddSingleton<LocalizationService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<MetricsService>();

var app = builder.Build();

// Initialize culture from URL or localStorage before the app starts
await InitializeCultureAsync(app.Services);

await app.RunAsync();

static async Task InitializeCultureAsync(IServiceProvider services)
{
    try
    {
        var jsRuntime = services.GetRequiredService<IJSRuntime>();
        
        // Get language from URL parameter or localStorage
        var urlLang = await jsRuntime.InvokeAsync<string>("eval", "new URLSearchParams(window.location.search).get('lang')");
        var storedLang = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "balatro-poker-language");
        
        var selectedLang = !string.IsNullOrEmpty(urlLang) ? urlLang : storedLang ?? "en";
        
        // Set culture before app initialization
        if (new[] { "en", "de", "fr", "it", "es" }.Contains(selectedLang))
        {
            var culture = new CultureInfo(selectedLang);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            Console.WriteLine($"Culture initialized to: {selectedLang}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Culture initialization failed: {ex.Message}");
    }
}

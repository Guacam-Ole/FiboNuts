using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
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

// Configure supported cultures for Blazor WebAssembly  
var supportedCultures = new[] { "en", "de", "fr", "it", "es" };
var defaultCulture = "en";

// Set default culture
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(defaultCulture);
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(defaultCulture);

var app = builder.Build();

// Initialize culture based on stored language preference after build
var localizationService = app.Services.GetService<LocalizationService>();
if (localizationService != null)
{
    try
    {
        var currentLanguage = await localizationService.GetCurrentLanguageAsync();
        await localizationService.SetLanguageWithoutReloadAsync(currentLanguage);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Culture initialization error: {ex.Message}");
    }
}

await app.RunAsync();

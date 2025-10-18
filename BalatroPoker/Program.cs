using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Globalization;
using System.Reflection;
using BalatroPoker;
using BalatroPoker.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure logging with Serilog and Loki
builder.Services.AddLogging(cfg => cfg.SetMinimumLevel(LogLevel.Debug));
builder.Services.AddSerilog(cfg =>
{
    cfg.MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("job", Assembly.GetEntryAssembly()?.GetName().Name)
        .Enrich.WithProperty("service", Assembly.GetEntryAssembly()?.GetName().Name)
        .Enrich.WithProperty("desktop", Environment.GetEnvironmentVariable("DESKTOP_SESSION"))
        .Enrich.WithProperty("language", Environment.GetEnvironmentVariable("LANGUAGE"))
        .Enrich.WithProperty("lc", Environment.GetEnvironmentVariable("LC_NAME"))
        .Enrich.WithProperty("timezone", Environment.GetEnvironmentVariable("TZ"))
        .Enrich.WithProperty("dotnetVersion", Environment.GetEnvironmentVariable("DOTNET_VERSION"))
        .Enrich.WithProperty("inContainer", Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"))
        .Enrich.WithProperty("environment", builder.HostEnvironment.Environment)
        .WriteTo.GrafanaLoki("http://thebeast:3100", propertiesAsLabels: ["job"]);
    
    if (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration == "Debug")
    {
        cfg.WriteTo.Console(new RenderedCompactJsonFormatter());
    }
    else
    {
        cfg.WriteTo.Console();
    }
});

// Add localization services
builder.Services.AddLocalization();
builder.Services.AddSingleton<LocalizationService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<GameService>();

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
        if (new[] { "en", "de", "fr", "it", "es" }.Contains(selectedLang))
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

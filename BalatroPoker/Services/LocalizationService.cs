using Microsoft.JSInterop;
using System.Globalization;
using System.Reflection;

namespace BalatroPoker.Services;

public class LocalizationService
{
    private readonly IJSRuntime _jsRuntime;
    
    public event Action? LanguageChanged;
    
    private readonly Dictionary<string, (string Name, string Flag)> _supportedLanguages = new()
    {
        { "en", ("English", "🇬🇧") },
        { "de", ("Deutsch", "🇩🇪") },
        { "fr", ("Français", "🇫🇷") },
        { "it", ("Italiano", "🇮🇹") },
        { "es", ("Español", "🇪🇸") }
    };
    
    public LocalizationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public Dictionary<string, (string Name, string Flag)> SupportedLanguages => _supportedLanguages;
    
    public async Task<string> GetCurrentLanguageAsync()
    {
        try
        {
            // Add a small delay to ensure DOM is ready
            await Task.Delay(100);
            
            // First check URL parameter
            var urlParams = await _jsRuntime.InvokeAsync<string>("eval", "new URLSearchParams(window.location.search).get('lang')");
            if (!string.IsNullOrEmpty(urlParams) && _supportedLanguages.ContainsKey(urlParams))
            {
                // If URL has lang parameter, store it and use it
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "balatro-poker-language", urlParams);
                return urlParams;
            }
            
            var storedLanguage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "balatro-poker-language");
            if (!string.IsNullOrEmpty(storedLanguage) && _supportedLanguages.ContainsKey(storedLanguage))
            {
                return storedLanguage;
            }
            
            // Fallback to browser language
            var browserLanguage = await _jsRuntime.InvokeAsync<string>("navigator.language");
            var languageCode = browserLanguage?.Split('-')[0].ToLower() ?? "en";
            
            return _supportedLanguages.ContainsKey(languageCode) ? languageCode : "en";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCurrentLanguageAsync error: {ex.Message}");
            return "en"; // Default fallback
        }
    }
    
    public async Task SetLanguageAsync(string languageCode)
    {
        if (_supportedLanguages.ContainsKey(languageCode))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "balatro-poker-language", languageCode);
            
            // Update URL with language parameter and reload
            var jsCode = $@"
                const url = new URL(window.location);
                url.searchParams.set('lang', '{languageCode}');
                window.location.href = url.toString();
            ";
            await _jsRuntime.InvokeVoidAsync("eval", jsCode);
        }
    }
    
    public async Task SetLanguageWithoutReloadAsync(string languageCode)
    {
        if (_supportedLanguages.ContainsKey(languageCode))
        {
            try
            {
                // Set the culture for the current session without reloading
                var culture = new CultureInfo(languageCode);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                
                // For Blazor WebAssembly, these are crucial
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                
                // Try to load satellite assembly for this culture
                await TryLoadSatelliteAssembly(languageCode);
                
                // Store the language choice
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "balatro-poker-language", languageCode);
                
                // Update URL with language parameter
                await UpdateUrlWithLanguageAsync(languageCode);
                
                // Notify components that language changed
                LanguageChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting language: {ex.Message}");
            }
        }
    }
    
    public async Task<string> GetGameLanguageAsync(string gameId)
    {
        try
        {
            var gameLanguage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"balatro-poker-game-language-{gameId}");
            return !string.IsNullOrEmpty(gameLanguage) && _supportedLanguages.ContainsKey(gameLanguage) ? gameLanguage : await GetCurrentLanguageAsync();
        }
        catch
        {
            return await GetCurrentLanguageAsync();
        }
    }
    
    public async Task SetGameLanguageAsync(string gameId, string languageCode)
    {
        if (_supportedLanguages.ContainsKey(languageCode))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"balatro-poker-game-language-{gameId}", languageCode);
        }
    }
    
    public async Task UpdateUrlWithLanguageAsync(string languageCode)
    {
        try
        {
            // JavaScript to update URL with language parameter
            var jsCode = $@"
                const url = new URL(window.location);
                url.searchParams.set('lang', '{languageCode}');
                window.history.replaceState(null, '', url);
            ";
            await _jsRuntime.InvokeVoidAsync("eval", jsCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateUrlWithLanguageAsync error: {ex.Message}");
        }
    }
    
    public string AddLanguageToUrl(string baseUrl, string languageCode)
    {
        if (string.IsNullOrEmpty(baseUrl) || !_supportedLanguages.ContainsKey(languageCode))
            return baseUrl;
            
        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}lang={languageCode}";
    }
    
    private async Task TryLoadSatelliteAssembly(string languageCode)
    {
        try
        {
            if (languageCode == "en") return; // Default culture, no satellite assembly needed
            
            // Try to load satellite assembly using .NET Assembly loading
            var assemblyName = $"BalatroPoker.resources";
            var culture = new CultureInfo(languageCode);
            
            // Force assembly loading by trying to access it
            var mainAssembly = Assembly.GetExecutingAssembly();
            var satelliteAssembly = mainAssembly.GetSatelliteAssembly(culture);
            
            Console.WriteLine($"Loaded satellite assembly for {languageCode}: {satelliteAssembly?.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load satellite assembly for {languageCode}: {ex.Message}");
            
            // Fallback: Try to use reflection to find and load the assembly
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var satelliteAssembly = assemblies.FirstOrDefault(a => 
                    a.FullName?.Contains($"BalatroPoker.resources") == true && 
                    a.FullName?.Contains(languageCode) == true);
                    
                if (satelliteAssembly != null)
                {
                    Console.WriteLine($"Found existing satellite assembly: {satelliteAssembly.FullName}");
                }
                else
                {
                    Console.WriteLine($"No satellite assembly found for {languageCode}");
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Reflection fallback failed: {ex2.Message}");
            }
        }
    }
}
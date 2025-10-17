using Microsoft.JSInterop;
using System.Globalization;

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
            
            // For user-initiated language changes, reload the page to apply new culture
            await _jsRuntime.InvokeVoidAsync("location.reload");
        }
    }
    
    public async Task SetLanguageWithoutReloadAsync(string languageCode)
    {
        if (_supportedLanguages.ContainsKey(languageCode))
        {
            // Set the culture for the current session without reloading
            var culture = new CultureInfo(languageCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            // Notify components that language changed
            LanguageChanged?.Invoke();
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
}
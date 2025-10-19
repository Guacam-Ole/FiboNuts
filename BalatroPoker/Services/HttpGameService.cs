using BalatroPoker.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BalatroPoker.Services;

public class HttpGameService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpGameService> _logger;
    private const string ApiBaseUrl = "/api/game";

    public HttpGameService(HttpClient httpClient, ILogger<HttpGameService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GameState?> CreateGameAsync(List<int> allowedValues, int jokerCount)
    {
        try
        {
            var request = new CreateGameRequest 
            { 
                AllowedValues = allowedValues, 
                JokerCount = jokerCount 
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/create", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GameState>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            _logger.LogError("Failed to create game: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return null;
        }
    }

    public async Task<GameState?> GetGameByAdminCodeAsync(string adminCode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/admin/{adminCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by admin code");
            return null;
        }
    }

    public async Task<GameState?> GetGameByPlayerCodeAsync(string playerCode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/player/{playerCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by player code");
            return null;
        }
    }

    public async Task<Player?> JoinGameAsync(string playerCode, string name)
    {
        try
        {
            var request = new JoinGameRequest { Name = name };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/player/{playerCode}/join", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Join game API response: {ResponseJson}", responseJson);
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                
                var player = JsonSerializer.Deserialize<Player>(responseJson, options);
                _logger.LogInformation("Deserialized player: ID={PlayerId}, Name={PlayerName}, Cards={CardCount}", 
                    player?.Id, player?.Name, player?.Cards?.Count);
                
                return player;
            }
            else
            {
                _logger.LogError("Join game API call failed: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            return null;
        }
    }

    public async Task<bool> SubmitVoteAsync(string playerCode, string playerId, List<Card> selectedCards)
    {
        try
        {
            var request = new SubmitVoteRequest 
            { 
                PlayerId = playerId, 
                SelectedCards = selectedCards 
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/player/{playerCode}/vote", content);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting vote");
            return false;
        }
    }

    public async Task<bool> RevealCardsAsync(string adminCode)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/{adminCode}/reveal", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing cards");
            return false;
        }
    }

    public async Task<bool> StartVotingAsync(string adminCode)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/{adminCode}/start-voting", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting voting");
            return false;
        }
    }

    public async Task<bool> StartNewRoundAsync(string adminCode)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/admin/{adminCode}/new-round", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new round");
            return false;
        }
    }
}

// Request DTOs - these should match the API
public class CreateGameRequest
{
    public List<int> AllowedValues { get; set; } = new();
    public int JokerCount { get; set; }
}

public class JoinGameRequest
{
    public string Name { get; set; } = "";
}

public class SubmitVoteRequest
{
    public string PlayerId { get; set; } = "";
    public List<Card> SelectedCards { get; set; } = new();
}
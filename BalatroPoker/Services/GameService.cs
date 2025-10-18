using BalatroPoker.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace BalatroPoker.Services;

public class GameService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly MetricsService _metricsService;
    private static readonly Random _random = new();
    private readonly JokerProcessor _jokerProcessor = new();
    
    public GameService(IJSRuntime jsRuntime, MetricsService metricsService)
    {
        _jsRuntime = jsRuntime;
        _metricsService = metricsService;
    }
    
    private async Task<Dictionary<string, GameState>> GetAllGamesAsync()
    {
        try
        {
            var gamesJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "balatro-poker-games");
            if (string.IsNullOrEmpty(gamesJson))
            {
                return new Dictionary<string, GameState>();
            }
                
            var games = JsonSerializer.Deserialize<Dictionary<string, GameState>>(gamesJson) ?? new Dictionary<string, GameState>();
            
            // Restore joker effects after deserialization
            foreach (var game in games.Values)
            {
                if (game.ActiveJokers.Any())
                {
                    JokerProcessor.RestoreJokerEffects(game.ActiveJokers);
                }
            }
            
            return games;
        }
        catch (Exception ex)
        {
            // Silent failure - return empty dictionary
            return new Dictionary<string, GameState>();
        }
    }
    
    private async Task SaveAllGamesAsync(Dictionary<string, GameState> games)
    {
        try
        {
            var gamesJson = JsonSerializer.Serialize(games);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "balatro-poker-games", gamesJson);
        }
        catch (Exception ex)
        {
            // Silent failure for localStorage save
        }
    }
    
    public async Task<GameState> CreateGameAsync()
    {
        var games = await GetAllGamesAsync();
        
        var game = new GameState
        {
            GameId = Guid.NewGuid().ToString(),
            AdminCode = GenerateCode(),
            PlayerCode = GenerateCode()
        };
        
        games[game.GameId] = game;
        await SaveAllGamesAsync(games);
        
        // Record metrics
        _metricsService.RecordGameCreated();
        
        return game;
    }
    
    public async Task<GameState?> GetGameByAdminCodeAsync(string adminCode)
    {
        var games = await GetAllGamesAsync();
        return games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
    }
    
    public async Task<GameState?> GetGameByPlayerCodeAsync(string playerCode)
    {
        var games = await GetAllGamesAsync();
        return games.Values.FirstOrDefault(g => g.PlayerCode == playerCode);
    }
    
    public async Task<Player> AddPlayerAsync(string playerCode, string name)
    {
        var games = await GetAllGamesAsync();
        var game = games.Values.FirstOrDefault(g => g.PlayerCode == playerCode);
        if (game == null) throw new ArgumentException("Invalid player code");
        
        var existingPlayer = game.Players.FirstOrDefault(p => p.Name == name);
        if (existingPlayer != null) return existingPlayer;
        
        var player = new Player
        {
            Name = name,
            Cards = GeneratePlayerDeck()
        };
        
        game.Players.Add(player);
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        await SaveAllGamesAsync(games);
        
        // Record metrics
        _metricsService.RecordPlayerJoined();
        
        return player;
    }
    
    public async Task SubmitVoteAsync(string playerCode, string playerId, List<Card> selectedCards)
    {
        var games = await GetAllGamesAsync();
        var game = games.Values.FirstOrDefault(g => g.PlayerCode == playerCode);
        if (game == null) throw new ArgumentException("Invalid player code");
        
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) throw new ArgumentException("Player not found");
        
        var sum = selectedCards.Sum(c => c.Value);
        if (!game.AllowedValues.Contains(sum))
            throw new ArgumentException("Sum must match an allowed Fibonacci value");
            
        player.SelectedCards = selectedCards;
        player.OriginalVote = sum;
        player.FinalVote = sum;
        player.HasVoted = true;
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        await SaveAllGamesAsync(games);
        
        // Record metrics
        _metricsService.RecordVoteSubmitted(sum, selectedCards);
    }
    
    public async Task UpdateGameSettingsAsync(string adminCode, List<int> allowedValues, int jokerCount)
    {
        var games = await GetAllGamesAsync();
        var game = games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
        if (game == null) throw new ArgumentException("Invalid admin code");
        
        game.AllowedValues = allowedValues;
        game.JokerCount = jokerCount;
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        await SaveAllGamesAsync(games);
    }
    
    public async Task StartGameAsync(string adminCode)
    {
        var games = await GetAllGamesAsync();
        var game = games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
        if (game == null) throw new ArgumentException("Invalid admin code");
        
        game.Phase = GamePhase.Voting;
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        await SaveAllGamesAsync(games);
    }
    
    public async Task RevealCardsAsync(string adminCode)
    {
        Console.WriteLine($"[DEBUG] RevealCardsAsync called with adminCode: {adminCode}");
        
        var games = await GetAllGamesAsync();
        Console.WriteLine($"[DEBUG] Found {games.Count} games in storage");
        
        var game = games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
        if (game == null) 
        {
            Console.WriteLine($"[ERROR] Game not found for adminCode: {adminCode}");
            throw new ArgumentException("Invalid admin code");
        }
        
        Console.WriteLine($"[DEBUG] Game found - Phase: {game.Phase}, Players: {game.Players.Count}");
        
        if (game.Phase != GamePhase.Voting) 
        {
            Console.WriteLine($"[WARNING] Game phase is {game.Phase}, not Voting. Returning.");
            return;
        }
        
        game.ActiveJokers = _jokerProcessor.SelectRandomJokers(game.JokerCount, game.JokerCount);
        Console.WriteLine($"[DEBUG] Selected {game.ActiveJokers.Count} jokers: {string.Join(", ", game.ActiveJokers.Select(j => j.Name))}");
        
        // Record joker usage metrics
        foreach (var joker in game.ActiveJokers)
        {
            _metricsService.RecordJokerUsage(joker.Name);
        }
        
        // Apply jokers even if some players haven't voted (use 0 for unvoted players)
        var votes = game.Players.Select(p => p.OriginalVote).ToList();
        Console.WriteLine($"[DEBUG] Original votes: [{string.Join(", ", votes)}]");
        
        if (game.ActiveJokers.Any() && votes.Any())
        {
            var context = new JokerContext
            {
                Votes = votes,
                Players = game.Players,
                ActiveJokers = game.ActiveJokers,
                Random = _random
            };
            
            var finalVotes = _jokerProcessor.ApplyJokers(context);
            Console.WriteLine($"[DEBUG] Final votes after jokers: [{string.Join(", ", finalVotes)}]");
            
            for (int i = 0; i < game.Players.Count; i++)
            {
                game.Players[i].FinalVote = finalVotes[i];
            }
        }
        else
        {
            // No jokers or no votes - just copy original votes to final votes
            Console.WriteLine($"[DEBUG] No jokers or no votes - copying original to final");
            for (int i = 0; i < game.Players.Count; i++)
            {
                game.Players[i].FinalVote = game.Players[i].OriginalVote;
            }
        }
        
        game.Phase = GamePhase.Revealed;
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        
        // Record round completion
        _metricsService.RecordRoundPlayed();
        
        Console.WriteLine($"[DEBUG] Saving game state - Phase: {game.Phase}");
        await SaveAllGamesAsync(games);
        Console.WriteLine($"[DEBUG] Game state saved successfully");
    }
    
    public async Task StartNewRoundAsync(string adminCode)
    {
        var games = await GetAllGamesAsync();
        var game = games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
        if (game == null) throw new ArgumentException("Invalid admin code");
        
        foreach (var player in game.Players)
        {
            player.HasVoted = false;
            player.OriginalVote = 0;
            player.FinalVote = 0;
            player.SelectedCards.Clear();
            player.Cards = GeneratePlayerDeck();
        }
        
        game.ActiveJokers.Clear();
        game.Phase = GamePhase.Voting;
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        await SaveAllGamesAsync(games);
    }
    
    private List<Card> GeneratePlayerDeck()
    {
        var suits = Enum.GetValues<Suit>();
        var cards = new List<Card>();
        
        // Regular number cards
        foreach (var value in new[] { 2, 3, 5, 8, 1 }) // A=1
        {
            cards.Add(new Card { Value = value, Suit = suits[_random.Next(suits.Length)] });
        }
        
        // Face cards (all worth 10 but display differently)
        cards.Add(new Card { Value = 10, Suit = suits[_random.Next(suits.Length)], FaceType = "J" });
        cards.Add(new Card { Value = 10, Suit = suits[_random.Next(suits.Length)], FaceType = "Q" });
        cards.Add(new Card { Value = 10, Suit = suits[_random.Next(suits.Length)], FaceType = "K" });
        
        return cards;
    }
    
    private string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 10)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }
}
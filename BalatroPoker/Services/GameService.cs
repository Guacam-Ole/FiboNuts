using BalatroPoker.Models;
using Microsoft.JSInterop;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BalatroPoker.Services;

public class GameService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<GameService> _logger;
    private static readonly Random _random = new();
    private readonly JokerProcessor _jokerProcessor = new();
    
    public GameService(IJSRuntime jsRuntime, ILogger<GameService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
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
        
        _logger.LogInformation("Game created with ID {GameId}", game.GameId);
        _logger.LogInformation("METRIC: game_created, game_id={GameId}, allowed_values={AllowedValues}, joker_count={JokerCount}", 
            game.GameId, string.Join(",", game.AllowedValues), game.JokerCount);
        
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
        
        _logger.LogInformation("Player {PlayerName} joined game {GameId}", name, game.GameId);
        _logger.LogInformation("METRIC: player_joined, game_id={GameId}, player_name={PlayerName}, player_count={PlayerCount}", 
            game.GameId, name, game.Players.Count);
        
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
        
        var cardsList = string.Join(",", selectedCards.Select(c => $"{c.Suit}_{c.Value}"));
        _logger.LogInformation("Vote submitted by {PlayerName} in game {GameId}: {VoteValue}", player.Name, game.GameId, sum);
        _logger.LogInformation("METRIC: vote_submitted, game_id={GameId}, player_name={PlayerName}, vote_value={VoteValue}, cards={Cards}", 
            game.GameId, player.Name, sum, cardsList);
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
        _logger.LogDebug("RevealCardsAsync called with adminCode: {AdminCode}", adminCode);
        
        var games = await GetAllGamesAsync();
        _logger.LogDebug("Found {GameCount} games in storage", games.Count);
        
        var game = games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
        if (game == null) 
        {
            _logger.LogError("Game not found for adminCode: {AdminCode}", adminCode);
            throw new ArgumentException("Invalid admin code");
        }
        
        _logger.LogDebug("Game found - Phase: {Phase}, Players: {PlayerCount}", game.Phase, game.Players.Count);
        
        if (game.Phase != GamePhase.Voting) 
        {
            _logger.LogWarning("Game phase is {Phase}, not Voting. Returning", game.Phase);
            return;
        }
        
        game.ActiveJokers = _jokerProcessor.SelectRandomJokers(game.JokerCount, game.JokerCount);
        _logger.LogDebug("Selected {JokerCount} jokers: {JokerNames}", game.ActiveJokers.Count, string.Join(", ", game.ActiveJokers.Select(j => j.Name)));
        
        // Log joker usage metrics
        foreach (var joker in game.ActiveJokers)
        {
            _logger.LogInformation("METRIC: joker_used, game_id={GameId}, joker_name={JokerName}, joker_description={JokerDescription}", 
                game.GameId, joker.Name, joker.Description);
        }
        
        // Apply jokers even if some players haven't voted (use 0 for unvoted players)
        var votes = game.Players.Select(p => p.OriginalVote).ToList();
        _logger.LogDebug("Original votes: [{OriginalVotes}]", string.Join(", ", votes));
        
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
            _logger.LogDebug("Final votes after jokers: [{FinalVotes}]", string.Join(", ", finalVotes));
            
            for (int i = 0; i < game.Players.Count; i++)
            {
                game.Players[i].FinalVote = finalVotes[i];
            }
        }
        else
        {
            // No jokers or no votes - just copy original votes to final votes
            _logger.LogDebug("No jokers or no votes - copying original to final");
            for (int i = 0; i < game.Players.Count; i++)
            {
                game.Players[i].FinalVote = game.Players[i].OriginalVote;
            }
        }
        
        game.Phase = GamePhase.Revealed;
        game.LastUpdate = DateTime.UtcNow;
        games[game.GameId] = game;
        
        // Log round completion metrics
        var finalVotesList = string.Join(",", game.Players.Select(p => p.FinalVote));
        var originalVotesList = string.Join(",", game.Players.Select(p => p.OriginalVote));
        var jokersList = string.Join(",", game.ActiveJokers.Select(j => j.Name));
        
        _logger.LogInformation("Round completed for game {GameId}", game.GameId);
        _logger.LogInformation("METRIC: round_completed, game_id={GameId}, player_count={PlayerCount}, original_votes={OriginalVotes}, final_votes={FinalVotes}, jokers_used={JokersUsed}", 
            game.GameId, game.Players.Count, originalVotesList, finalVotesList, jokersList);
        
        _logger.LogDebug("Saving game state - Phase: {Phase}", game.Phase);
        await SaveAllGamesAsync(games);
        _logger.LogDebug("Game state saved successfully");
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
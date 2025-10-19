using BalatroPoker.Api.Models;
using System.Collections.Concurrent;
using static BalatroPoker.Api.Models.Suit;

namespace BalatroPoker.Api.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, GameState> _games = new();
    private readonly ILogger<GameService> _logger;
    private static readonly Random _random = new();
    private readonly JokerProcessor _jokerProcessor = new();
    
    public GameService(ILogger<GameService> logger)
    {
        _logger = logger;
    }
    
    public GameState? GetGame(string gameId)
    {
        _games.TryGetValue(gameId, out var game);
        return game;
    }
    
    public GameState? GetGameByAdminCode(string adminCode)
    {
        return _games.Values.FirstOrDefault(g => g.AdminCode == adminCode);
    }
    
    public GameState? GetGameByPlayerCode(string playerCode)
    {
        return _games.Values.FirstOrDefault(g => g.PlayerCode == playerCode);
    }
    
    public GameState CreateGame(List<int> allowedValues, int jokerCount)
    {
        var gameId = Guid.NewGuid().ToString("N")[..8];
        var adminCode = Guid.NewGuid().ToString("N")[..12];
        var playerCode = Guid.NewGuid().ToString("N")[..8];
        
        var game = new GameState
        {
            GameId = gameId,
            AdminCode = adminCode,
            PlayerCode = playerCode,
            AllowedValues = allowedValues,
            JokerCount = jokerCount,
            Phase = GamePhase.Voting,
            Players = new List<Player>(),
            ActiveJokers = new List<Joker>(),
            CreatedAt = DateTime.UtcNow
        };
        
        _games.TryAdd(gameId, game);
        
        _logger.LogInformation("Game created with ID {GameId}", game.GameId);
        _logger.LogInformation("METRIC: game_created, game_id={GameId}, allowed_values={AllowedValues}, joker_count={JokerCount}", 
            game.GameId, string.Join(",", allowedValues), jokerCount);
        
        return game;
    }
    
    public Player? AddPlayer(string playerCode, string name)
    {
        _logger.LogDebug("Attempting to add player {PlayerName} to game with playerCode {PlayerCode}", name, playerCode);
        
        var game = GetGameByPlayerCode(playerCode);
        if (game == null) 
        {
            _logger.LogWarning("Game not found for playerCode {PlayerCode} when adding player {PlayerName}", playerCode, name);
            return null;
        }
        
        // Check if player already exists
        var existingPlayer = game.Players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existingPlayer != null)
        {
            _logger.LogInformation("Player {PlayerName} already exists in game {GameId}, returning existing player", name, game.GameId);
            return existingPlayer;
        }
        
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Cards = GeneratePlayerCards(),
            HasVoted = false,
            OriginalVote = 0,
            FinalVote = 0,
            SelectedCards = new List<Card>()
        };
        
        game.Players.Add(player);
        
        _logger.LogInformation("Player {PlayerName} joined game {GameId}", name, game.GameId);
        _logger.LogInformation("METRIC: player_joined, game_id={GameId}, player_name={PlayerName}, player_count={PlayerCount}", 
            game.GameId, name, game.Players.Count);
        
        return player;
    }
    
    public bool SubmitVote(string playerCode, string playerId, List<Card> selectedCards)
    {
        _logger.LogDebug("Attempting vote submission for player {PlayerId} in game with playerCode {PlayerCode}", playerId, playerCode);
        
        var game = GetGameByPlayerCode(playerCode);
        if (game == null) 
        {
            _logger.LogWarning("Game not found for playerCode {PlayerCode} during vote submission", playerCode);
            return false;
        }
        
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) 
        {
            _logger.LogWarning("Player {PlayerId} not found in game {GameId} during vote submission", playerId, game.GameId);
            return false;
        }
        
        var sum = selectedCards.Sum(c => c.Value);
        var cardDisplay = string.Join("+", selectedCards.Select(c => c.DisplayValue));
        
        if (!game.AllowedValues.Contains(sum))
        {
            _logger.LogWarning("Invalid vote sum {Sum} (cards: {Cards}) by player {PlayerName} in game {GameId}. Allowed values: {AllowedValues}", 
                sum, cardDisplay, player.Name, game.GameId, string.Join(",", game.AllowedValues));
            return false;
        }
        
        player.OriginalVote = sum;
        player.SelectedCards = selectedCards;
        player.HasVoted = true;
        
        _logger.LogInformation("Vote submitted by {PlayerName} in game {GameId}: {VoteValue} (cards: {Cards})", 
            player.Name, game.GameId, sum, cardDisplay);
        _logger.LogInformation("METRIC: vote_submitted, game_id={GameId}, player_name={PlayerName}, vote_value={VoteValue}, cards_display={CardsDisplay}, cards_detail={Cards}", 
            game.GameId, player.Name, sum, cardDisplay, string.Join(",", selectedCards.Select(c => $"{c.DisplayValue}({c.Value}){c.Suit}")));
        
        return true;
    }
    
    public bool RevealCards(string adminCode)
    {
        _logger.LogDebug("RevealCards called with adminCode: {AdminCode}", adminCode);
        
        var game = GetGameByAdminCode(adminCode);
        if (game == null)
        {
            _logger.LogError("Game not found for adminCode: {AdminCode}", adminCode);
            return false;
        }
        
        _logger.LogDebug("Game found - Phase: {Phase}, Players: {PlayerCount}", game.Phase, game.Players.Count);
        
        if (game.Phase != GamePhase.Voting)
        {
            _logger.LogWarning("Game phase is {Phase}, not Voting. Returning", game.Phase);
            return false;
        }
        
        // Select random jokers
        game.ActiveJokers = JokerProcessor.GetRandomJokers(game.JokerCount);
        _logger.LogDebug("Selected {JokerCount} jokers: {JokerNames}", game.ActiveJokers.Count, string.Join(", ", game.ActiveJokers.Select(j => j.Name)));
        
        // Log joker usage for metrics
        foreach (var joker in game.ActiveJokers)
        {
            _logger.LogInformation("METRIC: joker_used, game_id={GameId}, joker_name={JokerName}, joker_description={JokerDescription}", 
                game.GameId, joker.Name, joker.Description);
        }
        
        // Get all votes
        var votes = game.Players.Where(p => p.HasVoted).Select(p => p.OriginalVote).ToList();
        _logger.LogDebug("Original votes: [{OriginalVotes}]", string.Join(", ", votes));
        
        if (game.ActiveJokers.Any() && votes.Any())
        {
            // Apply jokers to get final votes
            var finalVotes = new List<int>(votes);
            
            foreach (var joker in game.ActiveJokers)
            {
                if (joker.SimpleEffect != null)
                {
                    var context = new JokerContext { Votes = finalVotes };
                    finalVotes = joker.SimpleEffect(context);
                }
            }
            
            _logger.LogDebug("Final votes after jokers: [{FinalVotes}]", string.Join(", ", finalVotes));
            
            // Update players with final votes
            var votedPlayers = game.Players.Where(p => p.HasVoted).ToList();
            for (int i = 0; i < votedPlayers.Count && i < finalVotes.Count; i++)
            {
                votedPlayers[i].FinalVote = finalVotes[i];
            }
        }
        else
        {
            _logger.LogDebug("No jokers or no votes - copying original to final");
            // No jokers or no votes - copy original votes to final votes
            foreach (var player in game.Players.Where(p => p.HasVoted))
            {
                player.FinalVote = player.OriginalVote;
            }
        }
        
        game.Phase = GamePhase.Revealed;
        
        _logger.LogInformation("Round completed for game {GameId}", game.GameId);
        _logger.LogInformation("METRIC: round_completed, game_id={GameId}, player_count={PlayerCount}, original_votes={OriginalVotes}, final_votes={FinalVotes}, jokers_used={JokersUsed}", 
            game.GameId, game.Players.Count, string.Join(",", votes), 
            string.Join(",", game.Players.Where(p => p.HasVoted).Select(p => p.FinalVote)), 
            string.Join(",", game.ActiveJokers.Select(j => j.Name)));
        
        return true;
    }
    
    public bool StartNewRound(string adminCode)
    {
        var game = GetGameByAdminCode(adminCode);
        if (game == null) return false;
        
        // Reset voting state
        foreach (var player in game.Players)
        {
            player.HasVoted = false;
            player.OriginalVote = 0;
            player.FinalVote = 0;
            player.SelectedCards = new List<Card>();
            player.Cards = GeneratePlayerCards();
        }
        
        game.ActiveJokers = new List<Joker>();
        game.Phase = GamePhase.Voting;
        
        return true;
    }
    
    public bool StartVoting(string adminCode)
    {
        _logger.LogDebug("Attempting to start voting for game with adminCode {AdminCode}", adminCode);
        
        var game = GetGameByAdminCode(adminCode);
        if (game == null) 
        {
            _logger.LogWarning("Game not found for adminCode {AdminCode} when starting voting", adminCode);
            return false;
        }
        
        var previousPhase = game.Phase;
        game.Phase = GamePhase.Voting;
        
        _logger.LogInformation("Voting started for game {GameId} (changed from {PreviousPhase} to {NewPhase}). Players: {PlayerCount}", 
            game.GameId, previousPhase, game.Phase, game.Players.Count);
        _logger.LogInformation("METRIC: voting_started, game_id={GameId}, player_count={PlayerCount}", 
            game.GameId, game.Players.Count);
        
        return true;
    }
    
    private List<Card> GeneratePlayerCards()
    {
        var suits = new[] { Spades, Hearts, Diamonds, Clubs };
        
        var cards = new List<Card>
        {
            // Ace (value 1)
            new Card { Value = 1, Suit = suits[0], FaceType = "A" },
            // Number cards
            new Card { Value = 2, Suit = suits[1] },
            new Card { Value = 3, Suit = suits[2] },
            new Card { Value = 5, Suit = suits[3] },
            new Card { Value = 8, Suit = suits[0] },
            // Face cards (all value 10)
            new Card { Value = 10, Suit = suits[1], FaceType = "J" },
            new Card { Value = 10, Suit = suits[2], FaceType = "Q" },
            new Card { Value = 10, Suit = suits[3], FaceType = "K" }
        };
        
        return cards;
    }
}
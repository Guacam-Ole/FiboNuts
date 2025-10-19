using Prometheus;
using BalatroPoker.Api.Models;

namespace BalatroPoker.Api.Services;

public class MetricsService
{
    // Game metrics
    private static readonly Counter GamesCreated = Metrics
        .CreateCounter("balatro_games_created_total", "Total number of games created");
    
    private static readonly Counter PlayersJoined = Metrics
        .CreateCounter("balatro_players_joined_total", "Total number of players who joined games");
    
    private static readonly Counter VotesSubmitted = Metrics
        .CreateCounter("balatro_votes_submitted_total", "Total number of votes submitted");
    
    private static readonly Counter RoundsCompleted = Metrics
        .CreateCounter("balatro_rounds_completed_total", "Total number of completed rounds");
    
    private static readonly Histogram PlayersPerGame = Metrics
        .CreateHistogram("balatro_players_per_game", "Number of players per game", 
            new HistogramConfiguration { Buckets = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 15, 20 } });
    
    private static readonly Histogram GameDuration = Metrics
        .CreateHistogram("balatro_game_duration_seconds", "Game duration in seconds",
            new HistogramConfiguration { Buckets = new double[] { 30, 60, 120, 300, 600, 1200, 1800, 3600 } });
    
    // Vote value metrics
    private static readonly Counter VotesByValue = Metrics
        .CreateCounter("balatro_votes_by_value_total", "Votes submitted by value", "value");
    
    private static readonly Counter CardCombinationsUsed = Metrics
        .CreateCounter("balatro_card_combinations_total", "Card combinations used", "cards");
    
    // Joker metrics
    private static readonly Counter JokersUsed = Metrics
        .CreateCounter("balatro_jokers_used_total", "Jokers used in games", "joker_name");
    
    // Current state metrics
    private static readonly Gauge ActiveGames = Metrics
        .CreateGauge("balatro_active_games", "Number of currently active games");
    
    private static readonly Gauge TotalActivePlayers = Metrics
        .CreateGauge("balatro_active_players_total", "Total number of active players across all games");

    public void RecordGameCreated()
    {
        GamesCreated.Inc();
    }

    public void RecordPlayerJoined()
    {
        PlayersJoined.Inc();
    }

    public void RecordVoteSubmitted(int value, List<Card> selectedCards)
    {
        VotesSubmitted.Inc();
        VotesByValue.WithLabels(value.ToString()).Inc();
        
        // Record card combination
        var cardCombination = string.Join("+", selectedCards.Select(c => c.DisplayValue));
        CardCombinationsUsed.WithLabels(cardCombination).Inc();
    }

    public void RecordRoundCompleted(GameState game)
    {
        RoundsCompleted.Inc();
        PlayersPerGame.Observe(game.Players.Count);
        
        // Calculate game duration
        var duration = DateTime.UtcNow - game.CreatedAt;
        GameDuration.Observe(duration.TotalSeconds);
        
        // Record jokers used
        foreach (var joker in game.ActiveJokers)
        {
            JokersUsed.WithLabels(joker.Name).Inc();
        }
    }

    public void UpdateActiveGameStats(int activeGamesCount, int totalActivePlayers)
    {
        ActiveGames.Set(activeGamesCount);
        TotalActivePlayers.Set(totalActivePlayers);
    }

    public void RecordVotingStarted()
    {
        // Could add specific voting started metrics if needed
    }

    public void RecordNewRound()
    {
        // Could add new round metrics if needed
    }
}
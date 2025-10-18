using BalatroPoker.Models;
using System.Collections.Concurrent;
using System.Text;

namespace BalatroPoker.Services;

public class MetricsService
{
    private long _gamesCreated = 0;
    private long _gamesCompleted = 0;
    private long _playersJoined = 0;
    private long _votesSubmitted = 0;
    private long _roundsPlayed = 0;
    
    private readonly ConcurrentDictionary<string, long> _languageUsage = new();
    private readonly ConcurrentDictionary<string, long> _cardUsage = new();
    private readonly ConcurrentDictionary<string, long> _jokerUsage = new();
    private readonly ConcurrentDictionary<int, long> _voteDistribution = new();
    
    private int _lowestVote = int.MaxValue;
    private int _highestVote = int.MinValue;
    
    private readonly object _lockObject = new();

    public MetricsService()
    {
    }

    public void RecordGameCreated()
    {
        Interlocked.Increment(ref _gamesCreated);
    }

    public void RecordGameCompleted()
    {
        Interlocked.Increment(ref _gamesCompleted);
    }

    public void RecordPlayerJoined()
    {
        Interlocked.Increment(ref _playersJoined);
    }

    public void RecordVoteSubmitted(int vote, List<Card> cards)
    {
        Interlocked.Increment(ref _votesSubmitted);
        
        // Track vote distribution
        _voteDistribution.AddOrUpdate(vote, 1, (key, value) => value + 1);
        
        // Track card usage
        foreach (var card in cards)
        {
            var cardKey = $"{card.Suit}_{card.Value}";
            _cardUsage.AddOrUpdate(cardKey, 1, (key, value) => value + 1);
        }
        
        // Update vote range
        lock (_lockObject)
        {
            if (vote < _lowestVote)
                _lowestVote = vote;
            if (vote > _highestVote)
                _highestVote = vote;
        }
        
    }

    public void RecordRoundPlayed()
    {
        Interlocked.Increment(ref _roundsPlayed);
    }

    public void RecordLanguageUsage(string language)
    {
        _languageUsage.AddOrUpdate(language, 1, (key, value) => value + 1);
    }

    public void RecordJokerUsage(string jokerName)
    {
        _jokerUsage.AddOrUpdate(jokerName, 1, (key, value) => value + 1);
    }

    public string GetPrometheusMetrics()
    {
        var sb = new StringBuilder();
        
        // Basic game metrics
        sb.AppendLine("# HELP balatro_games_created_total Total number of games created");
        sb.AppendLine("# TYPE balatro_games_created_total counter");
        sb.AppendLine($"balatro_games_created_total {_gamesCreated}");
        sb.AppendLine();
        
        sb.AppendLine("# HELP balatro_games_completed_total Total number of games completed");
        sb.AppendLine("# TYPE balatro_games_completed_total counter");
        sb.AppendLine($"balatro_games_completed_total {_gamesCompleted}");
        sb.AppendLine();
        
        sb.AppendLine("# HELP balatro_players_joined_total Total number of players that joined games");
        sb.AppendLine("# TYPE balatro_players_joined_total counter");
        sb.AppendLine($"balatro_players_joined_total {_playersJoined}");
        sb.AppendLine();
        
        sb.AppendLine("# HELP balatro_votes_submitted_total Total number of votes submitted");
        sb.AppendLine("# TYPE balatro_votes_submitted_total counter");
        sb.AppendLine($"balatro_votes_submitted_total {_votesSubmitted}");
        sb.AppendLine();
        
        sb.AppendLine("# HELP balatro_rounds_played_total Total number of rounds played");
        sb.AppendLine("# TYPE balatro_rounds_played_total counter");
        sb.AppendLine($"balatro_rounds_played_total {_roundsPlayed}");
        sb.AppendLine();
        
        // Vote range metrics
        if (_lowestVote != int.MaxValue)
        {
            sb.AppendLine("# HELP balatro_lowest_vote_ever Lowest vote ever submitted");
            sb.AppendLine("# TYPE balatro_lowest_vote_ever gauge");
            sb.AppendLine($"balatro_lowest_vote_ever {_lowestVote}");
            sb.AppendLine();
        }
        
        if (_highestVote != int.MinValue)
        {
            sb.AppendLine("# HELP balatro_highest_vote_ever Highest vote ever submitted");
            sb.AppendLine("# TYPE balatro_highest_vote_ever gauge");
            sb.AppendLine($"balatro_highest_vote_ever {_highestVote}");
            sb.AppendLine();
        }
        
        // Language usage metrics
        if (_languageUsage.Any())
        {
            sb.AppendLine("# HELP balatro_language_usage_total Language usage by users");
            sb.AppendLine("# TYPE balatro_language_usage_total counter");
            foreach (var lang in _languageUsage.OrderBy(x => x.Key))
            {
                sb.AppendLine($"balatro_language_usage_total{{language=\"{lang.Key}\"}} {lang.Value}");
            }
            sb.AppendLine();
        }
        
        // Card usage metrics
        if (_cardUsage.Any())
        {
            sb.AppendLine("# HELP balatro_card_usage_total Card usage frequency");
            sb.AppendLine("# TYPE balatro_card_usage_total counter");
            foreach (var card in _cardUsage.OrderByDescending(x => x.Value).Take(20)) // Top 20 most used cards
            {
                var parts = card.Key.Split('_');
                if (parts.Length == 2)
                {
                    sb.AppendLine($"balatro_card_usage_total{{suit=\"{parts[0]}\",value=\"{parts[1]}\"}} {card.Value}");
                }
            }
            sb.AppendLine();
        }
        
        // Vote distribution
        if (_voteDistribution.Any())
        {
            sb.AppendLine("# HELP balatro_vote_distribution_total Distribution of votes by value");
            sb.AppendLine("# TYPE balatro_vote_distribution_total counter");
            foreach (var vote in _voteDistribution.OrderBy(x => x.Key))
            {
                sb.AppendLine($"balatro_vote_distribution_total{{vote=\"{vote.Key}\"}} {vote.Value}");
            }
            sb.AppendLine();
        }
        
        // Joker usage metrics
        if (_jokerUsage.Any())
        {
            sb.AppendLine("# HELP balatro_joker_usage_total Joker usage frequency");
            sb.AppendLine("# TYPE balatro_joker_usage_total counter");
            foreach (var joker in _jokerUsage.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"balatro_joker_usage_total{{joker_name=\"{joker.Key}\"}} {joker.Value}");
            }
            sb.AppendLine();
        }
        
        // Derived metrics
        var completionRate = _gamesCreated > 0 ? (double)_gamesCompleted / _gamesCreated : 0;
        sb.AppendLine("# HELP balatro_game_completion_rate Game completion rate (completed/created)");
        sb.AppendLine("# TYPE balatro_game_completion_rate gauge");
        sb.AppendLine($"balatro_game_completion_rate {completionRate:F4}");
        sb.AppendLine();
        
        var avgPlayersPerGame = _gamesCreated > 0 ? (double)_playersJoined / _gamesCreated : 0;
        sb.AppendLine("# HELP balatro_avg_players_per_game Average players per game");
        sb.AppendLine("# TYPE balatro_avg_players_per_game gauge");
        sb.AppendLine($"balatro_avg_players_per_game {avgPlayersPerGame:F2}");
        sb.AppendLine();
        
        var avgVotesPerRound = _roundsPlayed > 0 ? (double)_votesSubmitted / _roundsPlayed : 0;
        sb.AppendLine("# HELP balatro_avg_votes_per_round Average votes per round");
        sb.AppendLine("# TYPE balatro_avg_votes_per_round gauge");
        sb.AppendLine($"balatro_avg_votes_per_round {avgVotesPerRound:F2}");
        
        return sb.ToString();
    }

    
    public Dictionary<string, object> GetMetricsSummary()
    {
        return new Dictionary<string, object>
        {
            ["games_created"] = _gamesCreated,
            ["games_completed"] = _gamesCompleted,
            ["players_joined"] = _playersJoined,
            ["votes_submitted"] = _votesSubmitted,
            ["rounds_played"] = _roundsPlayed,
            ["lowest_vote"] = _lowestVote == int.MaxValue ? null : _lowestVote,
            ["highest_vote"] = _highestVote == int.MinValue ? null : _highestVote,
            ["language_usage"] = _languageUsage.ToDictionary(x => x.Key, x => x.Value),
            ["most_used_cards"] = _cardUsage.OrderByDescending(x => x.Value).Take(10).ToDictionary(x => x.Key, x => x.Value),
            ["joker_usage"] = _jokerUsage.ToDictionary(x => x.Key, x => x.Value),
            ["vote_distribution"] = _voteDistribution.ToDictionary(x => x.Key, x => x.Value)
        };
    }
}
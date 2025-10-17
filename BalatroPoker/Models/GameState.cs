using System.Text.Json.Serialization;

namespace BalatroPoker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GamePhase
{
    Setup,
    Voting,
    Revealed
}

public class GameState
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = "";
    
    [JsonPropertyName("adminCode")]
    public string AdminCode { get; set; } = "";
    
    [JsonPropertyName("playerCode")]
    public string PlayerCode { get; set; } = "";
    
    [JsonPropertyName("phase")]
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    public List<int> AllowedValues { get; set; } = new() { 1, 2, 3, 5, 8, 13, 21 };
    public int JokerCount { get; set; } = 1;
    public List<Player> Players { get; set; } = new();
    public List<Joker> ActiveJokers { get; set; } = new();
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    public static readonly int[] FibonacciValues = { 1, 2, 3, 5, 8, 13, 21, 34 };
    
    public bool AllPlayersVoted => Players.Count > 0 && Players.All(p => p.HasVoted);
    
    public double AverageVote => Players.Count > 0 ? Players.Average(p => p.FinalVote) : 0;
    public int MinVote => Players.Count > 0 ? Players.Min(p => p.FinalVote) : 0;
    public int MaxVote => Players.Count > 0 ? Players.Max(p => p.FinalVote) : 0;
}
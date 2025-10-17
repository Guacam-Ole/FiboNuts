namespace BalatroPoker.Models;

public enum JokerPosition
{
    Anywhere,
    Left,
    Right
}

public class Joker
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public JokerPosition Position { get; set; }
    public int MinJokersRequired { get; set; } = 1;
    public Func<JokerContext, List<int>> SimpleEffect { get; set; } = null!;
}

public class JokerContext
{
    public List<int> Votes { get; set; } = new();
    public List<Player> Players { get; set; } = new();
    public List<Joker> ActiveJokers { get; set; } = new();
    public int CurrentJokerIndex { get; set; }
    public Random Random { get; set; } = new();
    
    public int Min => Votes.Count > 0 ? Votes.Min() : 0;
    public int Max => Votes.Count > 0 ? Votes.Max() : 0;
    public double Average => Votes.Count > 0 ? Votes.Average() : 0;
    public int Median => Votes.Count > 0 ? Votes.OrderBy(v => v).Skip(Votes.Count / 2).First() : 0;
}
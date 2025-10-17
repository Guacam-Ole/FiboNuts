namespace BalatroPoker.Models;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public List<Card> Cards { get; set; } = new();
    public bool HasVoted { get; set; }
    public int OriginalVote { get; set; }
    public int FinalVote { get; set; }
    public List<Card> SelectedCards { get; set; } = new();
    
    public int CurrentSum => SelectedCards.Sum(c => c.Value);
}
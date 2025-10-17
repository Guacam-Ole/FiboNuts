using System.Text.Json.Serialization;

namespace BalatroPoker.Models;

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public class Card
{
    [JsonPropertyName("value")]
    public int Value { get; set; }
    
    [JsonPropertyName("suit")]
    public Suit Suit { get; set; }
    
    [JsonPropertyName("isSelected")]
    public bool IsSelected { get; set; }
    
    [JsonPropertyName("faceType")]
    public string? FaceType { get; set; }
    
    public string DisplayValue => FaceType ?? Value switch
    {
        1 => "A",
        _ => Value.ToString()
    };
    
    public string SuitSymbol => Suit switch
    {
        Suit.Hearts => "♥",
        Suit.Diamonds => "♦",
        Suit.Clubs => "♣",
        Suit.Spades => "♠",
        _ => ""
    };
    
    public bool IsRed => Suit == Suit.Hearts || Suit == Suit.Diamonds;
    public bool IsBlack => Suit == Suit.Clubs || Suit == Suit.Spades;
}
using BalatroPoker.Api.Models;

namespace BalatroPoker.Api.Services;

public class JokerProcessor
{
    private static readonly Random _random = new();
    
    public static List<Joker> GetRandomJokers(int count)
    {
        var allJokers = GetAllJokers();
        return allJokers.OrderBy(x => _random.Next()).Take(count).ToList();
    }
    
    public static List<Joker> GetAllJokers()
    {
        return new List<Joker>
        {
            new()
            {
                Name = "Joker",
                Description = "Adds +4 to each vote",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v + 4).ToList()
            },
            new()
            {
                Name = "Greedy Joker",
                Description = "Each Diamond card gives +3 bonus",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyCardSuitBonus(context, Suit.Diamonds, 3)
            },
            new()
            {
                Name = "Lusty Joker",
                Description = "Each Heart gives +3 bonus",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyCardSuitBonus(context, Suit.Hearts, 3)
            },
            new()
            {
                Name = "Wrathful Joker",
                Description = "Each Spade gives +3 bonus",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyCardSuitBonus(context, Suit.Spades, 3)
            },
            new()
            {
                Name = "Gluttonous Joker",
                Description = "Each Club gives +3 bonus",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyCardSuitBonus(context, Suit.Clubs, 3)
            },
            new()
            {
                Name = "Misprint",
                Description = "Random bonus between 1 and 23",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v + _random.Next(1, 24)).ToList()
            },
            new()
            {
                Name = "Fibonacci",
                Description = "Each card gives +8 bonus",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyCardCountBonus(context, 8)
            },
            new()
            {
                Name = "Scary Face",
                Description = "Each face card gives +30 points",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyFaceCardBonus(context, 30)
            },
            new()
            {
                Name = "Abstract Joker",
                Description = "+3 for each active joker",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v + (context.ActiveJokers.Count * 3)).ToList()
            },
            new()
            {
                Name = "Hack",
                Description = "2, 3, and 5 count twice",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplySpecificValueDoubling(context, new[] { 2, 3, 5 })
            },
            new()
            {
                Name = "Gros Michel",
                Description = "+15 bonus to all votes",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v + 15).ToList()
            },
            new()
            {
                Name = "Even Steven",
                Description = "2 and 8 give +4 bonus",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplySpecificValueBonus(context, new[] { 2, 8 }, 4)
            },
            new()
            {
                Name = "Odd Todd",
                Description = "Odd cards give +31 points",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyOddCardBonus(context, 31)
            },
            new()
            {
                Name = "Scholar",
                Description = "Aces give +20 points and multiply by 4",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyAceBonus(context)
            },
            new()
            {
                Name = "Photograph",
                Description = "First face card multiplied by 2",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyFirstFaceCardMultiplier(context)
            },
            new()
            {
                Name = "Popcorn",
                Description = "+20 bonus to all votes",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v + 20).ToList()
            },
            new()
            {
                Name = "Sock and Buscin",
                Description = "Face cards count twice",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyFaceCardDoubling(context)
            },
            new()
            {
                Name = "Hanging Chad",
                Description = "First card played three times",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => ApplyFirstCardTripling(context)
            },
            new()
            {
                Name = "Brainstorm",
                Description = "Copies the leftmost joker",
                Position = JokerPosition.Right,
                MinJokersRequired = 2,
                SimpleEffect = context => CopyLeftmostJoker(context)
            },
            new()
            {
                Name = "Blueprint",
                Description = "Copies ability of joker to the right",
                Position = JokerPosition.Left,
                MinJokersRequired = 2,
                SimpleEffect = context => CopyRightJoker(context)
            }
        };
    }
    
    public List<int> ApplyJokers(JokerContext context)
    {
        var currentVotes = context.Votes.ToList();
        
        for (int i = 0; i < context.ActiveJokers.Count; i++)
        {
            context.CurrentJokerIndex = i;
            context.Votes = currentVotes;
            
            var joker = context.ActiveJokers[i];
            currentVotes = joker.SimpleEffect(context);
        }
        
        return currentVotes;
    }
    
    public List<Joker> SelectRandomJokers(int count, int totalJokersEnabled)
    {
        var availableJokers = GetAllJokers()
            .Where(j => j.MinJokersRequired <= totalJokersEnabled)
            .ToList();
            
        if (count == 0 || !availableJokers.Any())
            return new List<Joker>();
            
        var selectedJokers = new List<Joker>();
        
        for (int i = 0; i < count && availableJokers.Any(); i++)
        {
            var joker = availableJokers[_random.Next(availableJokers.Count)];
            selectedJokers.Add(joker);
            availableJokers.Remove(joker);
        }
        
        return ArrangeJokersByPosition(selectedJokers);
    }
    
    private List<Joker> ArrangeJokersByPosition(List<Joker> jokers)
    {
        var arranged = new List<Joker>();
        
        arranged.AddRange(jokers.Where(j => j.Position == JokerPosition.Left));
        arranged.AddRange(jokers.Where(j => j.Position == JokerPosition.Anywhere));
        arranged.AddRange(jokers.Where(j => j.Position == JokerPosition.Right));
        
        return arranged;
    }
    
    private static int GetNearestFibonacci(int value)
    {
        var fib = GameState.FibonacciValues;
        var closest = fib[0];
        var minDiff = Math.Abs(value - closest);
        
        foreach (var f in fib)
        {
            var diff = Math.Abs(value - f);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = f;
            }
        }
        
        return closest;
    }
    
    public static void RestoreJokerEffects(List<Joker> jokers)
    {
        var allJokers = GetAllJokers();
        
        foreach (var joker in jokers)
        {
            var template = allJokers.FirstOrDefault(j => j.Name == joker.Name);
            if (template != null)
            {
                joker.SimpleEffect = template.SimpleEffect;
            }
        }
    }
    
    // Helper methods for new joker effects
    private static List<int> ApplyCardSuitBonus(JokerContext context, Suit targetSuit, int bonus)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var suitCount = player.SelectedCards.Count(c => c.Suit == targetSuit);
            result.Add(context.Votes[i] + (suitCount * bonus));
        }
        return result;
    }
    
    private static List<int> ApplyCardCountBonus(JokerContext context, int bonusPerCard)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var cardCount = player.SelectedCards.Count;
            result.Add(context.Votes[i] + (cardCount * bonusPerCard));
        }
        return result;
    }
    
    private static List<int> ApplyFaceCardBonus(JokerContext context, int bonus)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var faceCardCount = player.SelectedCards.Count(c => !string.IsNullOrEmpty(c.FaceType));
            result.Add(context.Votes[i] + (faceCardCount * bonus));
        }
        return result;
    }
    
    private static List<int> ApplySpecificValueDoubling(JokerContext context, int[] targetValues)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var doubleValue = 0;
            foreach (var card in player.SelectedCards)
            {
                if (targetValues.Contains(card.Value))
                {
                    doubleValue += card.Value; // Add the card value again (double it)
                }
            }
            result.Add(context.Votes[i] + doubleValue);
        }
        return result;
    }
    
    private static List<int> ApplySpecificValueBonus(JokerContext context, int[] targetValues, int bonus)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var matchingCardCount = player.SelectedCards.Count(c => targetValues.Contains(c.Value));
            result.Add(context.Votes[i] + (matchingCardCount * bonus));
        }
        return result;
    }
    
    private static List<int> ApplyOddCardBonus(JokerContext context, int bonus)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var oddCardCount = player.SelectedCards.Count(c => c.Value % 2 == 1);
            result.Add(context.Votes[i] + (oddCardCount * bonus));
        }
        return result;
    }
    
    private static List<int> ApplyAceBonus(JokerContext context)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var aceCount = player.SelectedCards.Count(c => c.Value == 1);
            var aceBonus = aceCount * 20; // +20 points per ace
            var aceMultiplier = aceCount > 0 ? 4 : 1; // multiply by 4 if aces present
            result.Add((context.Votes[i] + aceBonus) * aceMultiplier);
        }
        return result;
    }
    
    private static List<int> ApplyFirstFaceCardMultiplier(JokerContext context)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var firstFaceCard = player.SelectedCards.FirstOrDefault(c => !string.IsNullOrEmpty(c.FaceType));
            var multiplier = firstFaceCard != null ? 2 : 1;
            result.Add(context.Votes[i] * multiplier);
        }
        return result;
    }
    
    private static List<int> ApplyFaceCardDoubling(JokerContext context)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            var faceCardValue = player.SelectedCards.Where(c => !string.IsNullOrEmpty(c.FaceType)).Sum(c => c.Value);
            result.Add(context.Votes[i] + faceCardValue); // Add face card values again (double them)
        }
        return result;
    }
    
    private static List<int> ApplyFirstCardTripling(JokerContext context)
    {
        var result = new List<int>();
        for (int i = 0; i < context.Votes.Count; i++)
        {
            var player = context.Players[i];
            if (player.SelectedCards.Any())
            {
                var firstCardValue = player.SelectedCards.First().Value;
                result.Add(context.Votes[i] + (firstCardValue * 2)); // Add twice more (triple total)
            }
            else
            {
                result.Add(context.Votes[i]);
            }
        }
        return result;
    }
    
    private static List<int> CopyLeftmostJoker(JokerContext context)
    {
        if (context.ActiveJokers.Any())
        {
            var leftmostJoker = context.ActiveJokers.First();
            if (leftmostJoker != context.ActiveJokers[context.CurrentJokerIndex])
            {
                return leftmostJoker.SimpleEffect(context);
            }
        }
        return context.Votes;
    }
    
    private static List<int> CopyRightJoker(JokerContext context)
    {
        var rightJokerIndex = context.CurrentJokerIndex + 1;
        if (rightJokerIndex < context.ActiveJokers.Count)
        {
            var rightJoker = context.ActiveJokers[rightJokerIndex];
            return rightJoker.SimpleEffect(context);
        }
        return context.Votes;
    }
}
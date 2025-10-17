using BalatroPoker.Models;

namespace BalatroPoker.Services;

public class JokerProcessor
{
    private static readonly Random _random = new();
    
    public static List<Joker> GetAllJokers()
    {
        return new List<Joker>
        {
            new()
            {
                Name = "The Multiplier",
                Description = "Doubles all votes",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v * 2).ToList()
            },
            new()
            {
                Name = "The Incrementor", 
                Description = "Add +5 to everyone",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v + 5).ToList()
            },
            new()
            {
                Name = "The Halver",
                Description = "Cut everything in half",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => Math.Max(1, v / 2)).ToList()
            },
            new()
            {
                Name = "The Minimalist",
                Description = "Everyone gets minimum",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(_ => context.Min).ToList()
            },
            new()
            {
                Name = "The Maximalist",
                Description = "Everyone gets maximum", 
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(_ => context.Max).ToList()
            },
            new()
            {
                Name = "The Equalizer",
                Description = "Everyone gets average",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(_ => (int)Math.Round(context.Average)).ToList()
            },
            new()
            {
                Name = "The Anarchist",
                Description = "All votes become random",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(_ => GameState.FibonacciValues[_random.Next(GameState.FibonacciValues.Length)]).ToList()
            },
            new()
            {
                Name = "The Fibonacci Lover",
                Description = "Round to nearest Fibonacci",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => GetNearestFibonacci(v)).ToList()
            },
            new()
            {
                Name = "The Chaos",
                Description = "Multiply by random 1-3",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => v * _random.Next(1, 4)).ToList()
            },
            new()
            {
                Name = "The Inverter",
                Description = "34 minus your vote",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => Math.Max(1, 34 - v)).ToList()
            },
            new()
            {
                Name = "The Square Root",
                Description = "Square root of all votes",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 1,
                SimpleEffect = context => context.Votes.Select(v => Math.Max(1, (int)Math.Round(Math.Sqrt(v)))).ToList()
            },
            new()
            {
                Name = "The Reverser",
                Description = "Swap highest and lowest",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 2,
                SimpleEffect = context =>
                {
                    var votes = context.Votes.ToList();
                    var min = context.Min;
                    var max = context.Max;
                    return votes.Select(v => v == min ? max : v == max ? min : v).ToList();
                }
            },
            new()
            {
                Name = "The Copycat",
                Description = "Copies the right joker's effect",
                Position = JokerPosition.Left,
                MinJokersRequired = 2,
                SimpleEffect = context =>
                {
                    var rightJokerIndex = context.CurrentJokerIndex + 1;
                    if (rightJokerIndex < context.ActiveJokers.Count)
                    {
                        var rightJoker = context.ActiveJokers[rightJokerIndex];
                        return rightJoker.SimpleEffect(context);
                    }
                    return context.Votes;
                }
            },
            new()
            {
                Name = "The Mirror",
                Description = "Applies left joker's effect twice",
                Position = JokerPosition.Right,
                MinJokersRequired = 2,
                SimpleEffect = context =>
                {
                    var leftJokerIndex = context.CurrentJokerIndex - 1;
                    if (leftJokerIndex >= 0)
                    {
                        var leftJoker = context.ActiveJokers[leftJokerIndex];
                        var firstPass = leftJoker.SimpleEffect(context);
                        var newContext = new JokerContext 
                        { 
                            Votes = firstPass, 
                            Players = context.Players,
                            ActiveJokers = context.ActiveJokers,
                            CurrentJokerIndex = context.CurrentJokerIndex,
                            Random = context.Random
                        };
                        return leftJoker.SimpleEffect(newContext);
                    }
                    return context.Votes;
                }
            },
            new()
            {
                Name = "The Median Seeker",
                Description = "Votes > median become median",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 2,
                SimpleEffect = context => context.Votes.Select(v => v > context.Median ? context.Median : v).ToList()
            },
            new()
            {
                Name = "The Pessimist",
                Description = "Add highest vote to everyone",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 2,
                SimpleEffect = context => context.Votes.Select(v => v + context.Max).ToList()
            },
            new()
            {
                Name = "The Duplicator",
                Description = "Lowest vote gets doubled",
                Position = JokerPosition.Anywhere,
                MinJokersRequired = 2,
                SimpleEffect = context => context.Votes.Select(v => v == context.Min ? v * 2 : v).ToList()
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
}
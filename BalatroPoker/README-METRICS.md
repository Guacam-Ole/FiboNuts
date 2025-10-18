# Prometheus Metrics

This Blazor WebAssembly application provides basic metrics collection for monitoring game usage.

## Implementation

- **MetricsService**: Thread-safe singleton that collects game statistics
- **In-memory storage**: Metrics persist during application runtime 
- **Client-side only**: All metrics are collected in the browser

## Endpoints

- **`/metrics/debug`** - Human-readable HTML dashboard for viewing metrics

## Metrics Collected

The application tracks comprehensive game statistics including games played, player activity, language preferences, card usage, joker effectiveness, and vote patterns.

## Metrics Available

### Counters
- `balatro_games_created_total` - Total games created
- `balatro_games_completed_total` - Total games completed  
- `balatro_players_joined_total` - Total players joined
- `balatro_votes_submitted_total` - Total votes submitted
- `balatro_rounds_played_total` - Total rounds played
- `balatro_language_usage_total{language}` - Language usage by users
- `balatro_card_usage_total{suit,value}` - Card usage frequency
- `balatro_joker_usage_total{joker_name}` - Joker usage frequency
- `balatro_vote_distribution_total{vote}` - Vote distribution by value

### Gauges
- `balatro_lowest_vote_ever` - Lowest vote ever submitted
- `balatro_highest_vote_ever` - Highest vote ever submitted
- `balatro_game_completion_rate` - Game completion rate (completed/created)
- `balatro_avg_players_per_game` - Average players per game
- `balatro_avg_votes_per_round` - Average votes per round

## Sample Prometheus Configuration

```yaml
scrape_configs:
  - job_name: 'balatro-poker'
    static_configs:
      - targets: ['your-app-url.com']
    metrics_path: '/metrics'
    scrape_interval: 30s
```

## Data Persistence

All metrics are stored in memory and persist during the application runtime. Data will be reset when the browser is refreshed or the application is restarted.
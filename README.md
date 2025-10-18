# 🃏 Balatro Planning Poker

A modern, interactive planning poker application inspired by the card game mechanics of Balatro. This tool helps development teams estimate user stories and tasks using a fun, gamified approach with playing cards and randomized jokers that can modify final estimates.

## Features

- **Multi-language Support**: Available in English, German, French, Italian, and Spanish
- **Real-time Collaboration**: Live updates as team members join and vote
- **Joker System**: Random jokers add interesting twists to the estimation process
- **Card-based Interface**: Use playing cards to build your estimates instead of traditional poker cards
- **Responsive Design**: Works seamlessly on desktop and mobile devices
- **No Registration Required**: Simply create a game and share the link

## Docker Deployment

### Quick Start

1. **Build the Docker image:**
   ```bash
   docker build -t balatro-poker .
   ```

2. **Run the container:**
   ```bash
   docker run -p 8080:80 balatro-poker
   ```

3. **Access the application:**
   Open your browser and navigate to `http://localhost:8080`

### Advanced Configuration

For production deployment with custom nginx configuration:

```bash
# Build with custom tag
docker build -t balatro-poker:latest .

# Run with volume mounting for logs (optional)
docker run -d \
  -p 80:80 \
  --name balatro-poker-app \
  --restart unless-stopped \
  balatro-poker:latest
```

## How It Works

Balatro Planning Poker combines traditional planning poker estimation with an engaging card game interface:

1. **🎴 Select Cards**: Players choose playing cards from their hand to build a sum that matches one of the allowed Fibonacci values
2. **🎭 Random Jokers**: When cards are revealed, random jokers may activate and modify the final estimates
3. **📊 Final Results**: See both original estimates and joker-modified final values with team statistics

## For Administrators

### Creating and Managing Games

1. **Start a New Game**:
   - Click "Create Game" on the homepage
   - Configure allowed Fibonacci values (1, 2, 3, 5, 8, 13, 21, 34, 55, 89)
   - Set the number of jokers (0-5) that will be drawn during estimation
   - Share the player URL with your team

2. **Game Settings**:
   - **Allowed Values**: Check/uncheck which Fibonacci numbers players can estimate
   - **Joker Count**: Use the slider to set how many jokers will be activated (adds unpredictability)
   - Settings can be changed at any time during the game

3. **Managing Rounds**:
   - Monitor voting progress in real-time
   - See which players have voted (green badges) vs. still voting (yellow badges)
   - Click "Reveal Cards" when all players have voted
   - Review both original estimates and joker-modified final values
   - Start new rounds with "New Round" button

4. **Understanding Results**:
   - **Original Votes**: What players initially estimated
   - **Final Values**: After jokers have been applied (may be different)
   - **Statistics**: Average, minimum, and maximum values
   - **Active Jokers**: See which jokers activated and their effects

### Admin Interface Features

- **Player Overview**: See all connected players and their voting status
- **Real-time Updates**: Interface updates automatically as players join and vote
- **Language Support**: Admin interface adapts to selected language
- **Game Statistics**: Comprehensive voting results and team metrics

## For Players

### Joining a Game

1. **Get the Game Link**: Your Scrum Master or facilitator will share a player URL
2. **Enter Your Name**: Type your name and click "Join Game"
3. **Start Playing**: You'll receive a hand of playing cards to use for estimation

### Making Estimates

1. **Review the Story**: Your team will discuss the user story or task to be estimated
2. **Select Cards**: Click on playing cards from your hand to select them
   - Cards show their value and suit (♠ ♥ ♦ ♣)
   - Selected cards have a highlighted border
   - Your current sum is displayed in real-time

3. **Build Your Estimate**: 
   - Combine cards to reach one of the allowed Fibonacci values
   - The sum must exactly match an allowed value (shown at the top)
   - Example: Select a 3♠ and 5♥ to make an estimate of 8

4. **Submit Your Vote**: 
   - Click "Confirm Vote" when your sum matches an allowed value
   - Wait for other team members to finish voting
   - You'll see a green checkmark when your vote is submitted

### Understanding Results

1. **Vote Revelation**: 
   - When all players vote, the admin reveals the cards
   - You'll see everyone's original estimates
   - Random jokers may activate and modify the final values

2. **Joker Effects**:
   - Jokers can add, subtract, multiply, or apply other modifications
   - Your final estimate might be different from your original vote
   - Jokers add an element of fun and can spark interesting discussions

3. **Team Statistics**:
   - View average, minimum, and maximum estimates
   - Compare your estimate with the team
   - Use results to discuss and align on the final story points

### Player Interface Features

- **Real-time Updates**: See other players join and submit votes
- **Progress Tracking**: Visual progress bar shows voting completion
- **Card Interface**: Intuitive playing card selection with visual feedback
- **Multi-language**: Switch languages using the flag selector
- **Mobile Friendly**: Full functionality on smartphones and tablets

### Tips for Players

- **Think in Fibonacci**: Remember you need to build sums that match allowed values (1, 2, 3, 5, 8, 13, etc.)
- **Card Strategy**: You have multiple ways to reach the same sum (e.g., 8 = 3+5 or 8 = 2+3+3)
- **Joker Awareness**: Jokers are random, so focus on your best estimate rather than trying to predict their effects
- **Team Discussion**: Use the estimation process as a opportunity to discuss complexity and requirements

---

## Development

Built with:
- **Blazor WebAssembly** (.NET 8.0)
- **Bootstrap** for responsive UI
- **Serilog** for logging integration
- **Nginx** for production serving

## License

This project is open source and available under the MIT License.
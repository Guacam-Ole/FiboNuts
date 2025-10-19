using Microsoft.AspNetCore.Mvc;
using BalatroPoker.Api.Models;
using BalatroPoker.Api.Services;

namespace BalatroPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;
    private readonly ILogger<GameController> _logger;

    public GameController(GameService gameService, ILogger<GameController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    [HttpPost("create")]
    public ActionResult<GameState> CreateGame([FromBody] CreateGameRequest request)
    {
        try
        {
            var game = _gameService.CreateGame(request.AllowedValues, request.JokerCount);
            return Ok(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return StatusCode(500, "Error creating game");
        }
    }

    [HttpGet("admin/{adminCode}")]
    public ActionResult<GameState> GetGameByAdminCode(string adminCode)
    {
        var game = _gameService.GetGameByAdminCode(adminCode);
        if (game == null)
        {
            return NotFound("Game not found");
        }
        return Ok(game);
    }

    [HttpGet("player/{playerCode}")]
    public ActionResult<GameState> GetGameByPlayerCode(string playerCode)
    {
        var game = _gameService.GetGameByPlayerCode(playerCode);
        if (game == null)
        {
            return NotFound("Game not found");
        }
        return Ok(game);
    }

    [HttpPost("player/{playerCode}/join")]
    public ActionResult<Player> JoinGame(string playerCode, [FromBody] JoinGameRequest request)
    {
        try
        {
            var player = _gameService.AddPlayer(playerCode, request.Name);
            if (player == null)
            {
                return NotFound("Game not found");
            }
            return Ok(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            return StatusCode(500, "Error joining game");
        }
    }

    [HttpPost("player/{playerCode}/vote")]
    public ActionResult SubmitVote(string playerCode, [FromBody] SubmitVoteRequest request)
    {
        try
        {
            var success = _gameService.SubmitVote(playerCode, request.PlayerId, request.SelectedCards);
            if (!success)
            {
                return BadRequest("Invalid vote or game not found");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting vote");
            return StatusCode(500, "Error submitting vote");
        }
    }

    [HttpPost("admin/{adminCode}/reveal")]
    public ActionResult RevealCards(string adminCode)
    {
        try
        {
            var success = _gameService.RevealCards(adminCode);
            if (!success)
            {
                return BadRequest("Cannot reveal cards or game not found");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing cards");
            return StatusCode(500, "Error revealing cards");
        }
    }

    [HttpPost("admin/{adminCode}/start-voting")]
    public ActionResult StartVoting(string adminCode)
    {
        try
        {
            var success = _gameService.StartVoting(adminCode);
            if (!success)
            {
                return NotFound("Game not found");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting voting");
            return StatusCode(500, "Error starting voting");
        }
    }

    [HttpPost("admin/{adminCode}/new-round")]
    public ActionResult StartNewRound(string adminCode)
    {
        try
        {
            var success = _gameService.StartNewRound(adminCode);
            if (!success)
            {
                return NotFound("Game not found");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new round");
            return StatusCode(500, "Error starting new round");
        }
    }
}

// Request DTOs
public class CreateGameRequest
{
    public List<int> AllowedValues { get; set; } = new();
    public int JokerCount { get; set; }
}

public class JoinGameRequest
{
    public string Name { get; set; } = "";
}

public class SubmitVoteRequest
{
    public string PlayerId { get; set; } = "";
    public List<Card> SelectedCards { get; set; } = new();
}
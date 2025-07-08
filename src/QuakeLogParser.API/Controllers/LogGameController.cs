using Microsoft.AspNetCore.Mvc;
using QuakeLogParser.Application.Interfaces;

namespace QuakeLogParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogGameController : ControllerBase
{
    private readonly ILogParserService _parserService;

    public LogGameController(ILogParserService parserService)
    {
        _parserService = parserService;
    }

    [HttpGet("games")]
    public IActionResult GetGames()
    {
        var games = _parserService.ParseLogFile("games.log");
        return Ok(games);
    }

    [HttpGet("games/{name}")]
    public IActionResult GetGameByName(string name)
    {
        var game = _parserService.GetGameByName(name);

        if (game == null)
            return NotFound($"Jogo '{name}' não encontrado.");

        return Ok(game);
    }
}

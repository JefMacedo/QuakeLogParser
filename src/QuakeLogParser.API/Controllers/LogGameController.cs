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
        var games = _parserService.ParseLogFile();
        return Ok(games);
    }
}

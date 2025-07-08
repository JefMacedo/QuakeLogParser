using QuakeLogParser.Infrastructure.Services;
using QuakeLogParser.Application.DTOs;
using Xunit;

namespace QuakeLogParser.Tests.Services;

public class LogParserServiceTests
{
    private readonly LogParserService _parserService = new();

    private string GetLogFilePath(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, $"../../../Fixtures/sampleLogs/{name}");
    }

    [Fact]
    public void ParseLogFile_DeveAgruparJogosCorretamente()
    {
        // Arrange
        var path = GetLogFilePath("games_multiple.log");

        // Act
        var games = _parserService.ParseLogFile(path).ToList();

        // Assert
        Assert.True(games.Count >= 2);
        Assert.All(games, g => Assert.StartsWith("game_", g.Name));
    }

    [Fact]
    public void ParseLogFile_DeveIgnorarLinhasInvalidas()
    {
        // Arrange
        var path = GetLogFilePath("games_invalid.log");

        // Act
        var games = _parserService.ParseLogFile(path).ToList();

        // Assert
        Assert.Single(games);
        Assert.True(games[0].TotalKills >= 0);
        Assert.NotNull(games[0].Players);
        Assert.NotNull(games[0].Kills);
    }

    [Fact]
    public void ParseLogFile_CalculaKillsCorretamenteComWorld()
    {
        // Arrange
        var path = GetLogFilePath("games_with_world.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        Assert.True(game.Kills.Values.Any(k => k < 0), "Esperado kill negativa por <world>");
        Assert.DoesNotContain("<world>", game.Players);
    }

    [Fact]
    public void ParseLogFile_NaoAdicionaWorldComoPlayer()
    {
        // Arrange
        var path = GetLogFilePath("games_with_world.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        Assert.DoesNotContain("<world>", game.Players);
    }

    [Fact]
    public void ParseLogFile_NaoAdicionaJogadoresRepetidos()
    {
        // Arrange
        var path = GetLogFilePath("games_with_repeats.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        var uniquePlayers = game.Players.Distinct().ToList();
        Assert.Equal(uniquePlayers.Count, game.Players.Count);
    }

    [Fact]
    public void ParseLogFile_DeveRetornarTotalKillsCorreto()
    {
        // Arrange
        var path = GetLogFilePath("games_single.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        Assert.Equal(5, game.TotalKills);
    }
}

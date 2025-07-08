using QuakeLogParser.Infrastructure.Services;
using QuakeLogParser.Application.DTOs;
using Xunit;
using FluentAssertions;

namespace QuakeLogParser.Tests.Services;

public class LogParserServiceTests
{
    private readonly LogParserService _parserService = new();

    private string GetLogFilePath(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, $"../../../Fixtures/sampleLogs/{name}");
    }

    #region ParseLogFile Tests

    [Fact]
    public void ParseLogFile_DeveAgruparJogosCorretamente()
    {
        // Arrange
        var path = GetLogFilePath("games_multiple.log");

        // Act
        var games = _parserService.ParseLogFile(path).ToList();

        // Assert
        games.Should().HaveCountGreaterOrEqualTo(2);
        games.Should().OnlyContain(g => g.Name.StartsWith("game_"));
        games.Select(g => g.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public void ParseLogFile_DeveIgnorarLinhasInvalidas()
    {
        // Arrange
        var path = GetLogFilePath("games_invalid.log");

        // Act
        var games = _parserService.ParseLogFile(path).ToList();

        // Assert
        games.Should().HaveCount(1);
        games[0].TotalKills.Should().BeGreaterOrEqualTo(0);
        games[0].Players.Should().NotBeNull();
        games[0].Kills.Should().NotBeNull();
    }

    [Fact]
    public void ParseLogFile_CalculaKillsCorretamenteComWorld()
    {
        // Arrange
        var path = GetLogFilePath("games_with_world.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        game.Kills.Values.Should().Contain(k => k < 0, "Esperado kill negativa por <world>");
        game.Players.Should().NotContain("<world>");
    }

    [Fact]
    public void ParseLogFile_NaoAdicionaWorldComoPlayer()
    {
        // Arrange
        var path = GetLogFilePath("games_with_world.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        game.Players.Should().NotContain("<world>");
        game.Kills.Keys.Should().NotContain("<world>");
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
        game.Players.Should().HaveCount(uniquePlayers.Count);
        game.Players.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ParseLogFile_DeveRetornarTotalKillsCorreto()
    {
        // Arrange
        var path = GetLogFilePath("games_single.log");

        // Act
        var game = _parserService.ParseLogFile(path).First();

        // Assert
        game.TotalKills.Should().Be(5);
        game.Name.Should().Be("game_1");
    }

    [Fact]
    public void ParseLogFile_ComArquivoInexistente_DeveLancarFileNotFoundException()
    {
        // Arrange
        var inexistentPath = "arquivo_inexistente.log";

        // Act & Assert
        _parserService.Invoking(s => s.ParseLogFile(inexistentPath))
            .Should().Throw<FileNotFoundException>()
            .WithMessage("*Arquivo de log não encontrado em:*");
    }

    [Fact]
    public void ParseLogFile_ComArquivoVazio_DeveRetornarListaVazia()
    {
        // Arrange
        var emptyFilePath = Path.GetTempFileName();
        File.WriteAllText(emptyFilePath, string.Empty);

        try
        {
            // Act
            var games = _parserService.ParseLogFile(emptyFilePath).ToList();

            // Assert
            games.Should().BeEmpty();
        }
        finally
        {
            File.Delete(emptyFilePath);
        }
    }

    [Fact]
    public void ParseLogFile_ComApenasInitGame_DeveCriarJogoSemKills()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "  0:00 InitGame:");

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFilePath).ToList();

            // Assert
            games.Should().HaveCount(1);
            games[0].Name.Should().Be("game_1");
            games[0].TotalKills.Should().Be(0);
            games[0].Players.Should().BeEmpty();
            games[0].Kills.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    #endregion

    #region GetGameByName Tests

    [Fact]
    public void GetGameByName_ComNomeExistente_DeveRetornarJogoCorreto()
    {
        // Act
        var game = _parserService.GetGameByName("game_1");

        // Assert
        game.Should().NotBeNull();
        game!.Name.Should().Be("game_1");
        game.TotalKills.Should().BeGreaterOrEqualTo(0);
        game.Players.Should().NotBeNull();
        game.Kills.Should().NotBeNull();
    }

    [Fact]
    public void GetGameByName_ComNomeInexistente_DeveRetornarNull()
    {
        // Act
        var game = _parserService.GetGameByName("game_999");

        // Assert
        game.Should().BeNull();
    }

    [Fact]
    public void GetGameByName_ComNomeEmCaseIncorreto_DeveRetornarJogo()
    {
        // Act
        var gameLower = _parserService.GetGameByName("game_1");
        var gameUpper = _parserService.GetGameByName("GAME_1");
        var gameMixed = _parserService.GetGameByName("Game_1");

        // Assert
        gameLower.Should().NotBeNull();
        gameUpper.Should().NotBeNull();
        gameMixed.Should().NotBeNull();
        
        gameUpper!.Name.Should().Be(gameLower!.Name);
        gameMixed!.Name.Should().Be(gameLower.Name);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ParseLogFile_E_GetGameByName_DevemRetornarMesmosDados()
    {
        // Arrange
        var allGames = _parserService.ParseLogFile("games.log").ToList();
        var firstGame = allGames.FirstOrDefault();

        // Act
        var gameByName = _parserService.GetGameByName(firstGame.Name);

        // Assert
        gameByName.Should().NotBeNull();
        gameByName!.Name.Should().Be(firstGame.Name);
        gameByName.TotalKills.Should().Be(firstGame.TotalKills);
        gameByName.Players.Should().BeEquivalentTo(firstGame.Players);
        gameByName.Kills.Should().BeEquivalentTo(firstGame.Kills);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseLogFile_ComLinhasKillMalFormadas_DeveIgnorarEContinuar()
    {
        // Arrange
        var path = GetLogFilePath("games_malformed.log");

        // Act
        var games = _parserService.ParseLogFile(path).ToList();

        // Assert
        games.Should().HaveCount(1);
        games[0].TotalKills.Should().Be(2); // Apenas as linhas válidas
        games[0].Players.Should().Contain("Isgalamido");
        games[0].Players.Should().Contain("Dono da Bola");
    }

    [Fact]
    public void ParseLogFile_ComJogadorMatandoEMorrendo_DeveContabilizarCorretamente()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        var logContent = @"  0:00 InitGame:
  0:01 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET
  0:02 Kill: 3 2 7: PlayerB killed PlayerA by MOD_RAILGUN
  0:03 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET";
        File.WriteAllText(tempFilePath, logContent);

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFilePath).ToList();

            // Assert
            games[0].Kills["PlayerA"].Should().Be(2);
            games[0].Kills["PlayerB"].Should().Be(1);
            games[0].TotalKills.Should().Be(3);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    #endregion
}

using QuakeLogParser.Infrastructure.Services;
using QuakeLogParser.Application.DTOs;
using FluentAssertions;
using System.IO;

namespace QuakeLogParser.Tests.Services;

/// <summary>
/// Testes unitários focados em cenários específicos e edge cases do LogParserService
/// </summary>
public class LogParserServiceUnitTests
{
    private readonly LogParserService _parserService = new();

    #region Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetGameByName_ComNomeInvalidoOuVazio_DeveRetornarNull(string? gameName)
    {
        // Act
        var result = _parserService.GetGameByName(gameName!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseLogFile_ComCaminhoInvalido_DeveLancarFileNotFoundException()
    {
        // Arrange
        var invalidPath = "C:\\arquivo_que_nao_existe_123456.log";

        // Act & Assert
        _parserService.Invoking(s => s.ParseLogFile(invalidPath))
            .Should().Throw<FileNotFoundException>()
            .WithMessage("*Arquivo de log não encontrado em:*");
    }

    #endregion

    #region Game Parsing Logic Tests

    [Fact]
    public void ParseLogFile_ComJogoSemKills_DeveCriarJogoVazio()
    {
        // Arrange
        var tempFile = CreateTempLogFile("  0:00 InitGame:");

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFile).ToList();

            // Assert
            games.Should().HaveCount(1);
            games[0].Name.Should().Be("game_1");
            games[0].TotalKills.Should().Be(0);
            games[0].Players.Should().BeEmpty();
            games[0].Kills.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseLogFile_ComKillsAntesDePrimeiroInitGame_DeveIgnorarKills()
    {
        // Arrange
        var logContent = @"  0:01 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET
  0:02 Kill: 3 2 7: PlayerB killed PlayerA by MOD_RAILGUN
  1:00 InitGame:
  1:01 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET";

        var tempFile = CreateTempLogFile(logContent);

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFile).ToList();

            // Assert
            games.Should().HaveCount(1);
            games[0].TotalKills.Should().Be(1, "apenas o kill após InitGame deve ser contado");
            games[0].Players.Should().Contain("PlayerA");
            games[0].Players.Should().Contain("PlayerB");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseLogFile_ComWorldKills_DeveDecrementarKillsDoPlayer()
    {
        // Arrange
        var logContent = @"  0:00 InitGame:
  0:01 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET
  0:02 Kill: 1022 2 22: <world> killed PlayerA by MOD_TRIGGER_HURT
  0:03 Kill: 1022 2 22: <world> killed PlayerA by MOD_TRIGGER_HURT";

        var tempFile = CreateTempLogFile(logContent);

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFile).ToList();

            // Assert
            games[0].Kills["PlayerA"].Should().Be(-1, "1 kill - 2 mortes pelo world");
            games[0].Kills["PlayerB"].Should().Be(0, "morreu mas não matou ninguém");
            games[0].TotalKills.Should().Be(3);
            games[0].Players.Should().NotContain("<world>");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Multiple Games Tests

    [Fact]
    public void ParseLogFile_ComMultiplosJogos_DeveReiniciarContadoresPorJogo()
    {
        // Arrange
        var logContent = @"  0:00 InitGame:
  0:01 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET
  1:00 InitGame:
  1:01 Kill: 4 5 7: PlayerC killed PlayerD by MOD_RAILGUN
  1:02 Kill: 5 4 8: PlayerD killed PlayerC by MOD_SHOTGUN";

        var tempFile = CreateTempLogFile(logContent);

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFile).ToList();

            // Assert
            games.Should().HaveCount(2);

            // Primeiro jogo
            games[0].Name.Should().Be("game_1");
            games[0].TotalKills.Should().Be(1);
            games[0].Players.Should().Contain("PlayerA").And.Contain("PlayerB");
            games[0].Players.Should().NotContain("PlayerC").And.NotContain("PlayerD");

            // Segundo jogo
            games[1].Name.Should().Be("game_2");
            games[1].TotalKills.Should().Be(2);
            games[1].Players.Should().Contain("PlayerC").And.Contain("PlayerD");
            games[1].Players.Should().NotContain("PlayerA").And.NotContain("PlayerB");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetGameByName_ComJogoNoMeio_DeveRetornarJogoCorreto()
    {
        // Act - Busca um jogo que existe no arquivo real
        var game = _parserService.GetGameByName("game_2");

        // Assert
        game.Should().NotBeNull();
        game!.Name.Should().Be("game_2");
        game.TotalKills.Should().BeGreaterOrEqualTo(0);
        game.Players.Should().NotBeNull();
        game.Kills.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseLogFile_ComJogoVazioEntreJogosComDados_DeveProcessarCorretamente()
    {
        // Arrange
        var logContent = @"  0:00 InitGame:
  0:01 Kill: 2 3 6: PlayerA killed PlayerB by MOD_ROCKET
  1:00 InitGame:
  2:00 InitGame:
  2:01 Kill: 4 5 7: PlayerC killed PlayerD by MOD_RAILGUN";

        var tempFile = CreateTempLogFile(logContent);

        try
        {
            // Act
            var games = _parserService.ParseLogFile(tempFile).ToList();

            // Assert
            games.Should().HaveCount(3);
            games[0].TotalKills.Should().Be(1);
            games[1].TotalKills.Should().Be(0);
            games[2].TotalKills.Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Helper Methods

    private string CreateTempLogFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    #endregion
}

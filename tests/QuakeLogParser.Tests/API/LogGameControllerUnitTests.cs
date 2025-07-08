using Microsoft.AspNetCore.Mvc;
using QuakeLogParser.API.Controllers;
using QuakeLogParser.Application.Interfaces;
using QuakeLogParser.Application.DTOs;
using Moq;
using FluentAssertions;

namespace QuakeLogParser.Tests.API;

/// <summary>
/// Testes unitários do LogGameController usando mocks para isolar a lógica do controller
/// </summary>
public class LogGameControllerUnitTests
{
    private readonly Mock<ILogParserService> _mockParserService;
    private readonly LogGameController _controller;

    public LogGameControllerUnitTests()
    {
        _mockParserService = new Mock<ILogParserService>();
        _controller = new LogGameController(_mockParserService.Object);
    }

    #region GetGames Tests

    [Fact]
    public void GetGames_ComServicoRetornandoDados_DeveRetornar200ComListaDeJogos()
    {
        // Arrange
        var expectedGames = new List<GameReportDto>
        {
            new() { Name = "game_1", TotalKills = 5, Players = ["PlayerA", "PlayerB"], Kills = new() { ["PlayerA"] = 3, ["PlayerB"] = 2 } },
            new() { Name = "game_2", TotalKills = 3, Players = ["PlayerC"], Kills = new() { ["PlayerC"] = 3 } }
        };

        _mockParserService.Setup(x => x.ParseLogFile("games.log"))
            .Returns(expectedGames);

        // Act
        var result = _controller.GetGames();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedGames);
        
        _mockParserService.Verify(x => x.ParseLogFile("games.log"), Times.Once);
    }

    [Fact]
    public void GetGames_ComServicoRetornandoListaVazia_DeveRetornar200ComListaVazia()
    {
        // Arrange
        var emptyGames = new List<GameReportDto>();
        _mockParserService.Setup(x => x.ParseLogFile("games.log"))
            .Returns(emptyGames);

        // Act
        var result = _controller.GetGames();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(emptyGames);
    }

    [Fact]
    public void GetGames_ComServicoLancandoException_DevePropagar()
    {
        // Arrange
        _mockParserService.Setup(x => x.ParseLogFile("games.log"))
            .Throws(new FileNotFoundException("Arquivo não encontrado"));

        // Act & Assert
        _controller.Invoking(c => c.GetGames())
            .Should().Throw<FileNotFoundException>()
            .WithMessage("Arquivo não encontrado");
    }

    #endregion

    #region GetGameByName Tests

    [Fact]
    public void GetGameByName_ComJogoExistente_DeveRetornar200ComJogo()
    {
        // Arrange
        var gameName = "game_1";
        var expectedGame = new GameReportDto
        {
            Name = gameName,
            TotalKills = 10,
            Players = ["PlayerA", "PlayerB", "PlayerC"],
            Kills = new() { ["PlayerA"] = 5, ["PlayerB"] = 3, ["PlayerC"] = 2 }
        };

        _mockParserService.Setup(x => x.GetGameByName(gameName))
            .Returns(expectedGame);

        // Act
        var result = _controller.GetGameByName(gameName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedGame);
        
        _mockParserService.Verify(x => x.GetGameByName(gameName), Times.Once);
    }

    [Fact]
    public void GetGameByName_ComJogoInexistente_DeveRetornar404()
    {
        // Arrange
        var gameName = "game_inexistente";
        _mockParserService.Setup(x => x.GetGameByName(gameName))
            .Returns((GameReportDto?)null);

        // Act
        var result = _controller.GetGameByName(gameName);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be($"Jogo '{gameName}' não encontrado.");
        
        _mockParserService.Verify(x => x.GetGameByName(gameName), Times.Once);
    }

    [Fact]
    public void GetGameByName_ComServicoLancandoException_DevePropagar()
    {
        // Arrange
        var gameName = "game_1";
        _mockParserService.Setup(x => x.GetGameByName(gameName))
            .Throws(new FileNotFoundException("Arquivo não encontrado"));

        // Act & Assert
        _controller.Invoking(c => c.GetGameByName(gameName))
            .Should().Throw<FileNotFoundException>()
            .WithMessage("Arquivo não encontrado");
    }

    [Theory]
    [InlineData("game_1")]
    [InlineData("GAME_1")]
    [InlineData("Game_1")]
    public void GetGameByName_ComDiferentesCases_DevePassarNomeExatoParaServico(string gameName)
    {
        // Arrange
        _mockParserService.Setup(x => x.GetGameByName(It.IsAny<string>()))
            .Returns((GameReportDto?)null);

        // Act
        var result = _controller.GetGameByName(gameName);

        // Assert
        _mockParserService.Verify(x => x.GetGameByName(gameName), Times.Once);
    }

    #endregion

    #region Dependency Injection Tests

    [Fact]
    public void Constructor_ComServicoValido_DeveCriarInstancia()
    {
        // Arrange
        var mockService = new Mock<ILogParserService>();

        // Act
        var controller = new LogGameController(mockService.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetGameByName_ComCaracteresEspeciais_DevePassarParaServico()
    {
        // Arrange
        var gameNameWithSpecialChars = "game@#$%_1";
        _mockParserService.Setup(x => x.GetGameByName(gameNameWithSpecialChars))
            .Returns((GameReportDto?)null);

        // Act
        var result = _controller.GetGameByName(gameNameWithSpecialChars);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockParserService.Verify(x => x.GetGameByName(gameNameWithSpecialChars), Times.Once);
    }

    [Fact]
    public void GetGameByName_ComNomeComEspacos_DevePassarParaServico()
    {
        // Arrange
        var gameNameWithSpaces = " game_1 ";
        _mockParserService.Setup(x => x.GetGameByName(gameNameWithSpaces))
            .Returns((GameReportDto?)null);

        // Act
        var result = _controller.GetGameByName(gameNameWithSpaces);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockParserService.Verify(x => x.GetGameByName(gameNameWithSpaces), Times.Once);
    }

    [Fact]
    public void GetGames_DeveSempreChamarParseLogFileComGamesLog()
    {
        // Arrange
        _mockParserService.Setup(x => x.ParseLogFile("games.log"))
            .Returns(new List<GameReportDto>());

        // Act
        _controller.GetGames();

        // Assert
        _mockParserService.Verify(x => x.ParseLogFile("games.log"), Times.Once);
        _mockParserService.Verify(x => x.ParseLogFile(It.Is<string>(s => s != "games.log")), Times.Never);
    }

    #endregion
}

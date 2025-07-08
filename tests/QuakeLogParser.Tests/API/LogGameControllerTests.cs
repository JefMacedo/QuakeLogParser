using Microsoft.AspNetCore.Mvc.Testing;
using QuakeLogParser.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace QuakeLogParser.Tests.API;

public class LogGameControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LogGameControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region GetGames Tests

    [Fact]
    public async Task GetGames_DeveRetornar200ComJogos()
    {
        // Act
        var response = await _client.GetAsync("/api/loggame/games");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("game_");
    }

    [Fact]
    public async Task GetGames_DeveRetornarListaDeGameReportDto()
    {
        // Act
        var response = await _client.GetAsync("/api/loggame/games");
        
        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var games = await response.Content.ReadFromJsonAsync<List<GameReportDto>>();
        
        games.Should().NotBeNull();
        games.Should().NotBeEmpty();
        games.Should().OnlyContain(g => g.Name.StartsWith("game_"));
        games.Should().OnlyContain(g => g.TotalKills >= 0);
        games.Should().OnlyContain(g => g.Players != null);
        games.Should().OnlyContain(g => g.Kills != null);
    }

    #endregion

    #region GetGameByName Tests

    [Fact]
    public async Task GetGameByName_ComJogoExistente_DeveRetornar200()
    {
        // Arrange - Primeiro busca todos os jogos para pegar um nome válido
        var responseAll = await _client.GetAsync("/api/loggame/games");
        var games = await responseAll.Content.ReadFromJsonAsync<List<GameReportDto>>();
        var firstGameName = games?.FirstOrDefault()?.Name;

        firstGameName.Should().NotBeNull("Deve existir pelo menos um jogo para testar");

        // Act
        var response = await _client.GetAsync($"/api/loggame/games/{firstGameName}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var game = await response.Content.ReadFromJsonAsync<GameReportDto>();
        
        game.Should().NotBeNull();
        game!.Name.Should().Be(firstGameName);
        game.TotalKills.Should().BeGreaterOrEqualTo(0);
        game.Players.Should().NotBeNull();
        game.Kills.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGameByName_ComJogoInexistente_DeveRetornar404()
    {
        // Act
        var response = await _client.GetAsync("/api/loggame/games/game_inexistente");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Jogo 'game_inexistente' não encontrado");
    }

    [Fact]
    public async Task GetGameByName_ComNomeComEspacos_DeveRetornar404()
    {
        // Act
        var response = await _client.GetAsync("/api/loggame/games/%20game_1%20");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGameByName_ComCaracteresEspeciais_DeveRetornar404()
    {
        // Act
        var response = await _client.GetAsync("/api/loggame/games/game@#$%");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task GetGames_E_GetGameByName_DevemRetornarDadosConsistentes()
    {
        // Arrange - Busca todos os jogos
        var responseAll = await _client.GetAsync("/api/loggame/games");
        var allGames = await responseAll.Content.ReadFromJsonAsync<List<GameReportDto>>();
        
        allGames.Should().NotBeNull();
        allGames.Should().NotBeEmpty();

        var targetGame = allGames!.First();

        // Act - Busca o jogo específico
        var responseSpecific = await _client.GetAsync($"/api/loggame/games/{targetGame.Name}");
        var specificGame = await responseSpecific.Content.ReadFromJsonAsync<GameReportDto>();

        // Assert - Os dados devem ser idênticos
        specificGame.Should().NotBeNull();
        specificGame!.Name.Should().Be(targetGame.Name);
        specificGame.TotalKills.Should().Be(targetGame.TotalKills);
        specificGame.Players.Should().BeEquivalentTo(targetGame.Players);
        specificGame.Kills.Should().BeEquivalentTo(targetGame.Kills);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetGames_DeveResponderEmTempoAceitavel()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/loggame/games");

        // Assert
        stopwatch.Stop();
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "API deve responder em menos de 5 segundos");
    }

    [Fact]
    public async Task GetGameByName_DeveResponderEmTempoAceitavel()
    {
        // Arrange
        var responseAll = await _client.GetAsync("/api/loggame/games");
        var games = await responseAll.Content.ReadFromJsonAsync<List<GameReportDto>>();
        var firstGameName = games?.FirstOrDefault()?.Name;

        firstGameName.Should().NotBeNull();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync($"/api/loggame/games/{firstGameName}");

        // Assert
        stopwatch.Stop();
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "GetGameByName deve ser mais rápido que GetGames");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetGameByName_ComNumeroDoJogoInvalido_DeveRetornar404()
    {
        // Act
        var response = await _client.GetAsync("/api/loggame/games/game_999999");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGames_ComMultiplasRequisicoesConcorrentes_DeveManterConsistencia()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Act // Requisições concorrentes
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/api/loggame/games"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        contents.Should().OnlyContain(c => c == contents[0]);
    }

    #endregion
}

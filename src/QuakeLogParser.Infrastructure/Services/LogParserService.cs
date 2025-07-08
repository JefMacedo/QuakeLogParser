using QuakeLogParser.Application.DTOs;
using QuakeLogParser.Application.Interfaces;
using System.IO;

namespace QuakeLogParser.Infrastructure.Services;

public class LogParserService : ILogParserService
{
    private const string DefaultLogFileName = "games.log";
    private const string WorldPlayer = "<world>";
    private const string GameNamePrefix = "game_";

    public IEnumerable<GameReportDto> ParseLogFile(string path)
    {
        var resolvedPath = ResolveLogFilePath(path);
        ValidateFileExists(resolvedPath);

        var lines = File.ReadAllLines(resolvedPath);
        return ParseLogLines(lines);
    }

    public GameReportDto? GetGameByName(string name)
    {
        var resolvedPath = ResolveLogFilePath(DefaultLogFileName);
        ValidateFileExists(resolvedPath);

        var lines = File.ReadAllLines(resolvedPath);
        return ParseLogLinesUntilGameFound(lines, name);
    }

    private string ResolveLogFilePath(string path)
    {
        if (path == DefaultLogFileName)
            return Path.Combine(AppContext.BaseDirectory, "../../../../../GameLog/games.log");

        return path;
    }

    private static void ValidateFileExists(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo de log não encontrado em: {path}");
    }

    private IEnumerable<GameReportDto> ParseLogLines(string[] lines)
    {
        var games = new List<GameReportDto>();
        var parseContext = new GameParseContext();

        foreach (var line in lines)
        {
            if (IsInitGameLine(line))
            {
                FinalizeCurrentGame(parseContext);
                StartNewGame(parseContext, games);
            }
            else if (IsKillLine(line) && parseContext.CurrentGame != null)
            {
                ProcessKillLine(line, parseContext);
            }
        }

        FinalizeCurrentGame(parseContext);
        return games;
    }

    private GameReportDto? ParseLogLinesUntilGameFound(string[] lines, string targetGameName)
    {
        var parseContext = new GameParseContext();
        GameReportDto? targetGame = null;

        foreach (var line in lines)
        {
            if (IsInitGameLine(line))
            {
                FinalizeCurrentGame(parseContext);
                StartNewGame(parseContext, new List<GameReportDto>());

                if (parseContext.CurrentGame?.Name.Equals(targetGameName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    targetGame = parseContext.CurrentGame;
                }
            }
            else if (IsKillLine(line) && parseContext.CurrentGame != null)
            {
                ProcessKillLine(line, parseContext);
            }

            if (targetGame != null && parseContext.CurrentGame != targetGame)
            {
                break;
            }
        }

        if (parseContext.CurrentGame == targetGame)
        {
            FinalizeCurrentGame(parseContext);
        }

        return targetGame;
    }

    private static bool IsInitGameLine(string line) => line.Contains("InitGame");

    private static bool IsKillLine(string line) => line.Contains("Kill:");

    private void FinalizeCurrentGame(GameParseContext context)
    {
        if (context.CurrentGame != null)
        {
            context.CurrentGame.Players = context.PlayerSet.ToList();
        }
    }

    private void StartNewGame(GameParseContext context, List<GameReportDto> games)
    {
        context.CurrentGame = new GameReportDto
        {
            Name = $"{GameNamePrefix}{++context.GameCount}"
        };

        games.Add(context.CurrentGame);
        context.PlayerSet = new HashSet<string>();
    }

    private void ProcessKillLine(string line, GameParseContext context)
    {
        var currentGame = context.CurrentGame!;
        
        var killInfo = ExtractKillInfo(line);
        if (killInfo == null) return;

        currentGame.TotalKills++;
        var (killer, killed) = killInfo.Value;

        UpdatePlayerSet(context.PlayerSet, killer, killed);
        UpdateKillsStats(currentGame, killer, killed);
    }

    private (string killer, string killed)? ExtractKillInfo(string line)
    {
        var killLineStart = line.IndexOf("Kill:") + "Kill:".Length;
        var killContent = line.Substring(killLineStart).Trim();

        var colonIndex = killContent.IndexOf(":");
        if (colonIndex == -1) return null;

        var description = killContent[(colonIndex + 1)..].Trim();
        var byIndex = description.IndexOf(" by ");
        if (byIndex == -1) return null;

        var killInfo = description[..byIndex].Trim();
        var killerKilled = killInfo.Split(" killed ");

        if (killerKilled.Length != 2) return null;

        var killer = killerKilled[0].Trim();
        var killed = killerKilled[1].Trim();

        return (killer, killed);
    }

    private static void UpdatePlayerSet(HashSet<string> playerSet, string killer, string killed)
    {
        if (killed != WorldPlayer)
            playerSet.Add(killed);
        if (killer != WorldPlayer)
            playerSet.Add(killer);
    }

    private static void UpdateKillsStats(GameReportDto game, string killer, string killed)
    {
        // Garante que ambos os jogadores tenham entradas no dicionário
        if (killer != WorldPlayer && !game.Kills.ContainsKey(killer))
        {
            game.Kills[killer] = 0;
        }
        if (killed != WorldPlayer && !game.Kills.ContainsKey(killed))
        {
            game.Kills[killed] = 0;
        }

        if (killer == WorldPlayer)
        {
            game.Kills[killed] = game.Kills.GetValueOrDefault(killed, 0) - 1;
        }
        else
        {
            game.Kills[killer] = game.Kills.GetValueOrDefault(killer, 0) + 1;
        }
    }

    private class GameParseContext
    {
        public GameReportDto? CurrentGame { get; set; }
        public HashSet<string> PlayerSet { get; set; } = new();
        public int GameCount { get; set; }
    }
}

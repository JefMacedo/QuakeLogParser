using QuakeLogParser.Application.DTOs;
using QuakeLogParser.Application.Interfaces;
using System.IO;

namespace QuakeLogParser.Infrastructure.Services;

public class LogParserService : ILogParserService
{
    public IEnumerable<GameReportDto> ParseLogFile(string path)
    {
        if (path == "games.log")
            path = Path.Combine(AppContext.BaseDirectory, "../../../../../GameLog/games.log");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo de log não encontrado em: {path}");

        var lines = File.ReadAllLines(path);
        var games = new List<GameReportDto>();
        GameReportDto? currentGame = null;
        HashSet<string> playerSet = new();
        int gameCount = 0;

        foreach (var line in lines)
        {
            if (line.Contains("InitGame"))
            {
                // Finaliza o jogo anterior antes de iniciar o novo
                if (currentGame != null)
                    currentGame.Players = playerSet.ToList();

                currentGame = new GameReportDto
                {
                    Name = $"game_{++gameCount}"
                };

                games.Add(currentGame);
                playerSet = new HashSet<string>(); // Reinicia para novo jogo
            }
            else if (line.Contains("Kill:") && currentGame != null)
            {
                currentGame.TotalKills++;

                var killLineStart = line.IndexOf("Kill:") + "Kill:".Length;
                var killContent = line.Substring(killLineStart).Trim();

                var colonIndex = killContent.IndexOf(":");
                if (colonIndex == -1)
                    continue;

                var description = killContent[(colonIndex + 1)..].Trim();
                var byIndex = description.IndexOf(" by ");
                if (byIndex == -1)
                    continue;

                var killInfo = description[..byIndex].Trim();
                var killerKilled = killInfo.Split(" killed ");

                if (killerKilled.Length != 2)
                    continue;

                var killer = killerKilled[0].Trim();
                var killed = killerKilled[1].Trim();

                if (killed != "<world>")
                    playerSet.Add(killed);
                if (killer != "<world>")
                    playerSet.Add(killer);

                if (killer == "<world>")
                {
                    if (currentGame.Kills.ContainsKey(killed))
                        currentGame.Kills[killed]--;
                    else
                        currentGame.Kills[killed] = -1;
                }
                else
                {
                    if (currentGame.Kills.ContainsKey(killer))
                        currentGame.Kills[killer]++;
                    else
                        currentGame.Kills[killer] = 1;
                }
            }
        }

        // Garante que o último jogo tenha os players salvos
        if (currentGame != null)
            currentGame.Players = playerSet.ToList();

        return games;
    }
}

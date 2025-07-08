namespace QuakeLogParser.Domain.Entities;

public class Game
{
    public string Name { get; set; } = string.Empty;
    public int TotalKills { get; set; }
    public HashSet<string> Players { get; set; } = new();
    public Dictionary<string, int> Kills { get; set; } = new();
}

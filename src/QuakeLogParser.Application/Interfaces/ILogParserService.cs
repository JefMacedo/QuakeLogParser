using QuakeLogParser.Application.DTOs;
using System.IO;

namespace QuakeLogParser.Application.Interfaces;

public interface ILogParserService
{
    IEnumerable<GameReportDto> ParseLogFile(string path);
}

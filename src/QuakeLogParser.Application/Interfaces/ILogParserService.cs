using QuakeLogParser.Application.DTOs;

namespace QuakeLogParser.Application.Interfaces;

public interface ILogParserService
{
    IEnumerable<GameReportDto> ParseLogFile();
}

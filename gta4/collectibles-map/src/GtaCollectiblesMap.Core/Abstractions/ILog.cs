namespace GtaCollectiblesMap.Core.Abstractions;

public interface ILog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Debug(string message);
}

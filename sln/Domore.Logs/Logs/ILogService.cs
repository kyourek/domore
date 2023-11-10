namespace Domore.Logs {
    public interface ILogService {
        void Log(string name, string data, LogSeverity severity);
        void Complete();
    }
}

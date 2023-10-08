using System.IO;
using DIRECTORY = System.IO.Directory;

namespace Domore.Logs.Service {
    internal sealed class FileLog : ILogService {
        private readonly object Locker = new object();

        private FileInfo FileInfo =>
            _FileInfo ?? (
            _FileInfo = new FileInfo(Path.Combine(DirectoryInfo.FullName, Name)));
        private FileInfo _FileInfo;

        private DirectoryInfo DirectoryInfo =>
            _DirectoryInfo ?? (
            _DirectoryInfo = new DirectoryInfo(Directory));
        private DirectoryInfo _DirectoryInfo;

        public string Directory {
            get => _Directory;
            set {
                if (_Directory != value) {
                    lock (Locker) {
                        _Directory = value;
                        _DirectoryInfo = null;
                        _FileInfo = null;
                    }
                }
            }
        }
        private string _Directory;

        public string Name {
            get => _Name;
            set {
                if (_Name != value) {
                    lock (Locker) {
                        _Name = value;
                        _FileInfo = null;
                    }
                }
            }
        }
        private string _Name;
        
        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                if (DirectoryInfo.Exists == false) {
                    DIRECTORY.CreateDirectory(DirectoryInfo.FullName);
                    DirectoryInfo.Refresh();
                }
                if (FileInfo.Exists == false) {
                    FileInfo.Create();
                    FileInfo.Refresh();
                }
                File.AppendAllLines(FileInfo.FullName, new[] { data });
            }
        }
    }
}

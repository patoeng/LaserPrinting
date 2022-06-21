using System.IO;
using System.Threading.Tasks;

namespace LaserPrinting.Services
{
    public class DatalogFileWatcher
    {
        public delegate Task<bool> FileChangedMethod(string filename);

        public event FileChangedMethod FileChangedDetected;

        public DatalogFileWatcher(string fileLocation, string filePattern)
        {
            FileLocation = fileLocation;
            FilePattern = filePattern;
            InitFileWatcher(FileLocation, FilePattern);
        }
        public string FileLocation { get; protected set; } = @".\";
        public string FilePattern { get; protected set; } = "HL_????????.TXT";
        public bool Busy { get; protected set; }
        

        private FileSystemWatcher _fileSystemWatcher;

        public void InitFileWatcher(string fileLocation, string filePattern)
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Changed -= FileWatcherOnChanged;
            }
            _fileSystemWatcher = new FileSystemWatcher
            {
                Path = fileLocation,
                NotifyFilter =  NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = filePattern
            };
            _fileSystemWatcher.Changed += FileWatcherOnChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private async void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (Busy) return;
            Busy = true;
            if (FileChangedDetected != null)
            {
                var s =await Task.Run(()=> FileChangedDetected?.Invoke(e.FullPath)) ;
            }
            Busy = false;
        }
    }
}

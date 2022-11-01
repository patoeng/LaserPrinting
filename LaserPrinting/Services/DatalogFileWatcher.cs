using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        private Timer _timerDelayFileChanged;
        private string _fileName;
        private int _counter;
        private bool _delayActive;
        private bool _waitingBusy;

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
            _timerDelayFileChanged = new Timer();
            _timerDelayFileChanged.Interval = 100;
            _timerDelayFileChanged.Tick += TimerDelayFileChangedTicked;
            _timerDelayFileChanged.Start();
        
    }

        private async void TimerDelayFileChangedTicked(object sender, EventArgs e)
        {
            if (_delayActive)
            {
               
                _counter++;
                if (_counter > 6)
                {
                    _delayActive = false;
                    var task  = FileWatcherOnChangedDelayed();
                    await task;
                }
            }
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (Busy)
            {
                _waitingBusy = true;
                return;
            }
            _fileName = e.FullPath;
            _counter = 0;
            _delayActive = true;
        }

        private async Task FileWatcherOnChangedDelayed()
        {
            if (Busy) return;
            Busy = true;
            if (FileChangedDetected != null)
            {
                var s =await Task.Run(()=> FileChangedDetected?.Invoke(_fileName)) ;
                if (_waitingBusy)
                {
                   s = await Task.Run(() => FileChangedDetected?.Invoke(_fileName));
                   _waitingBusy = false;
                }
            }
            Busy = false;
        }
    }
}

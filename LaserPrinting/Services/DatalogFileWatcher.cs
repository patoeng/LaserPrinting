using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPrinting.Services
{
    public class DatalogFileWatcher
    {
        public delegate bool FileChangedMethod(string filename);

        public DatalogFileWatcher(string fileLocation, string filePattern, FileChangedMethod fileChangedMethod)
        {
            FileLocation = fileLocation;
            FilePattern = filePattern;
            _fileChangedMethod = fileChangedMethod;
            InitFileWatcher(FileLocation, FilePattern);
        }
        public string FileLocation { get; protected set; } = @".\";
        public string FilePattern { get; protected set; } = "HL_????????.TXT";
        public bool Busy { get; protected set; }
        

        private FileSystemWatcher _fileSystemWatcher;
        private FileChangedMethod _fileChangedMethod;

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
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = filePattern
            };
            _fileSystemWatcher.Changed += FileWatcherOnChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (Busy) return;
            Busy = true;
            if (_fileChangedMethod != null)
            {
                var executeOk = _fileChangedMethod(e.FullPath);
            }
            Busy = false;
        }
    }
}

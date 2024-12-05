using SDMeta;

namespace SDMetaUI.Services
{
    public class FileSystemObserver : IDisposable
    {
        public FileSystemObserver(IImageDir configuration)
        {
            watchers = configuration.GetPath().Select(p => GetWatcher(p)).ToList();
        }

        public event FileSystemEventHandler? FileSystemChanged;
        private readonly List<string> added = [];
        private readonly List<string> removed = [];
        private readonly List<string> removedInAdvance = [];
        private readonly IEnumerable<FileSystemWatcher> watchers;

        private FileSystemWatcher GetWatcher(string directory)
        {
            var watcher = new FileSystemWatcher(directory);

            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnCreated;

            // watcher.Filter = "*.png";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        public void RegisterRemoval(string path)
        {
            removedInAdvance.Add(path);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (IsValidForEvent(e) && removedInAdvance.Contains(e.FullPath) == false)
            {
                if (added.Contains(e.FullPath))
                {
                    added.Remove(e.FullPath);
                }
                else if (removed.Contains(e.FullPath) == false)
                {
                    removed.Add(e.FullPath);
                }

                FileSystemChanged?.Invoke(this, e);
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (IsValidForEvent(e))
            {
                if (removed.Contains(e.FullPath))
                {
                    removed.Remove(e.FullPath);
                }
                else if (added.Contains(e.FullPath) == false)
                {
                    added.Add(e.FullPath);
                }

                FileSystemChanged?.Invoke(this, e);
            }
        }

        private static bool IsValidForEvent(FileSystemEventArgs e)
        {
            return e.FullPath.ToLower().EndsWith(".png");
        }

        public void Dispose()
        {
            if (watchers != null)
            {
                foreach (var watcher in watchers)
                {
                    watcher.Dispose();
                }
            }
        }

        public int AddedCount => added.Count;
        public IEnumerable<string> DequeueAdded()
        {

            lock (added)
            {
                var copy = added.ToList();
                added.Clear();
                return copy;
            }
        }

        public int RemovedCount => removed.Count;
        public IEnumerable<string> DequeueRemoved()
        {
            lock (removed)
            {
                var copy = removed.ToList();
                removed.Clear();
                return copy;
            }
        }

        public void Reset()
        {
            this.added.Clear();
            this.removed.Clear();
            FileSystemChanged?.Invoke(this, null);
        }
    }
}

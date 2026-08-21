using SDMeta;

namespace SDMeta.Api.Services;

public sealed class PendingFileChangeQueue : IDisposable
{
    private readonly ILogger<PendingFileChangeQueue> _logger;
    private readonly List<string> _added = [];
    private readonly List<string> _removed = [];
    private readonly List<string> _removedInAdvance = [];
    private readonly List<FileSystemWatcher> _watchers;

    public PendingFileChangeQueue(IImageDir imageDir, ILogger<PendingFileChangeQueue> logger)
    {
        _logger = logger;
        _watchers = imageDir.GetPath()
            .Where(Directory.Exists)
            .Select(CreateWatcher)
            .ToList();
    }

    public void RegisterRemoval(string path)
    {
        lock (_removedInAdvance)
        {
            _removedInAdvance.Add(path);
        }
    }

    public (int AddedCount, int RemovedCount) GetCounts()
    {
        lock (_added)
        lock (_removed)
        {
            return (_added.Count, _removed.Count);
        }
    }

    public (IReadOnlyList<string> Added, IReadOnlyList<string> Removed) Dequeue()
    {
        lock (_added)
        lock (_removed)
        {
            var added = _added.ToList();
            var removed = _removed.ToList();
            _added.Clear();
            _removed.Clear();
            return (added, removed);
        }
    }

    public void Reset()
    {
        lock (_added)
        lock (_removed)
        {
            _added.Clear();
            _removed.Clear();
        }
    }

    private FileSystemWatcher CreateWatcher(string directory)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };

        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnCreated;
        return watcher;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (IsValidForEvent(e.FullPath) == false)
        {
            return;
        }

        lock (_added)
        lock (_removed)
        {
            if (_removed.Remove(e.FullPath) == false && _added.Contains(e.FullPath) == false)
            {
                _added.Add(e.FullPath);
            }
        }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (IsValidForEvent(e.FullPath) == false)
        {
            return;
        }

        lock (_removedInAdvance)
        {
            if (_removedInAdvance.Remove(e.FullPath))
            {
                return;
            }
        }

        lock (_added)
        lock (_removed)
        {
            if (_added.Remove(e.FullPath) == false && _removed.Contains(e.FullPath) == false)
            {
                _removed.Add(e.FullPath);
            }
        }
    }

    private static bool IsValidForEvent(string fullPath)
    {
        var lower = fullPath.ToLowerInvariant();
        return lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg");
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing file watcher");
            }
        }
    }
}

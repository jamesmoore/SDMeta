using SDMeta;

namespace SDMetaUI.Services
{
	public class FileSystemObserver : IDisposable
	{
		private readonly IImageDir configuration;

		public FileSystemObserver(IImageDir configuration)
		{
			this.configuration = configuration;
			this.Start();
		}

		public event FileSystemEventHandler FileSystemChanged;
		private readonly IList<string> added = new List<string>();
		private readonly IList<string> removed = new List<string>();
		private readonly IList<string> removedInAdvanced = new List<string>();
		private IEnumerable<FileSystemWatcher> watchers;

		private void Start()
		{
			if (watchers == null)
			{
				watchers = configuration.GetPath().Select(p => GetWatcher(p)).ToList();
			}
		}

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
			removedInAdvanced.Add(path);
		}

		private void OnDeleted(object sender, FileSystemEventArgs e)
		{
			if (IsValidForEvent(e) && removedInAdvanced.Contains(e.FullPath) == false)
			{
				if (added.Contains(e.FullPath))
				{
					added.Remove(e.FullPath);
				}
				else if (removed.Contains(e.FullPath) == false)
				{
					removed.Add(e.FullPath);
				}

				if (this.FileSystemChanged != null) { this.FileSystemChanged(this, e); }
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

				if (this.FileSystemChanged != null) { this.FileSystemChanged(this, e); }
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

		public int Added => added.Count;
		public int Removed => removed.Count;

		public void Reset()
		{
			this.added.Clear();
			this.removed.Clear();
			if (this.FileSystemChanged != null) { this.FileSystemChanged(this, null); }
		}
	}
}

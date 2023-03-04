namespace SDMetaUI.Services
{
	public class FileSystemObserver : IDisposable
	{
		private readonly IConfiguration configuration;

		public FileSystemObserver(IConfiguration configuration)
		{
			this.configuration = configuration;
		}

		public event FileSystemEventHandler FileSystemChanged;
		private readonly IList<string> added = new List<string>();
		private readonly IList<string> removed = new List<string>();
		private readonly IList<string> removedInAdvanced = new List<string>();
		private FileSystemWatcher watcher;

		public void Start()
		{
			if (watcher == null)
			{
				var directory = configuration["ImageDir"];

				watcher = new FileSystemWatcher(directory);

				watcher.Created += OnCreated;
				watcher.Deleted += OnDeleted;

				// watcher.Filter = "*.png";
				watcher.IncludeSubdirectories = true;
				watcher.EnableRaisingEvents = true;
			}
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
				else
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
				else
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
			watcher?.Dispose();
		}

		public int Added => added.Count;
		public int Removed => removed.Count;

		public void Reset()
		{
			this.added.Clear();
			this.removed.Clear();
		}
	}
}

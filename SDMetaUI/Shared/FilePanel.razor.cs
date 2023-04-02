using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SDMetaTool;
using SDMetaUI.Models;

namespace SDMetaUI.Shared
{
	public partial class FilePanel
	{
		[Parameter]
		public PngFileViewModel selectedFile { get; set; }

		[Parameter]
		public EventCallback<MouseEventArgs> onDelete { get; set; }

		[Parameter]
		public EventCallback<MouseEventArgs> onClose { get; set; }

		[Parameter]
		public EventCallback<MouseEventArgs> onFullScreenView { get; set; }

		private GenerationParams? selectedFileParams;

		private string fileSize;
		private string lastUpdated;

		private IEnumerable<string> promptLines;

		private bool HasPrompt => string.IsNullOrWhiteSpace(@selectedFileParams?.Prompt) == false;

		protected override Task OnParametersSetAsync()
		{
			var realFile = pngfileDataSource.ReadPngFile(selectedFile.FileName);
			fileSize = realFile.Length.GetBytesReadable();
			lastUpdated = realFile.LastUpdated.ToString();
			selectedFileParams = realFile?.Parameters;

			this.promptLines = selectedFileParams?.GetFullPrompt().Split("\n").Select(p => p.Trim()).Select(p => p.FormatPromptLine()).ToList();

			return base.OnParametersSetAsync();
		}

		private async Task CopyTextToClipboard()
		{
			await JSRuntime.InvokeVoidAsync("copyClipboard", "selectedFileParams-textarea");
			Messenger.AddInformation(title: "Prompt", message: "Copied to clipboard");
		}
	}
}

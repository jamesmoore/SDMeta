using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SDMetaUI.Models;
using SDMeta;

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

		private string fileSize;
		private string lastUpdated;
		private string promptFormat;

		private IEnumerable<string> promptLines;
		private string fullPrompt;

		private bool HasPrompt => this.promptLines != null && this.promptLines.Any();

		protected override Task OnParametersSetAsync()
		{
			var realFile = pngfileDataSource.ReadPngFile(selectedFile.FileName);
			if (realFile != null)
			{
				fileSize = realFile.Length.GetBytesReadable();
				lastUpdated = realFile.LastUpdated.ToString();
				this.fullPrompt = realFile?.Prompt;
				this.promptLines = fullPrompt?.Split("\n").Select(p => p.Trim()).Select(p => p.FormatPromptLine()).ToList();
				this.promptFormat = realFile.PromptFormat.ToString();
			}
			return base.OnParametersSetAsync();
		}

		private async Task CopyTextToClipboard()
		{
			await JSRuntime.InvokeVoidAsync("copyClipboard", "selectedFileParams-textarea");
			Messenger.AddInformation(title: "Prompt", message: "Copied to clipboard");
		}
	}
}

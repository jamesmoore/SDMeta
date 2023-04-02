using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using SDMetaUI.Models;

namespace SDMetaUI.Shared
{
	public partial class FullScreenView
	{
		[Parameter]
		public PngFileViewModel selectedFile { get; set; }
		[Parameter]
		public EventCallback<KeyboardEventArgs> onLeft { get; set; }
		[Parameter]
		public EventCallback<KeyboardEventArgs> onRight { get; set; }

		private ElementReference containerRef;
		private HxModal? hxModal;
		public bool IsOpen { get; private set; }

		public Task ShowAsync()
		{
			return hxModal.ShowAsync();
		}

		private async void KeyboardEventHandler(KeyboardEventArgs args)
		{
			if (args.Code == "ArrowLeft") await onLeft.InvokeAsync(args);
			else if (args.Code == "ArrowRight") await onRight.InvokeAsync(args);
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await containerRef.FocusAsync();
			}
		}
	}
}

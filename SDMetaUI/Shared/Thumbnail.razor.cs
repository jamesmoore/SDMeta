using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using SDMetaUI.Models;

namespace SDMetaUI.Shared
{
	public partial class Thumbnail
	{
		[Parameter]
		public PngFileViewModel File { get; set; }

		[Parameter]
		public bool Selected { get; set; }

		[Parameter]
		public bool Expanded { get; set; }

		[Parameter]
		public string Text { get; set; }

		private string GetClass => (Selected ? "selected" : "") + (Expanded ? " expanded" : "");

		[Parameter]
		public EventCallback<MouseEventArgs> onclick { get; set; }
	}
}

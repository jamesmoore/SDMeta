using SDMetaTool;
using SDMetaTool.Auto1111;
using System.Text.RegularExpressions;
using System.Web;

namespace SDMetaUI
{
	public static partial class ExtensionMethods
	{
		public static T GetNext<T>(this IList<T> items, T item)
		{
			var index = items.IndexOf(item);
			var next = index < items.Count - 1 ? items[index + 1] : item;
			return next;
		}

		public static T GetPrevious<T>(this IList<T> items, T item)
		{
			var index = items.IndexOf(item);
			var previous = index > 0 ? items[index - 1] : item;
			return previous;
		}

		public static string FormatPromptLine(this string s)
		{
			const string negativePrompt = "Negative prompt:";
			if (s.StartsWith(negativePrompt))
			{
				return s.Replace(negativePrompt, $"<span class=\"text-info fw-bold\">{negativePrompt}</span>");
			}
			else
			{
				var multipleParamsMatch = Auto1111ParameterDecoder.MultipleParameterRegex().Match(s);

				if (multipleParamsMatch.Success)
				{
					var reformatted = ParameterHeadingRegex().Replace(s, "<span class=\"text-info fw-bold\">$1</span> $2, ");
					if (reformatted.EndsWith(", "))
					{
						reformatted = reformatted[..^2];
					}
					return reformatted;
				}
				else
				{
					var encoded = HttpUtility.HtmlEncode(s);

					var loraRegex = LoraHypernetRegex();
					var loraMatches = loraRegex.Match(encoded);
					if (loraMatches.Success)
					{
						var loraReplaced =  loraRegex.Replace(encoded, "<span class=\"text-success fw-bold\">$1$2$4</span>");
						return loraReplaced;
					}
					else
					{
						return encoded;
					}
				}
			}
		}

		[GeneratedRegex("\\s*([\\w ]+:)\\s*(\"(?:\\\\|\\\"|[^\\\"])+\"|[^,]*)(?:,|$)")]
		private static partial Regex ParameterHeadingRegex();

		[GeneratedRegex("(&lt;lora:|&lt;hypernet:)(((?!&gt;).)*)(&gt;)")]
		private static partial Regex LoraHypernetRegex();
	}
}

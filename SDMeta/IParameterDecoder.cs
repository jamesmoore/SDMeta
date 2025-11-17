namespace SDMeta
{
	public interface IParameterDecoder
	{
		GenerationParams GetParameters(PngFile pngFile);
	}
}
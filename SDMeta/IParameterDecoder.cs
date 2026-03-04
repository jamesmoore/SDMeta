namespace SDMeta
{
	public interface IParameterDecoder
	{
		GenerationParams GetParameters(ImageFile pngFile);
	}
}
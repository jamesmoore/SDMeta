namespace SDMeta.Parameterless
{
    public class ParameterlessDecoder : IParameterDecoder
    {
        public GenerationParams GetParameters(PngFile pngFile)
        {
            return new GenerationParams();
        }
    }
}

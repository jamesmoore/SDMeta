namespace SDMeta.Parameterless
{
    public class ParameterlessDecoder : IParameterDecoder
    {
        public GenerationParams GetParameters(ImageFile pngFile)
        {
            return new GenerationParams();
        }
    }
}

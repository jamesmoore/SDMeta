namespace SDMeta.Parameterless
{
    public class ParameterlessDecoder : IParameterDecoder
    {
        public GenerationParams GetParameters(ImageFile imageFile) => GenerationParams.Empty;
    }
}

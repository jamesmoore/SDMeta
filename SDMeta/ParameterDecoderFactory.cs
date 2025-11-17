using SDMeta.Auto1111;
using SDMeta.Comfy;
using SDMeta.Parameterless;

namespace SDMeta
{
    public class ParameterDecoderFactory(
        Auto1111ParameterDecoder auto1111ParameterDecoder,
        ComfyUIParameterDecoder comfyUIParameterDecoder,
        ParameterlessDecoder parameterlessDecoder) : IParameterDecoder
    {
        public GenerationParams GetParameters(PngFile pngFile)
        {
            var decoder = GetParameterDecoder(pngFile.PromptFormat);
            return decoder.GetParameters(pngFile);
        }

        private IParameterDecoder GetParameterDecoder(PromptFormat promptFormat) => promptFormat switch
        {
            PromptFormat.Auto1111 => auto1111ParameterDecoder,
            PromptFormat.ComfyUI => comfyUIParameterDecoder,
            _ => parameterlessDecoder,
        };
    }
}

using SDMeta.Auto1111;
using SDMeta.Comfy;

namespace SDMeta
{
    public class ParameterDecoderFactory(
        Auto1111ParameterDecoder auto1111ParameterDecoder,
        ComfyUIParameterDecoder comfyUIParameterDecoder
        ) : IParameterDecoder
    {
        public IParameterDecoder GetParameterDecoder(PromptFormat promptFormat)
        {
            return promptFormat switch
            {
                PromptFormat.Auto1111 => auto1111ParameterDecoder,
                PromptFormat.ComfyUI => comfyUIParameterDecoder,
                _ => null
            };
        }

        public GenerationParams GetParameters(PngFile pngFile)
        {
            var decoder = GetParameterDecoder(pngFile.PromptFormat);
            return decoder.GetParameters(pngFile);
        }
    }
}

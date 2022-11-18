namespace SDMetaTool
{
    public interface IDirectoryProcessor
    {
        int ProcessList(string path, IPngFileListProcessor processor, bool whatif = false);
    }
}
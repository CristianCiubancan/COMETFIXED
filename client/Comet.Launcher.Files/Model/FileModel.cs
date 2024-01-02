using System.Runtime.InteropServices;

namespace Comet.Launcher.Files.Model
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct FileModel
    {
        public string Hash { get; init; }
        public string Name { get; init; }
    }
}

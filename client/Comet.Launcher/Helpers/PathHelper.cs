namespace Comet.Launcher.Helpers
{
    public static class PathHelper
    {

        public static string GetParentDirectory(this string path)
        {
            path = path.TrimEnd(Path.DirectorySeparatorChar);
            return Directory.GetParent(path)?.FullName;
        }

    }
}

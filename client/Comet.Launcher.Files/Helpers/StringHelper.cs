using System.Security.Cryptography;

namespace Comet.Launcher.Files.Helpers
{
    public static class StringHelper
    {
        public static string GetSha256(this string path)
        {
            try
            {
                using var sha = SHA256.Create();
                using FileStream stream = File.OpenRead(path);
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
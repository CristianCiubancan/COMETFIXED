namespace Comet.Launcher.Files.Helpers
{
    public static class RemoteFileHelper
    {
        private const double Kbyte = 1024;
        private const double Mbyte = Kbyte * 1024;
        private const double Gbyte = Mbyte * 1024;

        public static async Task<bool> ExistsAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                using var message = new HttpRequestMessage(HttpMethod.Head, url);
                HttpResponseMessage result = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                result.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> ReadStringFromUrlAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                using var message = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage result = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead);
                return await result.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<long> FileSizeAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                using var message = new HttpRequestMessage(HttpMethod.Head, url);
                HttpResponseMessage result = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                result.EnsureSuccessStatusCode();
                return result.Content.Headers.ContentLength ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public static string ParseFileSize(long size)
        {
            if (size > Gbyte)
                return $"{size / Gbyte:N2} GB";
            if (size > Mbyte)
                return $"{size / Mbyte:N2} MB";
            if (size > Kbyte)
                return $"{size / Kbyte:N2} KB";
            return $"{size} B";
        }

        public static string ParseDownloadSpeed(long amount)
        {
            if (amount > Gbyte)
                return $"{amount / Gbyte:N2} GB/s";
            if (amount > Mbyte)
                return $"{amount / Mbyte:N2} MB/s";
            if (amount > Kbyte)
                return $"{amount / Kbyte:N2} KB/s";
            return $"{amount} B/s";
        }
    }
}
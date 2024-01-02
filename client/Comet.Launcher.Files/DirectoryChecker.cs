using Comet.Launcher.Files.Helpers;
using Comet.Launcher.Files.Model;
using Comet.Shared;

namespace Comet.Launcher.Files
{
    public sealed class DirectoryChecker
    {
        private TimeOutMS mTimeOut;

        private readonly string[] mUpdateFolders =
        {
            "ani",
            "AutoPatch",
            "c3",
            "data",
            "data2",
            "Help",
            "ini",
            "map",
            "sound"
        };

        public delegate Task OnProgress(string currentFileName, int current, int total);

        private readonly Queue<string> mFilesToCheck = new();
        private readonly List<FileModel> mFiles = new();

        private DirectoryChecker(OnProgress? progress)
        {
            if (progress != null)
                OnProgressAsync = progress;

            mTimeOut = new TimeOutMS(1000);
        }

        /// <remarks>Must be ran as Administrator.</remarks>
        public static Task<DirectoryChecker> CreateAsync(string main, OnProgress? progress = null)
        {
            DirectoryChecker checker = new DirectoryChecker(progress);

            foreach (string dir in Directory.EnumerateFiles(main, "*.*", SearchOption.AllDirectories)
                                            .OrderBy(x => x))
            {
                checker.mFilesToCheck.Enqueue(dir);
            }

            return Task.FromResult(checker);
        }

        public OnProgress? OnProgressAsync;

        public int FilesCount => mFilesToCheck.Count;

        public async Task QueryLocalDataAsync()
        {
            int total = FilesCount;
            int current = Math.Max(0, Math.Min(total, 1));
            string file;
            while ((file = mFilesToCheck.Dequeue()) != null)
            {
                string md5Hash = file.GetSha256();

                FileModel model = new FileModel
                {
                    Hash = md5Hash,
                    Name = file
                };

                mFiles.Add(model);

                if (mTimeOut.ToNextTime() && OnProgressAsync != null)
                    await OnProgressAsync(file, current++, total);

            }
        }

        public async Task<bool> ProcessAsync()
        {


            return true;
        }

    }
}

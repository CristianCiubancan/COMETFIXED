using Comet.Launcher.Files.Helpers;
using Comet.Launcher.Managers;
using Comet.Launcher.States;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Packets
{
    internal sealed class MsgUpdPatchList : MsgUpdPatchList<Server>
    {
        private static int Retries = 0;

        private readonly string[] mSendFileList =
        {
            "Conquer.exe",
            Path.Combine("AutoPatch", "Comet.Launcher.exe")
        };

        /// <inheritdoc />
        public override async Task ProcessAsync(Server client)
        {
            if (Mode == MsgUpdPatchType.NoUpdate)
            {
                var msg = new MsgUpdCheckHash
                {
                    Eof = true
                };
                foreach (string file in mSendFileList)
                {
                    if (!File.Exists(Path.Combine(FrmMain.WorkingDirectory, file)))
                    {
                        client.Disconnect();
                        return;
                    }

                    string hash = Path.Combine(FrmMain.WorkingDirectory, file).GetSha256();
                    msg.Hashes.Add(new MsgUpdCheckHash<Server>.FileHash
                    {
                        FilePath = file,
                        Hash = hash
                    });
                }

                if (msg.Hashes.Count < 1)
                {
                    client.Disconnect();
                    return;
                }

                await client.SendAsync(msg);
                return;
            }

            if (Patches.Count < 1)
            {
                if (Retries++ < 5)
                {
                    FrmMain.Instance.SetProgressLabel(LocaleManager.GetString("StrFileFetchError"));
                    await client.SendAsync(new MsgUpdQueryVersion
                    {
                        ClientVersion = FrmMain.CURRENT_VERSION,
                        GameVersion = FrmMain.CurrentGameVersion
                    });
                }
                else
                {
                    FrmMain.Instance.SetProgressLabel(LocaleManager.GetString("StrFileFetchFatalError"));
                    await Task.Delay(5000);
                    client.Disconnect();
                }
                return;
            }

            Retries = 0;
            await FrmMain.Instance.PrepareDownloadingAsync(Mode, Patches, Domain);
        }
    }
}
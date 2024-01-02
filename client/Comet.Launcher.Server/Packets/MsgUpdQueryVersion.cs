using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Launcher.Server.States;
using Comet.Launcher.Server.States.Patches;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Server.Packets
{
    internal sealed class MsgUpdQueryVersion : MsgUpdQueryVersion<Client>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Client client)
        {
            int version = UpdateManager.LatestClientUpdate();
            if (version > 0 && ClientVersion != version)
            {
                List<UpdateStruct> patchList = UpdateManager.GetUpdateSequence(ClientVersion);
                var msg = new MsgUpdPatchList
                {
                    Mode = MsgUpdPatchType.Client,
                    Domain = UpdateManager.DownloadFrom
                };
                msg.Patches.AddRange(patchList.Select(x => new UpdatePatch
                {
                    FileName = x.FullFileName,
                    Version = x.To,
                    Hash = x.Hash
                }));
                await client.SendAsync(msg);
                return;
            }

            version = UpdateManager.LatestGameClientUpdate();
            if (version > 0 && GameVersion < version)
            {
                List<UpdateStruct> patchList = UpdateManager.GetUpdateSequence(GameVersion);
                var msg = new MsgUpdPatchList 
                { 
                    Mode = MsgUpdPatchType.Game,
                    Domain = UpdateManager.DownloadFrom
                };
                msg.Patches.AddRange(patchList.Select(x => new UpdatePatch
                {
                    FileName = x.FullFileName,
                    Version = x.To,
                    Hash = x.Hash
                }));
                await client.SendAsync(msg);
                return;
            }

            await client.SendAsync(new MsgUpdPatchList
            {
                Mode = MsgUpdPatchType.NoUpdate,
                Domain = UpdateManager.DownloadFrom
            });
        }
    }
}
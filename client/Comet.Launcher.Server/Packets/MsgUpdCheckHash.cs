using System;
using System.Threading.Tasks;
using Comet.Launcher.Server.States;
using Comet.Launcher.Server.States.Patches;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Server.Packets
{
    internal sealed class MsgUpdCheckHash : MsgUpdCheckHash<Client>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Client client)
        {
            var success = false;
            foreach (FileHash file in Hashes)
            {
                if (file.FilePath.ToUpperInvariant().EndsWith("CONQUER.EXE") 
                    && UpdateManager.ConquerHash.Equals(file.Hash, StringComparison.InvariantCultureIgnoreCase))
                {
                    client.ConquerHashOk = true;
                    success = true;
                }
            }

            if (!Eof)
                return;

            int errorResult;
            do
            {
                errorResult = await Kernel.NextAsync(1, int.MaxValue);
            }
            while (errorResult == MsgUpdConfirmHash.SUCCESS);

            if (!success)
            {
                await client.SendAsync(new MsgUpdConfirmHash
                {
                    Result = errorResult
                });
                client.Disconnect();
                return;
            }

            await client.SendAsync(new MsgUpdConfirmHash
            {
                Result = client.ConquerHashOk ? MsgUpdConfirmHash.SUCCESS : errorResult
            });
        }
    }
}
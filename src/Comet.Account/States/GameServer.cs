using System;
using System.Linq;
using System.Net.Sockets;
using Comet.Database.Entities;
using Comet.Network;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Account.States
{
    public sealed class GameServer : TcpServerActor
    {
        //public DiffieHellman DiffieHellman { get; set; }

        public GameServer(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, AesCipher.Create(), partition, NetworkDefinition.ACCOUNT_FOOTER)
        {
        }

        public DbRealm Realm { get; private set; }

        public void SetRealm(string name)
        {
            Realm = Kernel.Realms.Values.FirstOrDefault(
                x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
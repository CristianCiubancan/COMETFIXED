using System;
using System.Net.Sockets;
using Comet.Database.Entities;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Account.States
{
    /// <summary>
    ///     Client encapsules the accepted client socket's actor and account server state.
    ///     The class should be initialized by the server's Accepted method and returned
    ///     to be passed along to the Receive loop and kept alive.
    /// </summary>
    public sealed class Client : TcpServerActor
    {
        // Fields and Properties
        public DbAccount Account;
        public uint Seed;

        /// <summary>
        ///     Instantiates a new instance of <see cref="Client" /> using the Accepted event's
        ///     resulting socket and pre-allocated buffer. Initializes all account server
        ///     states, such as the cipher used to decrypt and encrypt data.
        /// </summary>
        /// <param name="socket">Accepted remote client socket</param>
        /// <param name="buffer">pre-allocated buffer from the server listener</param>
        /// <param name="partition">Packet processing partition</param>
        public Client(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, new TQCipher(), partition)
        {
            GUID = Guid.NewGuid().ToString();
        }

        public string GUID { get; }
        public DbRealm Realm { get; set; }
    }
}
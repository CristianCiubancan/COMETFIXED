using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Game.States
{
    /// <summary>
    ///     Client encapsules the accepted client socket's actor and game server state.
    ///     The class should be initialized by the server's Accepted method and returned
    ///     to be passed along to the Receive loop and kept alive. Contains all world
    ///     interactions with the player.
    /// </summary>
    public sealed class Client : TcpServerActor
    {
        // Fields and Properties 
        public Character Character = null;
        public Creation Creation = null;
        public NDDiffieHellman NdDiffieHellman;

        /// <summary>
        ///     Instantiates a new instance of <see cref="Client" /> using the Accepted event's
        ///     resulting socket and pre-allocated buffer. Initializes all account server
        ///     states, such as the cipher used to decrypt and encrypt data.
        /// </summary>
        /// <param name="socket">Accepted remote client socket</param>
        /// <param name="buffer">pre-allocated buffer from the server listener</param>
        /// <param name="partition">Packet processing partition</param>
        public Client(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, new BlowfishCipher(BlowfishCipher.Default), partition, "TQServer")
        {
            NdDiffieHellman = new NDDiffieHellman();

            GUID = Guid.NewGuid().ToString();
        }

        // Client unique identifier
        public uint Identity => Character?.Identity ?? 0;
        public uint AccountIdentity { get; set; }
        public ushort AuthorityLevel { get; set; }
        public string MacAddress { get; set; } = "Unknown";
        public int LastLogin { get; set; }
        public string GUID { get; }

        public override Task<int> SendAsync(byte[] packet)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            return base.SendAsync(packet);
        }
    }
}
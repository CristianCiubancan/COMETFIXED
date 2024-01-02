using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Comet.Network.Packets;
using Comet.Network.Security;
using Comet.Shared;

namespace Comet.Network.Sockets
{
    /// <summary>
    ///     Actors are assigned to accepted client sockets to give connected clients a state
    ///     across socket operations. This allows the server to handle multiple receive writes
    ///     across single processing reads, and keep a buffer alive for faster operations.
    /// </summary>
    public abstract class TcpServerActor
    {
        // Fields and Properties
        public Memory<byte> Buffer { get; }
        public ICipher Cipher { get; }
        public byte[] PacketFooter { get; }
        public uint Partition { get; }
        private object SendLock { get; }
        public Socket Socket { get; }

        public int ReceiveTimeOutSeconds = 30;

        /// <summary>
        ///     Instantiates a new instance of <see cref="TcpServerActor" /> using an accepted
        ///     client socket and pre-allocated buffer from the server listener.
        /// </summary>
        /// <param name="socket">Accepted client socket</param>
        /// <param name="buffer">Pre-allocated buffer for socket receive operations</param>
        /// <param name="cipher">Cipher for handling client encipher operations</param>
        /// <param name="partition">Packet processing partition, default is disabled</param>
        /// <param name="packetFooter">Length of the packet footer</param>
        protected TcpServerActor(
            Socket socket,
            Memory<byte> buffer,
            ICipher cipher,
            uint partition = 0,
            string packetFooter = "")
        {
            Buffer = buffer;
            Cipher = cipher;
            Socket = socket;
            PacketFooter = Encoding.ASCII.GetBytes(packetFooter);
            Partition = partition;
            SendLock = new object();

            IpAddress = (Socket.RemoteEndPoint as IPEndPoint)?.Address.MapToIPv4().ToString();
        }

        /// <summary>
        ///     Returns the remote IP address of the connected client.
        /// </summary>
        public string IpAddress { get; }

        /// <summary>
        ///     Sends a packet to the game client after encrypting bytes. This may be called
        ///     as-is, or overridden to provide channel functionality and thread-safety around
        ///     the accepted client socket. By default, this method locks around encryption
        ///     and sending data.
        /// </summary>
        /// <param name="packet">Bytes to be encrypted and sent to the client</param>
        public virtual Task<int> SendAsync(byte[] packet)
        {
            var data = new byte[packet.Length + PacketFooter.Length];
            packet.CopyTo(data, 0);

            BitConverter.TryWriteBytes(data, (ushort) packet.Length);
            Array.Copy(PacketFooter, 0, data, packet.Length, PacketFooter.Length);

            lock (SendLock)
            {
                try
                {
                    if (Socket?.Connected != true)
                        return Task.FromResult(-1);
                    
                    Cipher?.Encrypt(data, data);
                    int result = Socket.Send(data, SocketFlags.None);
                    return Task.FromResult(result);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode is < SocketError.ConnectionAborted or > SocketError.Shutdown)
                        Console.WriteLine(e);
                    return Task.FromResult(-1);
                }
                catch (Exception ex)
                {
                    _ = Log.WriteLogAsync("TcpServerActor-SendAsync", LogLevel.Exception, ex.ToString())
                           .ConfigureAwait(false);
                    return Task.FromResult(-1);
                }
            }
        }

        /// <summary>
        ///     Sends a packet to the game client after encrypting bytes. This may be called
        ///     as-is, or overridden to provide channel functionality and thread-safety around
        ///     the accepted client socket. By default, this method locks around encryption
        ///     and sending data.
        /// </summary>
        /// <param name="packet">Packet to be encrypted and sent to the client</param>
        public virtual Task<int> SendAsync(IPacket packet)
        {
            return SendAsync(packet.Encode());
        }

        /// <summary>
        ///     Force closes the client connection.
        /// </summary>
        public virtual void Disconnect()
        {
            Socket?.Disconnect(false);
        }
    }
}
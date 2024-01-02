using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Comet.Network;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Launcher.Server.States
{
    public class Client : TcpServerActor
    {
        public Client(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, AesCipher.Create(), partition, NetworkDefinition.PATCHER_FOOTER)
        {
            GUID = Guid.NewGuid().ToString();
            DiffieHellman = DiffieHellman.Create();
        }

        // ReSharper disable once InconsistentNaming
        public string GUID { get; }

        public string CurrentFileMd5 { get; set; }
        public string MacAddress { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public string MachineDomain { get; set; }
        public string WindowsVersion { get; set; }
        public List<string> IpAddresses { get; set; } = new();
        public DiffieHellman DiffieHellman { get; }
        public bool ConquerHashOk { get; set; }
    }
}
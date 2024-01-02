using System;
using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiLoginExchangeEx : MsgAiLoginExchangeEx<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            switch (Result)
            {
                case AiLoginResult.Success:
                {
                    Kernel.GameServer = client;
                    await Log.WriteLogAsync("Accepted on the game server!");

                    await Log.WriteLogAsync("Sending data to game server...");
                    await GeneratorManager.SynchroGeneratorsAsync();
                    await Log.WriteLogAsync("Finished data sync");

                    break;
                }

                case AiLoginResult.AlreadySignedIn:
                {
                    if (client.Socket.Connected)
                        client.Disconnect();

                    await Log.WriteLogAsync(LogLevel.Error, "Could not connect to the game server! Already signed in.");
                    break;
                }

                case AiLoginResult.InvalidPassword:
                {
                    if (client.Socket.Connected)
                        client.Disconnect();
                    await Log.WriteLogAsync(LogLevel.Error,
                                            "Could not connect to the game server! Invalid username or password.");
                    break;
                }

                case AiLoginResult.InvalidAddress:
                {
                    if (client.Socket.Connected)
                        client.Disconnect();
                    await Log.WriteLogAsync(LogLevel.Error,
                                            "Could not connect to the game server! Address not authorized.");
                    Environment.Exit(0);
                    break;
                }

                case AiLoginResult.AlreadyBound:
                {
                    if (client.Socket.Connected)
                        client.Disconnect();
                    await Log.WriteLogAsync(LogLevel.Error,
                                            "Could not connect to the game server! Socket already bound.");
                    break;
                }
            }
        }
    }
}
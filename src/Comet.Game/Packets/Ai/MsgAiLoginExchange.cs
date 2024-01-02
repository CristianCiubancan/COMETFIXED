using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgAiLoginExchange : MsgAiLoginExchange<AiClient>
    {
        public override async Task ProcessAsync(AiClient client)
        {
            if (client.Stage != AiClient.ConnectionStage.AwaitingAuth)
            {
                await Log.WriteLogAsync(
                    $"MsgAiLoginExchange.ProcessAsync >> Client {client.GUID} is already logged in");
                await client.SendAsync(new MsgAiLoginExchangeEx
                {
                    Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.AlreadySignedIn
                });
                return;
            }


            if (!UserName.Equals(Kernel.Configuration.AiNetwork.Username) ||
                !Password.Equals(Kernel.Configuration.AiNetwork.Password))
            {
                await Log.WriteLogAsync(
                    $"MsgAiLoginExchange.ProcessAsync >> Invalid username or password for {client.GUID} [{client.IpAddress}]");
                await client.SendAsync(new MsgAiLoginExchangeEx
                {
                    Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.InvalidPassword
                });
                client.Disconnect();
                return;
            }

            if (Kernel.AiServer != null)
            {
                await Log.WriteLogAsync(
                    $"MsgAiLoginExchange.ProcessAsync >> Server has bound an NPC Server already {client.GUID} [{client.IpAddress}]");
                await client.SendAsync(new MsgAiLoginExchangeEx
                {
                    Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.AlreadyBound
                });
                client.Disconnect();
                return;
            }

            client.Stage = AiClient.ConnectionStage.Authenticated;
            await client.SendAsync(new MsgAiLoginExchangeEx
            {
                Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.Success
            });

            Kernel.AiServer = client;
        }
    }
}
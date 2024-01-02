using System;
using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgAiPing : MsgAiPing<AiClient>
    {
        public override Task ProcessAsync(AiClient client)
        {
            // Keep alive
            if (RecvTimestamp == 0 && RecvTimestampMs == 0)
            {
                RecvTimestamp = Environment.TickCount;
                RecvTimestampMs = Environment.TickCount64;
                return client.SendAsync(this);
            }

            int ping = (Environment.TickCount - Timestamp) / 2;
            long pingMs = (Environment.TickCount64 - TimestampMs) / 2;

            if (ping > 2000 || pingMs > 2000)
                return Log.WriteLogAsync($"Inter server network lag detected! Ping: {ping}s ({pingMs}ms)");
            return Task.CompletedTask;
        }
    }
}
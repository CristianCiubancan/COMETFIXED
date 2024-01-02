using System;
using System.Threading.Tasks;
using Comet.AI.States;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiPing : MsgAiPing<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            // Keep alive
            if (RecvTimestamp != 0 && RecvTimestampMs != 0)
            {
                int ping = (Environment.TickCount - RecvTimestamp) / 2;
                long pingMs = (Environment.TickCount64 - RecvTimestampMs) / 2;

                Timestamp = RecvTimestamp;
                TimestampMs = RecvTimestampMs;
                RecvTimestamp = Environment.TickCount;
                RecvTimestampMs = Environment.TickCount64;
                await client.SendAsync(this);

                if (ping > 1000 || pingMs > 1000)
                    await Log.WriteLogAsync($"Inter server network lag detected! Ping: {ping}s ({pingMs}ms)");
            }
        }
    }
}
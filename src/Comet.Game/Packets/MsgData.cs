﻿using System;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgData : MsgData<Client>
    {
        public MsgData()
            : base(DateTime.Now)
        {
        }
    }
}
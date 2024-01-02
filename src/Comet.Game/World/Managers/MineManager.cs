using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public static class MineManager
    {
        private static readonly List<MineCtrl> mMineControl = new();

        public static async Task<bool> InitializeAsync()
        {
            foreach (DbMineCtrl ctrl in await MineCtrlRepository.GetAsync())
            {
                DbItemtype it = ItemManager.GetItemtype(ctrl.ItemId);
                if (it == null)
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Could not find {ctrl.ItemId} for mining {ctrl.Id}");
                    continue;
                }

                mMineControl.Add(new MineCtrl(ctrl));
            }

            return true;
        }

        public static async Task<uint> MineAsync(uint mapId, Character target)
        {
            IOrderedEnumerable<MineCtrl> mapPool = mMineControl.Where(x => x.MapId == mapId).OrderBy(x => x.Percent);
            foreach (MineCtrl ctrl in mapPool)
                if (ctrl.IsPickUpAllowed && await ctrl.TryPickUpAsync())
                {
                    if (target.UserPackage.MultiCheckItem(ctrl.ItemId, ctrl.ItemId,
                                                          (int) ctrl.Limit)) // user has reached limit
                        continue;

                    ctrl.Refresh();
                    return ctrl.ItemId;
                }

            return 0;
        }
    }

    public class MineCtrl
    {
        private readonly DbMineCtrl mMineCtrl;
        private readonly TimeOut mTimeout;

        public MineCtrl(DbMineCtrl ctrl)
        {
            mMineCtrl = ctrl;
            mTimeout = new TimeOut();
            mTimeout.Startup(ctrl.Interval);
        }

        public uint ItemId => mMineCtrl.ItemId;
        public uint MapId => mMineCtrl.MapId;
        public uint Percent => mMineCtrl.Percent;
        public uint Limit => mMineCtrl.AmountLimit;

        public async Task<bool> TryPickUpAsync()
        {
            return await Kernel.ChanceCalcAsync((int) mMineCtrl.Percent, 40000000);
        }

        public bool IsPickUpAllowed => mTimeout.IsTimeOut();

        public void Refresh()
        {
            mTimeout.Update();
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Comet.Shared;
using Microsoft.Extensions.Hosting;

namespace Comet.Game.World
{
    public class ServerProcessor : BackgroundService
    {
        public const int NO_MAP_GROUP = 0;
        public const int PVP_MAP_GROUP = 1;
        public const int NORMAL_MAP_GROUP = 2;

        protected readonly Task[] mBackgroundTasks;
        protected readonly Channel<Func<Task>>[] mChannels;
        protected readonly Partition[] mPartitions;
        protected CancellationToken mCancelReads;
        protected CancellationToken mCancelWrites;

        public readonly int Count;

        public ServerProcessor()
        {
            Count = Math.Max(1, Math.Min(Environment.ProcessorCount, 2)) + NORMAL_MAP_GROUP;

            mBackgroundTasks = new Task[Count];
            mChannels = new Channel<Func<Task>>[Count];
            mPartitions = new Partition[Count];
            mCancelReads = new CancellationToken();
            mCancelWrites = new CancellationToken();
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            for (var i = 0; i < Count; i++)
            {
                mPartitions[i] = new Partition {ID = (uint) i, Weight = 0};
                mChannels[i] = Channel.CreateUnbounded<Func<Task>>();
                mBackgroundTasks[i] = DequeueAsync(i, mChannels[i]);
            }

            return Task.WhenAll(mBackgroundTasks);
        }

        public void Queue(int partition, Func<Task> task)
        {
            if (!mCancelWrites.IsCancellationRequested) mChannels[partition].Writer.TryWrite(task);
        }

        protected virtual async Task DequeueAsync(int partition, Channel<Func<Task>> channel)
        {
            while (!mCancelReads.IsCancellationRequested)
            {
                Func<Task> action = await channel.Reader.ReadAsync(mCancelReads);
                if (action != null)
                    try
                    {
                        await action
                            .Invoke(); //.ConfigureAwait(true); // THE QUEUE MUST BE EXECUTED IN ORDER, NO CONCURRENCY
                    }
                    catch (Exception ex)
                    {
                        await Log.WriteLogAsync(LogLevel.Exception, $"{ex.Message}\r\n\t{ex}");
                    }
            }
        }

        /// <summary>
        ///     Triggered when the application host is stopping the background task with a
        ///     graceful shutdown. Requests that writes into the channel stop, and then reads
        ///     from the channel stop.
        /// </summary>
        public new async Task StopAsync(CancellationToken cancellationToken)
        {
            mCancelWrites = new CancellationToken(true);
            foreach (Channel<Func<Task>> channel in mChannels)
                if (channel.Reader.Count > 0)
                    await channel.Reader.Completion;
            mCancelReads = new CancellationToken(true);
        }

        /// <summary>
        ///     Selects a partition for the client actor based on partition weight. The
        ///     partition with the least popluation will be chosen first. After selecting a
        ///     partition, that partition's weight will be increased by one.
        /// </summary>
        public uint SelectPartition()
        {
            uint partition = mPartitions.Where(x => x.ID >= NORMAL_MAP_GROUP).Aggregate((aggr, next) =>
                                                                            next.Weight.CompareTo(aggr.Weight) < 0
                                                                                ? next
                                                                                : aggr).ID;
            Interlocked.Increment(ref mPartitions[partition].Weight);
            return partition;
        }

        /// <summary>
        ///     Deslects a partition after the client actor disconnects.
        /// </summary>
        /// <param name="partition">The partition id to reduce the weight of</param>
        public void DeselectPartition(uint partition)
        {
            Interlocked.Decrement(ref mPartitions[partition].Weight);
        }

        public override string ToString()
        {
            return $"ProcessorQueue[{string.Join(',', mChannels.Select(x => $"{x.Reader.Count:00000}").ToArray())}]";
        }

        protected class Partition
        {
            public uint ID;
            public int Weight;
        }
    }
}
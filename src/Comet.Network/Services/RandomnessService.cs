using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Comet.Network.Services
{
    /// <summary>
    ///     This background service instantiates a single Random instance to generate all random
    ///     numbers for the server. This allows the server to generate random numbers across
    ///     multiple threads without generating the same number or returning zero. This service
    ///     in particular buffers random numbers to a channel to avoid locking.
    /// </summary>
    public class RandomnessService : BackgroundService
    {
        // Fields and Properties
        private readonly Channel<double> BufferChannel;

        /// <summary>
        ///     Instantiates a new instance of <see cref="RandomnessService" /> using a default
        ///     capacity to buffer random numbers.
        /// </summary>
        /// <param name="capacity">Capacity of the bounded channel.</param>
        public RandomnessService(int capacity = 10000)
        {
            BufferChannel = Channel.CreateBounded<double>(capacity);
        }

        /// <summary>
        ///     Triggered when the application host is ready to start queuing random numbers.
        ///     Since the channel holding random numbers is bounded, writes will block
        ///     naturally on an await rather than locking threads to generate numbers.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
                await BufferChannel.Writer.WriteAsync(
                    RandomNumberGenerator.GetInt32(int.MaxValue) / (double) int.MaxValue,
                    stoppingToken);
        }

        /// <summary>Returns the next random number from the generator.</summary>
        /// <param name="minValue">The least legal value for the Random number.</param>
        /// <param name="maxValue">One greater than the greatest legal return value.</param>
        public async Task<int> NextAsync(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException();

            long range = (long) maxValue - minValue;
            if (range > int.MaxValue)
                throw new ArgumentOutOfRangeException();

            double value = await BufferChannel.Reader.ReadAsync();
            int result = (int) (value * range) + minValue;
            return result;
        }

        public async Task<double> NextRateAsync(double range)
        {
            int random = await NextAsync(0, 999) + 1;
            double a = Math.Sin(random * Math.PI / 1000);
            double b;
            if (random >= 90)
                b = 1.0 + range - Math.Sqrt(Math.Sqrt(a)) * range;
            else
                b = 1.0 - range + Math.Sqrt(Math.Sqrt(a)) * range;
            return b;
        }

        /// <summary>Writes random numbers from the generator to a buffer.</summary>
        /// <param name="buffer">Buffer to write bytes to.</param>
        public async Task NextBytesAsync(byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = (byte) await NextAsync(0, 255);
        }
    }
}
using System.Threading;

namespace Comet.Shared
{
    /// <summary>
    ///     A monitor for the networking I/O. From COPS V6 Enhanced Edition.
    /// </summary>
    public sealed class NetworkMonitor
    {
        private long mTotalRecvBytes;
        private int mTotalRecvPackets;

        private long mTotalSentBytes;
        private int mTotalSentPackets;

        /// <summary>
        ///     The number of bytes received by the server.
        /// </summary>
        private int mRecvBytes;

        private int mRecvPackets;

        /// <summary>
        ///     The number of bytes sent by the server.
        /// </summary>
        private int mSentBytes;

        private int mSentPackets;

        public int PacketsSent => mSentPackets;
        public int PacketsRecv => mRecvPackets;
        public int BytesSent => mSentBytes;
        public int BytesRecv => mRecvBytes;
        public long TotalPacketsSent => mTotalSentPackets;
        public long TotalPacketsRecv => mTotalRecvPackets;
        public long TotalBytesSent => mTotalSentBytes;
        public long TotalBytesRecv => mTotalRecvBytes;

        /// <summary>
        ///     Called by the timer.
        /// </summary>
        public string UpdateStatsAsync(int interval)
        {
            double download = mRecvBytes / (double) interval * 8.0 / 1024.0;
            double upload = mSentBytes / (double) interval * 8.0 / 1024.0;
            int sent = mSentPackets;
            int recv = mRecvPackets;

            mRecvBytes = 0;
            mSentBytes = 0;
            mRecvPackets = 0;
            mSentPackets = 0;

            return $"(↑{upload:F2} kbps [{download:0000}], ↓{sent:F2} kbps [{recv:0000}])";
        }

        /// <summary>
        ///     Signal to the monitor that aLength bytes were sent.
        /// </summary>
        /// <param name="aLength">The number of bytes sent.</param>
        public void Send(int aLength)
        {
            Interlocked.Increment(ref mSentPackets);
            Interlocked.Increment(ref mTotalSentPackets);
            Interlocked.Add(ref mSentBytes, aLength);
            Interlocked.Add(ref mTotalSentBytes, aLength);
        }

        /// <summary>
        ///     Signal to the monitor that aLength bytes were received.
        /// </summary>
        /// <param name="aLength">The number of bytes received.</param>
        public void Receive(int aLength)
        {
            Interlocked.Increment(ref mRecvPackets);
            Interlocked.Increment(ref mTotalRecvPackets);
            Interlocked.Add(ref mRecvBytes, aLength);
            Interlocked.Add(ref mTotalRecvBytes, aLength);
        }
    }
}
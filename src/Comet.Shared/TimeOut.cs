using System;

namespace Comet.Shared
{
    public sealed class TimeOut
    {
        private int mInterval;
        private long mUpdateTime;

        public TimeOut(int nInterval = 0)
        {
            mInterval = nInterval;
            mUpdateTime = 0;
        }

        public long Clock()
        {
            return Environment.TickCount / 1000;
        }

        public bool Update()
        {
            mUpdateTime = Clock();
            return true;
        }

        public bool IsTimeOut()
        {
            return Clock() >= mUpdateTime + mInterval;
        }

        public bool ToNextTime()
        {
            if (IsTimeOut()) return Update();
            return false;
        }

        public void SetInterval(int nSecs)
        {
            mInterval = nSecs;
        }

        public void Startup(int nSecs)
        {
            mInterval = nSecs;
            Update();
        }

        public bool TimeOver()
        {
            if (IsActive() && IsTimeOut()) return Clear();
            return false;
        }

        public bool IsActive()
        {
            return mUpdateTime != 0;
        }

        public bool Clear()
        {
            mUpdateTime = mInterval = 0;
            return true;
        }

        public void IncInterval(int nSecs, int nLimit)
        {
            mInterval = Calculations.CutOverflow(mInterval + nSecs, nLimit);
        }

        public void DecInterval(int nSecs)
        {
            mInterval = Calculations.CutTrail(mInterval - nSecs, 0);
        }

        public bool IsTimeOut(int nSecs)
        {
            return Clock() >= mUpdateTime + nSecs;
        }

        public bool ToNextTime(int nSecs)
        {
            if (IsTimeOut(nSecs)) return Update();
            return false;
        }

        public bool TimeOver(int nSecs)
        {
            if (IsActive() && IsTimeOut(nSecs)) return Clear();
            return false;
        }

        public bool ToNextTick(int nSecs)
        {
            if (IsTimeOut(nSecs))
            {
                if (Clock() >= mUpdateTime + nSecs * 2)
                    return Update();
                mUpdateTime += nSecs;
                return true;
            }

            return false;
        }

        public int GetRemain()
        {
            return mUpdateTime != 0
                       ? Calculations.CutRange(mInterval - ((int) Clock() - (int) mUpdateTime), 0, mInterval)
                       : 0;
        }

        public int GetInterval()
        {
            return mInterval;
        }

        public static implicit operator bool(TimeOut ms)
        {
            return ms.ToNextTime();
        }
    }

    public sealed class TimeOutMS
    {
        private int mInterval;
        private long mUpdateTime;

        public TimeOutMS(int nInterval = 0)
        {
            if (nInterval < 0)
                nInterval = int.MaxValue;
            mInterval = nInterval;
            mUpdateTime = 0;
        }

        public long Clock()
        {
            return Environment.TickCount;
        }

        public bool Update()
        {
            mUpdateTime = Clock();
            return true;
        }

        public bool IsTimeOut()
        {
            return Clock() >= mUpdateTime + mInterval;
        }

        public bool ToNextTime()
        {
            if (IsTimeOut())
                return Update();
            return false;
        }

        public void SetInterval(int nMilliSecs)
        {
            mInterval = nMilliSecs;
        }

        public void Startup(int nMilliSecs)
        {
            mInterval = Math.Min(nMilliSecs, int.MaxValue);
            Update();
        }

        public bool TimeOver()
        {
            if (IsActive() && IsTimeOut()) return Clear();
            return false;
        }

        public bool IsActive()
        {
            return mUpdateTime != 0;
        }

        public bool Clear()
        {
            mUpdateTime = mInterval = 0;
            return true;
        }

        public void IncInterval(int nMilliSecs, int nLimit)
        {
            mInterval = Calculations.CutOverflow(mInterval + nMilliSecs, nLimit);
        }

        public void DecInterval(int nMilliSecs)
        {
            mInterval = Calculations.CutTrail(mInterval - nMilliSecs, 0);
        }

        public bool IsTimeOut(int nMilliSecs)
        {
            return Clock() >= mUpdateTime + nMilliSecs;
        }

        public bool ToNextTime(int nMilliSecs)
        {
            if (IsTimeOut(nMilliSecs)) return Update();
            return false;
        }

        public bool TimeOver(int nMilliSecs)
        {
            if (IsActive() && IsTimeOut(nMilliSecs)) return Clear();
            return false;
        }

        public bool ToNextTick(int nMilliSecs)
        {
            if (IsTimeOut(nMilliSecs))
            {
                if (Clock() >= mUpdateTime + nMilliSecs * 2)
                    return Update();
                mUpdateTime += nMilliSecs;
                return true;
            }

            return false;
        }

        public int GetRemain()
        {
            return mUpdateTime != 0
                       ? Calculations.CutRange(mInterval - ((int) Clock() - (int) mUpdateTime), 0, mInterval)
                       : 0;
        }

        public int GetInterval()
        {
            return mInterval;
        }

        public static implicit operator bool(TimeOutMS ms)
        {
            return ms.ToNextTime();
        }
    }
}
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Funcular.IdGenerators.Base36
{
    public static class ConcurrentStopwatch
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly object _lock = new object();

        private static readonly Stopwatch _sw = Stopwatch.StartNew();

        private static long _lastMicroseconds;

        private static DateTime _lastInitialized;

        private static TimeSpan _timeZero;

        public static long GetMicroseconds()
        {
            lock (_lock)
            {
                long microseconds = _lastMicroseconds;
                while (microseconds <= _lastMicroseconds)
                {
                    microseconds = (long) (_sw.Elapsed.TotalMilliseconds*1000.0);// TimeZero.Add(_sw.Elapsed).TotalMicroseconds();
                }
                _lastMicroseconds = microseconds;
                return microseconds;
            }

        }

        private static void Init()
        {
            _sw.Restart();
            _lastInitialized = DateTime.Now;
            _timeZero = _lastInitialized.Subtract(_epoch);
            _lastMicroseconds = DateTime.UtcNow.AddDays(1).Ticks / 10;
        }

        public static DateTime LastInitialized
        {
            get
            {
                if (_lastInitialized == default(DateTime))
                    Init();
                return _lastInitialized;
            }
        }

        public static TimeSpan TimeZero
        {
            get
            {
                if (_timeZero == default(TimeSpan))
                    Init();
                return _timeZero;
            }
        }
    }
}
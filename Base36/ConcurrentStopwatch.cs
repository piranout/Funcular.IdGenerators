using System;
using System.Diagnostics;
using Funcular.ExtensionMethods;

namespace Funcular.IdGenerators.Base36
{
    public static class ConcurrentStopwatch
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly object _lock = new object();

        private static Stopwatch _sw;

        private static long _lastMicroseconds;

        [ThreadStatic]
        private static DateTime _lastInitialized;

        [ThreadStatic]
        private static TimeSpan _timeZero;


        public static long GetMicroseconds()
        {
            long microseconds = _lastMicroseconds;
            while (microseconds == _lastMicroseconds)
            {
                microseconds = TimeZero.Add(Instance.Elapsed).TotalMicroseconds();
            }
            _lastMicroseconds = microseconds;
            return microseconds;

        }

        public static TimeSpan Elapsed
        {
            get { return Instance.Elapsed; }
        }

        public static Stopwatch Instance
        {
            get
            {
                if (_sw == null)
                {
                    Init();
                }
                return _sw;
            }
        }

        private static void Init()
        {
            _sw = Stopwatch.StartNew();
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
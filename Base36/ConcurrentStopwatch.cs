using System;
using System.Diagnostics;
using Funcular.ExtensionMethods;

namespace Funcular.IdGenerators.Base36
{
    public static class ConcurrentStopwatch
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [ThreadStatic] private static Stopwatch _sw;
                               
        [ThreadStatic] private static DateTime _lastInitialized;
                               
        [ThreadStatic] private static TimeSpan _timeZero;

        public static long GetMicroseconds()
        {
            return TimeZero.Add(Instance.Elapsed).TotalMicroseconds();
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
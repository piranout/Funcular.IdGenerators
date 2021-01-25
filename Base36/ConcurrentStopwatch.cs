using System;
using System.Diagnostics;

namespace Funcular.IdGenerators.Base36
{
    /// <summary>
    /// Thread safe microseconds stopwatch implementation. 
    /// </summary>
    public static class ConcurrentStopwatch
    {
        private static readonly DateTime _utcEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly object _lock = new object();

        private static readonly Stopwatch _sw = Stopwatch.StartNew();

        private static long _lastMicroseconds;

        private static readonly long _timeZeroMicroseconds;

        static ConcurrentStopwatch()
        {
            var lastInitialized = DateTime.UtcNow;
            var timeZero = lastInitialized.Subtract(_utcEpoch);
            _timeZeroMicroseconds = timeZero.Ticks/10;
        }

        /// <summary>
        /// Returns the time in microseconds since <paramref name="since"/>
        /// </summary>
        /// <param name="since"></param>
        /// <returns></returns>
        public static long GetMicroseconds(DateTimeOffset since)
        {
            lock (_lock)
            {
                return since.Subtract(_utcEpoch).Ticks / 10;
            }
        }

        /// <summary>
        /// Returns the Unix time in microseconds (µ″ since UTC epoch)
        /// </summary>
        /// <returns></returns>
        public static long GetMicroseconds()
        {
            lock (_lock)
            {
                long microseconds = 0;
                while (microseconds <= _lastMicroseconds)
                {
                    microseconds = _timeZeroMicroseconds + (_sw.Elapsed.Ticks / 10);
                }
                _lastMicroseconds = microseconds;
                return microseconds;
            }

        }

    }
}
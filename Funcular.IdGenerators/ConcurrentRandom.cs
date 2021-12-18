using System;
using System.Security.Cryptography;

namespace Funcular.IdGenerators
{
    public static class ConcurrentRandom
    {
        [ThreadStatic]
        private static Random _random;
        private static readonly object Lock = new object();
        private static long _lastValue;
#if !NET6_0
        private static readonly RNGCryptoServiceProvider RngCryptoServiceProvider;
#endif


        static ConcurrentRandom()
        {
#if !NET6_0
            RngCryptoServiceProvider = new RNGCryptoServiceProvider();
#endif
        }

        public static long NextLong()
        {
            lock (Lock)
            {
                long value;
                do
                {
                    value = (long)(Random.NextDouble() * MaxRandom);
                } while (value == _lastValue);
                _lastValue = value;
                return value;
            }
        }

        public static Random Random
        {
            get
            {
                if (_random != null)
                    return _random;
                var cryptoResult = new byte[4];
#if NET6_0
                cryptoResult = RandomNumberGenerator.GetBytes(4);
#else
                RngCryptoServiceProvider.GetBytes(cryptoResult);
#endif

                int seed = BitConverter.ToInt32(cryptoResult, 0);
                _random = new Random(seed);
                return _random;
            }
        }

        public static long MaxRandom { get; set; }
    }
}
using System;
using System.Security.Cryptography;
using Funcular.ExtensionMethods;

namespace Funcular.IdGenerators
{
    public static class ConcurrentRandom
    {
        [ThreadStatic]
        private static Random _random;
        private static readonly object _lock = new object();
        private static long _maxRandom;
        private static long _lastValue;
        private static readonly RNGCryptoServiceProvider _rngCryptoServiceProvider;

        static ConcurrentRandom()
        {
            _rngCryptoServiceProvider = new RNGCryptoServiceProvider();
        }

        public static long NextLong()
        {
            lock (_lock)
            {
                long value;
                do
                {
                    value = Random.NextLong(_maxRandom);
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
                _rngCryptoServiceProvider.GetBytes(cryptoResult);
                int seed = BitConverter.ToInt32(cryptoResult, 0);
                _random = new Random(seed);
                return _random;
            }
        }
        public static long MaxRandom
        {
            get { return _maxRandom; }
            set { _maxRandom = value; }
        }
    }
}
using System;
using System.Security.Cryptography;

namespace Funcular.IdGenerators
{
    public static class ConcurrentRandom
    {
        [ThreadStatic]
        private static Random _random;

        public static Random Random
        {
            get
            {
                if (_random == null)
                {
                    var cryptoResult = new byte[4];
                    new RNGCryptoServiceProvider().GetBytes(cryptoResult);
                    int seed = BitConverter.ToInt32(cryptoResult, 0);
                    _random = new Random(seed);
                }
                return _random;
            }
        }
    }
}
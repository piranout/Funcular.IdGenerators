#region File info

// *********************************************************************************************************
// Funcular.IdGenerators>Funcular.IdGenerators>Base36IdGenerator.cs
// Created: 2015-06-26 2:57 PM
// Updated: 2015-06-26 3:08 PM
// By: Paul Smith 
// 
// *********************************************************************************************************
// LICENSE: The MIT License (MIT)
// *********************************************************************************************************
// Copyright (c) 2010-2015 <copyright holders>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// *********************************************************************************************************

#endregion



#region Usings

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Funcular.IdGenerators.BaseConversion;
using Funcular.ExtensionMethods;
using Random = System.Random;
using Timer = System.Timers.Timer;
#endregion



namespace Funcular.IdGenerators.Base36
{
    public class Base36IdGenerator
    {
        private readonly int _numTimestampCharacters;
        private readonly int _numServerCharacters;
        private readonly int _numRandomCharacters;



        #region Private fields

        private static readonly string _hostHash;
        private static readonly DateTime _lastInitialized = DateTime.UtcNow;
        private static readonly object _rndLock = new object();
        private static readonly object _microsecondsLock = new object();
        private static readonly Mutex _mutex;
        private static long _lastMicroseconds;
        // reserved byte, start at the max Base36 value, can decrement 
        // up to 35 times when values are exhausted (every ~115 years),
        // or repurpose as a discriminator if desired:
        private static int _reserved = 35;
        private static string _reservedHash;

        private static DateTime _inService = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///     Field initializer never fires
        /// </summary>
        private static readonly MD5 _md5; 

        private static readonly Random _rnd;
        private static readonly Stopwatch _sw;
        private static readonly long _oneYearInMicroseconds = TimeSpan.FromDays(365.25*115).TotalMicroseconds();
        private static readonly long _maxRandom; // default = 36 ^ 3 - 1: 46655;
        private static TimeSpan _timeZero; 
        private static SpinLock _locker = new SpinLock(true);
        
        private Timer _checkReservedTimer;
        private bool _shouldCheckReserved;
        private bool _unsafeAllowed;
        private string _reservedValue;

        #endregion



        #region Public properties

        public string HostHash { get { return _hostHash; } }

        public DateTime InServiceDate { get { return _inService; } }

        public string Reserved { get { return _reservedHash ?? (_reservedHash = Base36Converter.FromInt32(_reserved)); } }

        public bool UnsafeAllowed { get { return this._unsafeAllowed; } set { this._unsafeAllowed = value; } }

        #endregion



        #region Constructors

        /// <summary>
        ///     Static constructor never fires
        /// </summary>
        static Base36IdGenerator()
        {
            Debug.WriteLine("Static constructor fired");
            // TODO: Include inception date in mutex name to reduce vulnerability
            // todo  to malicious DOS use (obscure the name).     
            _mutex = new Mutex(false, @"Global\Base36IdGeneratorMicroseconds");
            _md5 = MD5.Create();
            _rnd = new Random();
            new Regex("[^0-9a-zA-Z]", RegexOptions.Compiled);
            _hostHash = ComputeHostHash().PadLeft(2, '0');
            _reservedHash = Base36Converter.FromInt32(_reserved);
            _rndLock = new object();
            _timeZero = _lastInitialized.Subtract(_inService);
            _sw = Stopwatch.StartNew();
            _maxRandom = 46655;
        }

        /// <summary>
        ///     Instance constructor never fires
        /// </summary>
        public Base36IdGenerator(int numTimestampCharacters = 10, int numServerCharacters = 2, int numRandomCharacters = 3, string reservedValue = null)
        {
            this._numTimestampCharacters = numTimestampCharacters;
            this._numServerCharacters = numServerCharacters;
            this._numRandomCharacters = numRandomCharacters;
            this._reservedValue = reservedValue;
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Base36IdGenerator).TypeHandle);
            Debug.WriteLine("Instance constructor fired");
            this._checkReservedTimer = new Timer();
            this._checkReservedTimer.Elapsed += tmr_Elapsed;
            this._checkReservedTimer.Interval = 10000;
            this._checkReservedTimer.Start();

            string appSettingValue;
            if (ConfigurationManager.AppSettings.HasKeys()
                && ConfigurationManager.AppSettings.AllKeys.Contains("base36IdInceptionDate")
                && (appSettingValue = ConfigurationManager.AppSettings["base36IdInceptionDate"]).HasValue())
            {
                DateTime inService;
                if (DateTime.TryParse(appSettingValue, out inService))
                    _inService = inService;
            }

            _timeZero = _lastInitialized.Subtract(_inService);
            initStaticMicroseconds();
        }

        private void tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            checkReserved();
        }

        #endregion



        #region Public methods

        /*public Base36IdFields NewBase36Struct(string overrideHostString = null, byte? overrideReservedByte = null)
        {
            string tmp;
            if (string.IsNullOrEmpty(overrideHostString) && null == overrideReservedByte)
                tmp = NewBase36String();
            else
            {
                byte[] origBase = getBytes(NewBase36String());
                byte[] overrideHostBytes = getBytes(overrideHostString);
                if (overrideHostBytes.Length > 0)
                    origBase[10] = overrideHostBytes[0];
                if (overrideHostBytes.Length > 1)
                    origBase[11] = overrideHostBytes[1];
                if (overrideReservedByte.HasValue)
                    origBase[12] = overrideReservedByte.Value;
                tmp = getString(origBase);
            }
            return Parse(tmp);
            //return getString(origBase);
        }*/
        /// <summary>
        ///     Generates a new 16-character Base36 identifier with the 11th and 12th characters
        ///     replaced by the first two characters of the value supplied for <paramref name="overrideHostString" />.
        ///     If <paramref name="overrideReservedByte" /> is supplied, the
        ///     reserved 13th bit (normally 'Z') will be replaced by it.
        ///     Examples of overriding the host bytes might include re-purposing
        ///     those characters for entity type identification, or for synchronizing
        ///     a group of servers to issue ids in the same range. (This last example
        ///     somewhat increases the possibility of a collision by about 1.5432 * 10⁻³).
        /// </summary>
        /// <param name="overrideHostString"></param>
        /// <param name="overrideReservedByte"></param>
        /// <returns></returns>
        /// <summary>
        ///     Generates a unique, sequential, Base36 string, 16 characters long.
        ///     The first 10 characters are the microseconds elapsed since the InService DateTime
        ///     (constant field you hardcode in this file).
        ///     The next 2 characters are a compressed checksum of the MD5 of this host.
        ///     The next 1 character is a reserved constant of 36 ('Z' in Base36).
        ///     The last 3 characters are random number less than 46655 additional for additional uniqueness.
        /// </summary>
        /// <returns>Returns a unique, sequential, 16-character Base36 string</returns>
        public string NewBase36String()
        {
            return NewBase36String(null);
        }

        public string NewBase36StringConcat()
        {
            string rndHexStr = getRandomDigitsLock();
            long microseconds = GetMicrosecondsCrossProcess();
            return Base36Converter.FromInt64(microseconds).PadLeft(10, '0')
                   + _hostHash
                   + _reservedHash
                   + Base36Converter.FromHex(rndHexStr).PadLeft(3, '0');
        }

        /// <summary>
        ///     Generates a unique, sequential, Base36 string, 16 characters long.
        ///     The first 10 characters are the microseconds elapsed since the InService DateTime
        ///     (constant field you hardcode in this file).
        ///     The next 2 characters are a compressed checksum of the MD5 of this host.
        ///     The next 1 character is a reserved constant of 36 ('Z' in Base36).
        ///     The last 3 characters are random number less than 46655 additional for additional uniqueness.
        /// </summary>
        /// <param name="delimiter">
        ///     If provided, formats the ID as four groups of
        ///     4 characters separated by the delimiter.
        /// </param>
        /// <returns>Returns a unique, sequential, 16-character Base36 string</returns>
        public string NewBase36String(string delimiter)
        {
            // Keep access sequential so threads cannot accidentally
            // read another thread's values within this method:
            // For 2 chars from host id MD5...
            // string hostHash = (_hostHash ?? (_hostHash = ComputeHostHash()));

            // 1 reserved char; static largest base36 digit.
            // If the same ID system, scheme and sequence is still in use 
            // more than 115.85 years after in-service date, decrements
            // 'reserved' by 1 for each whole multiple of 115 years
            // elapsed, up to 35 times max. If the same system, scheme
            // and sequence is still in service 3,850 years from the
            // initial go-live, you probably have bigger problems than 
            // ID collisions...
            string rndHexStr;
            StringBuilder sb;
            sb = new StringBuilder();
            //lock (_lockObj)
            //{
            // 3 chars random in Base36 = 46656 units
            rndHexStr = getRandomDigitsLock(); // this.UnsafeOkay ? getRandomDigitsUnsafe() : getRandomDigitsLock();

            // Microseconds since InService (using Stopwatch) provides the 
            // first 10 chars:
            //}
            long microseconds = GetMicrosecondsCrossProcess();
            sb.Append(Base36Converter.FromInt64(microseconds).PadLeft(10, '0'));
            sb.Length = 10;
            sb.Append(_hostHash); //.PadLeft(2, '0'));
            sb.Length = 12;
            sb.Append(_reservedHash);
            sb.Length = 13;
            sb.Append(Base36Converter.FromHex(rndHexStr).PadLeft(3, '0'));
            sb.Length = 16;
            if (!string.IsNullOrEmpty(delimiter))
            {
                sb.Insert(12, delimiter);
                sb.Insert(8, delimiter);
                sb.Insert(4, delimiter);
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Compresses the MD5 of this server's hostname by summing the bytes until the sum
        ///     fits into two Base36 characters (&lt;= 36^2, or 1296):
        /// </summary>
        /// <returns>2 character Base36 checksum of MD5 of hostname</returns>
        public static string ComputeHostHash()
        {
            return Base36Converter.Encode(Math.Abs(Dns.GetHostName().GetHashCode()));
            /*string hash = Base36Converter.Encode(
                (_hostSum.HasValue ? _hostSum.Value : (
                    _hostSum = getBytesSum(computeMd5(getHostBytes()), 36, 2)).Value));
            return hash;*/
        }

        /// <summary>
        ///     For internal use; use the parameterless overload for effeciency.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public string ComputeHostHash(string host)
        {
            long sum = new long?(getBytesSum(computeMd5(host), 36, 2)).Value;
            string hash = Base36Converter.Encode(sum);
            return hash;
        }

        /*public Base36IdFields Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(paramName: "value");
            string ret = _regex.Replace(value, "").ToUpper();
            if (ret.Length != 16)
                throw new ArgumentException("Value was not a valid Base36 id", "value");

            Base36IdFields fields = new Base36IdFields
            {
                Id = ret,
                HostHash = ret.Substring(10, 2),
                Reserved = ret.Substring(12, 1),
                InService = _inService
            };
            int pos = Base36Converter.CharList.IndexOf(fields.Reserved, StringComparison.Ordinal);
            int centuryCycles = Math.Max(0, ((35 - pos) - 1));
            // some precision is lost, as chars beyond 10 weren't originally from the timestamp 
            // component of the ID:
            int probableEncodedLength =
                Base36Converter.FromInt64(InServiceDate.AddDays(365.25 * 115.89 * centuryCycles).Ticks / 10).Length;
            if (centuryCycles == 0)
                probableEncodedLength = 10;
            string encodedMs = ret.Substring(0, probableEncodedLength);
            long ms = Base36Converter.Decode(encodedMs);
            fields.Microseconds = ms;
            fields.CreatedRoughly = _inService.AddTicks(ms * 10); //.AddTicks((3656158440062976 * 10 + 9) * centuryCycles);
            return fields;
        }*/
        /// <summary>
        ///     Returns value with all non base 36 characters removed.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <summary>
        ///     Return the elapsed microseconds since the in-service DateTime; will never
        ///     return the same value twice, even across multiple processes.
        /// </summary>
        /// <returns></returns>
        public static long GetMicrosecondsCrossProcess()
        {
            try
            {
                _mutex.WaitOne();
                long microseconds;
                do
                {
                    microseconds = (_timeZero.Add(_sw.Elapsed).TotalMicroseconds());
                }
                while (microseconds <= Thread.VolatileRead(ref _lastMicroseconds));
                //Thread.VolatileWrite(ref this._lastMicroseconds, microseconds);
                Interlocked.Exchange(ref _lastMicroseconds, microseconds);
                return microseconds;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     Return the elapsed microseconds since the in-service DateTime; will never
        ///     return the same value twice. Uses a high-resolution Stopwatch (not DateTime.Now)
        ///     to measure durations.
        /// </summary>
        /// <returns></returns>
        public static long GetMicroseconds()
        {
            lock (_microsecondsLock)
            {
                long microseconds;
                do
                {
                    microseconds = (_timeZero.Add(_sw.Elapsed).TotalMicroseconds());
                }
                while (microseconds <= Thread.VolatileRead(ref _lastMicroseconds));
                Thread.VolatileWrite(ref _lastMicroseconds, microseconds);
                //Interlocked.Exchange(ref _lastMicroseconds, microseconds);
                return microseconds;
            }
        }

        private void checkReserved()
        {
            this._shouldCheckReserved = _lastMicroseconds > _oneYearInMicroseconds;
            if (this._shouldCheckReserved)
            {
                _reserved = 35 - Convert.ToInt32(_lastMicroseconds/3656158440062975);
                _lastMicroseconds -= _oneYearInMicroseconds;
            }
        }

        #endregion



        #region Nonpublic methods

        private void initStaticMicroseconds()
        {
            _lastMicroseconds = GetMicroseconds();
        }

        /// <summary>
        ///     Returns a random Base36 number 3 characters long.
        /// </summary>
        /// <returns></returns>
        private string getRandomDigitsSpinLock()
        {
            long value;
            //bool lockTaken = false;
            //try
            //{
            //    _rndSpinLock.Enter(ref lockTaken);
            //lock(_lockObj)
            while (true)
            {
                bool lockTaken = false;
                try
                {
                    //if (_spinLockTaken == false)
                    //{
                    _locker.Enter(ref lockTaken);
                    value = _rnd.NextLong(_maxRandom);
                    break; //_rnd.Next(46655);
                    //}
                    // Do stuff...
                }
                finally
                {
                    if (lockTaken) // && _locker.IsHeldByCurrentThread)
                    {
                        _locker.Exit(false);
                        //                        _spinLockTaken = false;
                    }
                }
            }
            return Base36Converter.Encode(value); //_rng.Value.Next(46655);// 
        }

        private string getRandomDigitsUnsafe()
        {
            //lock (_lockObj)
            long value = _rnd.NextLong(_maxRandom);
            return Base36Converter.Encode(value);
        }

        private static string getRandomDigitsLock()
        {
            // NOTE: Using a mutex would enable cross-process locking.
            lock (_rndLock)
            {
                long next = _rnd.NextLong(_maxRandom);
                return Base36Converter.Encode(next);
            }
        }

        private static byte[] computeMd5(string value)
        {
            return _md5.ComputeHash(getBytes(value));
        }

        private static byte[] computeMd5(byte[] value)
        {
            return _md5.ComputeHash(value);
        }

        /// <summary>
        ///     Recursively sums a byte array until the value will fit within
        ///     <paramref name="maxChars" /> characters in the base specified by
        ///     <paramref name="forBase" />.
        /// </summary>
        /// <param name="val">The byte array to checksum</param>
        /// <param name="maxChars">The max number of characters to hold the checksum</param>
        /// <param name="forBase">The base in which the checksum will be expressed</param>
        /// <returns></returns>
        private static long getBytesSum(byte[] val, int forBase, int maxChars)
        {
            long maxVal = Convert.ToInt64(Math.Pow(forBase, maxChars));
            byte[] arr = val;
            long tmp;
            do
            {
                tmp = 0;
                foreach (var t in arr)
                {
                    tmp += t;
                }
                arr = BitConverter.GetBytes(tmp);
            }
            while (tmp > maxVal);
            return tmp; // arr.Where(b => b != 0).ToArray();
        }

        /// <summary>
        ///     Returns a byte array containing the HostName if available, or
        ///     else the mac address of the fastest non-loopback, non-tunnel
        ///     NIC available. If neither can be determined, will default to an
        ///     array of 6 bytes all set to 35.
        /// </summary>
        /// <returns></returns>
        private static byte[] getHostBytes()
        {
            const int MIN_MAC_ADDR_LENGTH = 6;
            byte[] defaultBytes = {35, 35, 35, 35, 35, 35};
            byte[] retBytes = {0};
            byte[] hostnameBytes = getBytes(Dns.GetHostName());
            if (hostnameBytes.Length > 3)
                retBytes = hostnameBytes;
            else
            {
                byte[] macBytes;
                try
                {
                    List<NetworkInterface> candidateInterfaces =
                        NetworkInterface.GetAllNetworkInterfaces().ToList().Where(nw =>
                            nw.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && nw.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                            && nw.OperationalStatus == OperationalStatus.Up
                            && nw.GetPhysicalAddress().GetAddressBytes().Length >= MIN_MAC_ADDR_LENGTH)
                            .ToList();
                    macBytes = candidateInterfaces.Max(nw => nw.GetPhysicalAddress().GetAddressBytes());
                    if (macBytes.Length >= 6)
                        retBytes = macBytes;
                }
                catch (Exception ex)
                { // back to default
                    Trace.WriteLine(ex);
                    macBytes = defaultBytes;
                    retBytes = macBytes;
                }
            }
            if ((retBytes).Length < 6)
                retBytes = retBytes.Union(defaultBytes).ToArray();
            return retBytes;
        }

        /// <summary>
        ///     Shorthand for Encoding.Default.GetBytes
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static byte[] getBytes(string str)
        {
            return Encoding.Default.GetBytes(str);
        }

        /// <summary>
        ///     Shorthand for Encoding.Default.GetString
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string getString(byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }

       

        #endregion
    }
}
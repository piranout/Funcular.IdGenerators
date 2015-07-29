#region File info

// *********************************************************************************************************
// Funcular.IdGenerators>Funcular.IdGenerators>Base36Converter.cs
// Created: 2015-06-26 2:42 PM
// Updated: 2015-06-26 2:43 PM
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
using System.Globalization;
using System.Linq;

#endregion



namespace Funcular.IdGenerators.BaseConversion
{
    public static class Base36Converter
    {
        private static string _charList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        ///     The character set for encoding. Defaults to upper-case alphanumerics 0-9, A-Z.
        /// </summary>
        public static string CharList { get { return _charList; } set { _charList = value; } }

        public static string FromHex(string hex)
        {
            BaseConverter.CharList = _charList;
            return BaseConverter.Convert(hex.ToUpper(), 16, 36);
        }

        public static string FromGuid(Guid guid)
        {
            BaseConverter.CharList = _charList;
            return BaseConverter.Convert(guid.ToString("N"), 16, 36);
        }

        public static string FromInt32(int int32)
        {
            BaseConverter.CharList = _charList;
            return BaseConverter.Convert(int32.ToString(CultureInfo.InvariantCulture), 10, 36);
        }

        public static string FromInt64(long int64)
        {
            BaseConverter.CharList = _charList;
            return BaseConverter.Convert(number: int64.ToString(CultureInfo.InvariantCulture), fromBase: 10, toBase: 36);
        }

        /// <summary>
        /// Converts the given decimal number to the numeral system with the
        /// specified radix (in the range [2, 36]).
        /// </summary>
        /// <param name="decimalNumber">The number to convert.</param>
        /// <returns></returns>
        public static string FromLong(long decimalNumber)
        {
            const int BITS_IN_LONG = 64;
            const int RADIX = 36;
            const string DIGITS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (decimalNumber == 0)
                return "0";

            int index = BITS_IN_LONG - 1;
            long currentNumber = Math.Abs(decimalNumber);
            char[] charArray = new char[BITS_IN_LONG];

            while (currentNumber != 0)
            {
                int remainder = (int)(currentNumber % RADIX);
                charArray[index--] = DIGITS[remainder];
                currentNumber = currentNumber / RADIX;
            }

            string result = new String(charArray, index + 1, BITS_IN_LONG - index - 1);
            if (decimalNumber < 0)
            {
                result = "-" + result;
            }

            return result;
        }

        /// <summary>
        ///     Encode the given number into a Base36 string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static String Encode(long input)
        {
            if (input < 0)
                throw new ArgumentOutOfRangeException("input", input, "input cannot be negative");
            char[] clistarr = CharList.ToCharArray();
            var result = new Stack<char>();
            while (input != 0)
            {
                result.Push(clistarr[input%36]);
                input /= 36;
            }
            return new string(result.ToArray());
        }

        /// <summary>
        ///     Decode the Base36 Encoded string into a number
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Int64 Decode(string input)
        {
            IEnumerable<char> reversed = input.ToUpper().Reverse();
            long result = 0;
            int pos = 0;
            foreach (var c in reversed)
            {
                result += CharList.IndexOf(c)*(long) Math.Pow(36, pos);
                pos++;
            }
            return result;
        }
    }
}
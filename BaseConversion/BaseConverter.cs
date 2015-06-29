#region File info

// *********************************************************************************************************
// Funcular.IdGenerators>Funcular.IdGenerators>BaseConverter.cs
// Created: 2015-06-26 2:41 PM
// Updated: 2015-06-26 2:44 PM
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
using System.Linq;

#endregion



namespace Funcular.IdGenerators.BaseConversion
{
    /// <summary>
    ///     A Base36 De- and Encoder
    /// </summary>
    /// <remarks>
    ///     Adapted from the base36 encoder at
    ///     http://www.stum.de/2008/10/20/base36-encoderdecoder-in-c/
    /// </remarks>
    public static class BaseConverter
    {
        private static string _charList;

        /// <summary>
        ///     The character set for encoding.
        /// </summary>
        public static string CharList { get { return _charList; } set { _charList = value; } }

        /// <summary>
        ///     Convert a <paramref name="number" /> (expressed as a string) from <paramref name="fromBase" /> to
        ///     <paramref name="toBase" />
        /// </summary>
        /// <param name="number">String representation of the number to be converted</param>
        /// <param name="fromBase">The current base of the number</param>
        /// <param name="toBase">The desired base to convert to</param>
        /// <returns></returns>
        public static string Convert(string number, int fromBase, int toBase)
        {
            if (string.IsNullOrEmpty(_charList))
                throw new FormatException("You must populate .CharList before calling Convert().");
            int length = number.Length;
            string result = string.Empty;
            List<int> nibbles = number.Select(c => CharList.IndexOf(c)).ToList();
            int newlen;
            do
            {
                int value = 0;
                newlen = 0;
                for (int i = 0; i < length; ++i)
                {
                    value = value*fromBase + nibbles[i];
                    if (value >= toBase)
                    {
                        if (newlen == nibbles.Count)
                            nibbles.Add(0);
                        nibbles[newlen++] = value/toBase;
                        value %= toBase;
                    }
                    else if (newlen > 0)
                    {
                        if (newlen == nibbles.Count)
                            nibbles.Add(0);
                        nibbles[newlen++] = 0;
                    }
                }
                length = newlen;
                result = CharList[value] + result;
            }
            while (newlen != 0);
            return result;
        }
    }
}
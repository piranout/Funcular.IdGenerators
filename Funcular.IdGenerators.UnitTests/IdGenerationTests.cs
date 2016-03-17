#region File info

// *********************************************************************************************************
// Funcular.IdGenerators>Funcular.IdGenerators.UnitTests>IdGenerationTests.cs
// Created: 2015-06-29 12:32 PM
// Updated: 2015-06-29 4:13 PM
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Funcular.ExtensionMethods;
using Funcular.IdGenerators.Base36;
using Funcular.IdGenerators.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion


// ReSharper disable RedundantArgumentDefaultValue
namespace Funcular.IdGenerators.UnitTests
{
    [TestClass]
    public class IdGenerationTests
    {
        private Base36IdGenerator _idGenerator;
        private string _delimiter;
        private int[] _delimiterPositions;

        [TestInitialize]
        public void Setup()
        {
            this._delimiter = "-";
            this._delimiterPositions = new[] {15, 10, 5};
            this._idGenerator = new Base36IdGenerator(
                numTimestampCharacters: 11,
                numServerCharacters: 5,
                numRandomCharacters: 4,
                reservedValue: "",
                delimiter: this._delimiter,
                // give the positions in reverse order if you
                // don't want to have to account for modifying
                // the loop internally. To do the same in ascending
                // order, you would need to pass 5, 11, 17:
                delimiterPositions: this._delimiterPositions);
                // delimiterPositions: new[] {5, 11, 17});
        }

        [TestMethod]
        public void Initialize()
        {
            _idGenerator.NewId();
        }

        [TestMethod]
        public void Ids_Are_Ascending()
        {
            string id1 = this._idGenerator.NewId();
            string id2 = this._idGenerator.NewId();
            Assert.IsTrue(String.Compare(id2, id1, StringComparison.OrdinalIgnoreCase) > 0);
        }

        [TestMethod]
        public void Server_Hash_Does_Not_Throw()
        {
            string result;
            try
            {
                Assert.IsTrue((result = this._idGenerator.ComputeHostHash("RD00155DC193F9")).HasValue());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void Id_Length_Is_Correct()
        {
            // These are the segment lengths passed to the constructor:
            int expectedLength = 11 + 5 + 0 + 4;
            string id = this._idGenerator.NewId();
            Assert.AreEqual(id.Length, expectedLength);
            // Should include 3 delimiter dashes when called with (true):            
            id = this._idGenerator.NewId(true);
            Assert.AreEqual(id.Length, expectedLength + 3);
        }

        // Uncomment this method to run an extended, multithreaded test to ensure
        //  Id generation is thread safe and maintains uniqueness across threads.
        //  This method requires running from one to several seconds, so it needen't
        //  be part of every build.
        [TestMethod]
        public void Ids_Do_Not_Collide()
        {
            var ids = new ConcurrentDictionary<string, string>();
            var cancellationTokenSource = new CancellationTokenSource();
            // Increase the concurrent tasks for more thorough testing.
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task(() =>
                {
                    while (true)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            return;
                        if (!ids.TryAdd(this._idGenerator.NewId(), ""))
                            Assert.Fail();
                    }
                }, cancellationTokenSource.Token);
                tasks[i].Start();
            }
            // Lengthen the timespan for more thorough testing.
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2));
            while (cancellationTokenSource.IsCancellationRequested == false)
                Thread.Yield();
            Debug.WriteLine(ids.Count);
        }

        [TestMethod]
        public void Formatted_Id_Has_Correct_Length()
        {
            var id = _idGenerator.NewId();
            var length = id.Length;
            var formatted = _idGenerator.Format(id);
            Assert.IsTrue(formatted.Contains(_delimiter) && formatted.Length == length + (_delimiter.Length * _delimiterPositions.Length));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Timestamps_Throw_Out_Of_Range()
        {
            _idGenerator.GetTimestamp(13);
        }

        [TestMethod]
        [ExpectedException(typeof(OverflowException))]
        public void Timestamps_Throws_Overflow_When_Strict()
        {
            _idGenerator.GetTimestamp(length: 5, resolution: TimestampResolution.Ticks, strict: true);
        }

        [TestMethod]
        public void Timestamp_Only_Throws_Overflow_When_Strict()
        {
            Assert.IsTrue
                (_idGenerator.GetTimestamp(length: 10, resolution: TimestampResolution.Day, strict: false).HasValue());
        }

        [TestMethod]
        public void Timestamp_Is_Expected_Length()
        {
            Assert.IsTrue
                (_idGenerator.GetTimestamp(length: 10, resolution: TimestampResolution.Day, strict: false).Length == 10);
        }
    }
}
// ReSharper restore RedundantArgumentDefaultValue

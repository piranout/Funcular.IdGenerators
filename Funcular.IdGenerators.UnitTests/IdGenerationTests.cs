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
using Funcular.IdGenerators.Base36;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion



namespace Funcular.IdGenerators.UnitTests
{
    [TestClass]
    public class IdGenerationTests
    {
        private Base36IdGenerator _idGenerator;

        [TestInitialize]
        public void Setup()
        {
            this._idGenerator = new Base36IdGenerator(
                numTimestampCharacters: 11,
                numServerCharacters: 5,
                numRandomCharacters: 4,
                reservedValue: "",
                delimiter: "-",
                // give the positions in reverse order if you
                // don't want to have to account for modifying
                // the loop internally. To do the same in ascending
                // order, you would need to pass 5, 11, 17:
                // delimiterPositions: new[] {15, 10, 5});
                delimiterPositions: new[] {5, 11, 17});
        }

        [TestMethod]
        public void TestIdsAreAscending()
        {
            string id1 = this._idGenerator.NewId();
            string id2 = this._idGenerator.NewId();
            Assert.IsTrue(String.Compare(id2, id1, StringComparison.OrdinalIgnoreCase) > 0);
        }

        [TestMethod]
        public void TestIdLengthsAreAsExpected()
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
        //[TestMethod]
        public void TestCollisionAvoidance()
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
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));
            while (cancellationTokenSource.IsCancellationRequested == false)
                Thread.Yield();
            Debug.WriteLine(ids.Count);
        }
    }
}
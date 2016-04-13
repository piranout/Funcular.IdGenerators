using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Funcular.IdGenerators.Base36;

namespace Funcular.IdGenerators.PerformanceTests
{
    internal class PerformanceTests
    {
        private readonly Base36IdGenerator _generator = new Base36IdGenerator(11, 4, 5, null, "-", new[] {15, 10, 5});
        private readonly HashSet<string> _ids = new HashSet<string>();

        [ThreadStatic]
        private static string _newId;

        public PerformanceTests(Base36IdGenerator generator)
        {

        }

        internal int TestSingleThreaded(int seconds)
        {
            string newId = _generator.NewId();
            Console.WriteLine($"First Id generated: {newId}");
            var hashSet = new HashSet<string>();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < seconds)
            {
                newId = _generator.NewId();
                if (!hashSet.Add(newId))
                {
                    throw new InvalidOperationException("Duplicate id!");
                }
            }
            Console.WriteLine($"Last Id generated: {newId}\r\n");
            return hashSet.Count;
        }

        internal int TestTimestampUniqueness(int seconds, int threads)
        {
            _ids.Clear();
            var source = new CancellationTokenSource();
            for (var i = 0; i < threads; i++)
            {
                ThreadPool.QueueUserWorkItem((MakeIdsMultithreaded), source.Token);
            }
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            source.Cancel();
            Console.WriteLine();
            return _ids.Count;
        }

        internal int TestMultithreaded(int seconds, int threads)
        {
            _ids.Clear();
            var source = new CancellationTokenSource();
            for (var i = 0; i < threads; i++)
            {
                ThreadPool.QueueUserWorkItem((MakeIdsMultithreaded), source.Token);
            }
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            source.Cancel();
            Console.WriteLine();
            return _ids.Count;
        }
        
        private void MakeIdsMultithreaded(object cancellationToken)
        {
            while (!((CancellationToken)cancellationToken).IsCancellationRequested)
            {
                _newId = _generator.NewId();
                lock (_ids)
                {

                    if (!_ids.Add(_newId))
                    {
                        Console.WriteLine("Current Count: {0}", _ids.Count);
                        Console.WriteLine("Last Id: {0}", _newId);
                        Console.WriteLine("ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
                        Console.WriteLine("ThreadId of duplicate value: {0}", _newId);

                        throw new InvalidOperationException("Duplicate id!");
                    }
                }
            }
        }
    }
}
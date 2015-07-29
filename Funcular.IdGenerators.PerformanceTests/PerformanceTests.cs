using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Funcular.IdGenerators.Base36;

namespace Funcular.IdGenerators.PerformanceTests
{
    internal class PerformanceTests
    {
        private readonly Base36IdGenerator _generator;
        private readonly ConcurrentDictionary<string, string> _ids = new ConcurrentDictionary<string, string>();

        public PerformanceTests(Base36IdGenerator generator)
        {
            _generator = generator;
        }

        internal int TestSingleThreaded(int seconds)
        {
            _ids.Clear();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.Seconds < seconds)
            {
                if (!_ids.TryAdd(_generator.NewId(), null))
                {
                    throw new InvalidOperationException("Duplicate id!");
                }
            }
            return _ids.Count;
        }

        internal int TestMultithreaded(int seconds, int threads)
        {
            var source = new CancellationTokenSource();
            for (var i = 0; i < threads; i++)
            {
                ThreadPool.QueueUserWorkItem((MakeIdsMultithreaded), source.Token);
            }
            source.CancelAfter(TimeSpan.FromSeconds(seconds));
            for (var i = 0; i < seconds; i++)
            {
                Console.Write("... ");
                Thread.Sleep(1000);
            }
            Console.WriteLine();
            return _ids.Count;
        }

        private void MakeIdsMultithreaded(object cancellationToken)
        {
            _ids.Clear();
            while (!((CancellationToken) cancellationToken).IsCancellationRequested)
            {
                if (!_ids.TryAdd(_generator.NewId(), null))
                {
                    throw new InvalidOperationException("Duplicate id!");
                }
            }
        }
    }
}
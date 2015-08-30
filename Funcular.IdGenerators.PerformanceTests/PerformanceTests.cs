using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Funcular.IdGenerators.Base36;

namespace Funcular.IdGenerators.PerformanceTests
{
    internal class PerformanceTests
    {
        private readonly ThreadLocal<Base36IdGenerator> _generator = new ThreadLocal<Base36IdGenerator>(() => new Base36IdGenerator(11,4,5,"","-",new []{15,10,5}));
        private readonly ConcurrentDictionary<string, int> _ids = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<long, TimeSpan> _timestamps = new ConcurrentDictionary<long, TimeSpan>();
        
        [ThreadStatic]
        private static string _newId;

        public PerformanceTests(Base36IdGenerator generator)
        {

        }

        internal int TestSingleThreaded(int seconds)
        {
            _ids.Clear();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < seconds)
            {
                if (!_ids.TryAdd(_generator.Value.NewId(), Thread.CurrentThread.ManagedThreadId))
                {
                    throw new InvalidOperationException("Duplicate id!");
                }
            }
            return _ids.Count;
        }

        internal int TestTimestampUniqueness(int seconds, int threads)
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

        private void MakeTimestampsMultithreaded(object cancellationToken)
        {
            _timestamps.Clear();
            while (!((CancellationToken)cancellationToken).IsCancellationRequested)
            {
                var timestamp = ConcurrentStopwatch.GetMicroseconds(); 
                if (!_timestamps.TryAdd(timestamp, DateTime.Now.TimeOfDay))
                {
                    var value = _timestamps[timestamp];
                    var indexOf = _timestamps.Keys.ToList().IndexOf(timestamp);
                    Console.WriteLine("Current timestamp count: {0}", _ids.Count);
                    Console.WriteLine("Last Id: {0}", _newId);
                    Console.WriteLine("ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("ThreadId of duplicate timestamp: {0}", value);
                    Console.WriteLine("Index of existing item: {0}", indexOf);

                    throw new InvalidOperationException("Duplicate timestamp!");
                }
            }
        }

        private void MakeIdsMultithreaded(object cancellationToken)
        {
            _ids.Clear();
            while (!((CancellationToken)cancellationToken).IsCancellationRequested)
            {
                _newId = _generator.Value.NewId();
                if (!_ids.TryAdd(_newId, Thread.CurrentThread.ManagedThreadId))
                {
                    var value = _ids[_newId];
                    var indexOf = _ids.Keys.ToList().IndexOf(_newId);
                    Console.WriteLine("Current Count: {0}", _ids.Count);
                    Console.WriteLine("Last Id: {0}", _newId);
                    Console.WriteLine("ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("ThreadId of duplicate value: {0}", value);
                    Console.WriteLine("Index of existing item: {0}", indexOf);

                    throw new InvalidOperationException("Duplicate id!");
                }
            }
        }
    }
}
using System;
using System.Diagnostics;
using Funcular.IdGenerators.Base36;

namespace Funcular.IdGenerators.PerformanceTests
{
    internal class Program
    {
        private static Base36IdGenerator _generator;
        private static PerformanceTests _tests;

        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {

            var seconds = 20;
            Console.WriteLine("Begin performance testing; {0} seconds each async/sync…", seconds);
            Console.WriteLine(); 
            _tests = SetupTests();
            long asyncAggregate=0;
            long syncAggregate = 0;
            long timestampAggregate = 0;
            // get some initialization out of the way:
            RunAsyncTest(5);
            Console.Clear();
            var includeTimestampTests = args.Length > 0 && args[0].ToUpper() == "T";
            int i;
            for (i = 0; i < 10; i++)
            {
                if(includeTimestampTests)
                    timestampAggregate += RunTimestampTest(seconds);
                asyncAggregate += RunAsyncTest(seconds);
                syncAggregate += RunSyncTest(seconds);
            }
            if (includeTimestampTests)
                Console.WriteLine("\r\nTimestamp avg: {0:n0}/s", timestampAggregate / i);
            Console.WriteLine("\r\nAsync average: {0:n0}/s", asyncAggregate / i);
            Console.WriteLine("\r\n Sync average: {0:n0}/s", syncAggregate / i);

            Console.WriteLine("\r\n");
            PromptKey("Press any key to exit... ");
        }

        private static int RunSyncTest(int seconds)
        {
            var sw = Stopwatch.StartNew();
            var testSingleThreaded = _tests.TestSingleThreaded(seconds);
            var count = testSingleThreaded;
            sw.Stop();
            Console.WriteLine();
            var rate = count/sw.Elapsed.Seconds;
            Console.WriteLine("Synchronously:\tCreated {0:n0} Ids in {1}; rate {2:n0}/s", count, sw.Elapsed,
                rate);
            return rate;
        }

        private static int RunAsyncTest(int seconds)
        {
            var processors = Environment.ProcessorCount + 1;
            var sw = Stopwatch.StartNew();
            var count = _tests.TestMultithreaded(seconds, processors);
            sw.Stop();
            Console.WriteLine();
            var rate = count/sw.Elapsed.Seconds;
            Console.WriteLine("Asynchronously:\tCreated {0:n0} Ids in {1}; rate {2:n0}/s", count, sw.Elapsed,
                rate);
            return rate;
        }

        private static int RunTimestampTest(int seconds)
        {
            var processors = Environment.ProcessorCount + 1;
            var sw = Stopwatch.StartNew();
            var count = _tests.TestTimestampUniqueness(seconds, processors);
            sw.Stop();
            Console.WriteLine();
            var rate = count / sw.Elapsed.Seconds;
            Console.WriteLine("Asynchronously:\tCreated {0:n0} timestamps in {1}; rate {2:n0}/s", count, sw.Elapsed,
                rate);
            return rate;
        }


        private static PerformanceTests SetupTests()
        {
            _generator = new Base36IdGenerator(11, 4, 5, "", "-", new[] {15, 10, 5});
            var tests = new PerformanceTests(_generator);
            return tests;
        }

        private static void PromptKey(string prompt)
        {
            Console.Write(prompt);
            Console.ReadKey(true);
        }
    }
}
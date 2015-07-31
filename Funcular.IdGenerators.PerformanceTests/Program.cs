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

            var seconds = 10;
            Console.WriteLine("Begin performance testing; {0} seconds each async/sync…", seconds);
            Console.WriteLine(); 
            _tests = SetupTests();
            RunAsyncTest(seconds);
            RunSyncTest(seconds);
            Console.WriteLine("\r\n");
            PromptKey("Press any key to exit... ");
        }

        private static void RunSyncTest(int seconds)
        {
            var sw = Stopwatch.StartNew();
            var testSingleThreaded = _tests.TestSingleThreaded(seconds);
            var count = testSingleThreaded;
            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("Synchronously:\tCreated {0:n0} Ids in {1}; rate {2:n0}/s", count, sw.Elapsed,
                count/sw.Elapsed.Seconds);
        }

        private static void RunAsyncTest(int seconds)
        {
            var processors = Environment.ProcessorCount + 1;
            var sw = Stopwatch.StartNew();
            var count = _tests.TestMultithreaded(seconds, processors);
            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("Asynchronously:\tCreated {0:n0} Ids in {1}; rate {2:n0}/s", count, sw.Elapsed,
                count/sw.Elapsed.Seconds);
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
//css_dir .\..\
//css_inc cli.cs
//css_inc xunit.polyfill.cs
//css_ref cscs.dll
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CLI;
using CSScripting;
using static dbg; // to use 'print' instead of 'dbg.print'
using Xunit;
using Xunit.Abstractions;

class Script
{
    static public void Main(string[] args)
    {
        if (args.Contains("?") || args.Contains("-?") || args.Contains("-help"))
        {
            string version = Path.GetFileNameWithoutExtension(
                       Directory.GetFiles(Path.GetDirectoryName(GetEnvironmentVariable("EntryScript")), "*.version")
                                .FirstOrDefault() ?? "0.0.0.0.version");

            Console.WriteLine($@"v{version} ({GetEnvironmentVariable("EntryScript")})");
            Console.WriteLine("Execute self-testing (unit-tests) of the script engine.");
            return;
        }

        Environment.SetEnvironmentVariable("css_test_asm", Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", @"cscs.dll")));

        var test_class = new cscs_cli(new ConsoleTestOutputHelper());

        CliTestFolder.Set(Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), "cs-script.test"));

        print("Discovering tests...");

        var tests = typeof(cscs_cli)
            .GetMethods()
            .Where(m =>
            {
                bool isWinHost = Environment.OSVersion.IsWin();

                var attrs = m.GetCustomAttributes(false);

                bool isTest = attrs.OfType<FactAttribute>().Any(x => x.Skip == null);
                bool isWinOnlyTest = attrs.OfType<FactWinOnlyAttribute>().Any();

                return isTest && (isWinHost || !isWinOnlyTest);
            });

        int? requestedTest = null;

        if (args.Any())
        {
            if (int.TryParse(args[0], out int value))
                requestedTest = value;
        }

        var passed = 0;
        var failed = 0;

        var sw = Stopwatch.StartNew();

        print($"Starting the test execution of {tests.Count()} tests in:\n\t", CliTestFolder.root);
        print("================================");

        var index = 0;
        //Parallel.ForEach(tests, test =>
        foreach (var test in tests)
        {
            index++;

            if (requestedTest.HasValue && requestedTest != index)
                continue;

            try
            {
                Console.Write($"{index}> {test.Name}: ");
                test.Invoke(test_class, no_args);
                Console.WriteLine("passed");
                passed++;
            }
            catch (Exception e)
            {
                failed++;
                // Console.Write($"{index}> {test.Name}: ");
                Console.WriteLine("failed");
                if (requestedTest.HasValue)
                    Console.WriteLine(e.InnerException?.Message ?? e.Message);
            }
        }
        //);

        print("================================");
        print("Time: " + sw.Elapsed.TotalSeconds);
        print("Result: " + (failed > 0 ? "failure" : "success")); ;
        print("--------------------------------");
        print($"Passed tests: {passed}");
        print($"Failed tests: {failed}");
    }

    static object[] no_args = new object[0];
}
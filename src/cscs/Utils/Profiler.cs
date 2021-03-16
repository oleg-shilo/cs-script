using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSScripting
{
    class Profiler
    {
        static public Stopwatch Stopwatch = new Stopwatch();

        static public string EngineContext = "";

        static Dictionary<string, Stopwatch> items = new Dictionary<string, Stopwatch>();

        public static bool has(string key) => items.ContainsKey(key);

        public static Stopwatch get(string key)
        {
            if (!items.ContainsKey(key))
                items[key] = new Stopwatch();
            return items[key];
        }

        public static void measure(string name, Action action)
        {
            var sw = new Stopwatch();
            try
            {
                sw.Start();
                action();
            }
            finally
            {
                Console.WriteLine($"{name}: {sw.Elapsed}");
            }
        }
    }
}
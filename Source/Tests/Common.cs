using System;
using System.IO;
using System.Linq;

namespace Tests
{
    public interface ICalc
    {
        int Sum(int a, int b);
    }

    public class InputData
    {
        public int Index = 0;
    }

    internal static class StringExtensions
    {
        public static string GetDirectoryName(this string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}
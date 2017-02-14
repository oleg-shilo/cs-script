using System;
using System.IO;
namespace UntrustedCode
{
    public class UntrustedClass
    {
        // Pretend to be a method checking if a number is a Fibonacci
        // but which actually attempts to read a file.
        public static bool IsFibonacci(int number)
        {
           File.ReadAllText("C:\\Temp\\file.txt");
           return false;
        }
    }
}
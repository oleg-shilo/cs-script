using System;
using csscript;
using CSScriptLib;
using static CSScriptLib.CSharpParser;
using Xunit;

namespace Misc
{
    public class GenericTests
    {
        [Fact]
        public void Replace_polyfil()
        {
            Assert.Equal("bbb123bbb456bbb7890bbb", "aa123aa456AA7890AA".Replace("aa", "bbb", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("123bbb456bbb7890", "123aa456AA7890".Replace("aa", "bbb", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("123bbb456AA7890", "123aa456AA7890".Replace("aa", "bbb", StringComparison.Ordinal));
        }
    }
}
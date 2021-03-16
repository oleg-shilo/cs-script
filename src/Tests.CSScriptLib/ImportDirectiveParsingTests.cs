using CSScriptLib;
using static CSScriptLib.CSharpParser;
using Xunit;

namespace Misc
{
    public class ImportDirectiveParsingTests
    {
        [Fact]
        public void Path_Simple()
        {
            var file = @"c:\test\child.cs";

            var info = new ImportInfo($"{file}", "parent.cs");

            Assert.Equal(file, info.file);
            Assert.False(info.preserveMain);
        }

        [Fact]
        public void Path_Simple_WithQuotation()
        {
            var file = @"c:\test\child.cs";

            var info = new ImportInfo($"{file}", "parent.cs");

            Assert.Equal(file, info.file);
            Assert.False(info.preserveMain);
        }

        [Fact]
        public void Path_WithSpaces()
        {
            var file = @"c:\test dir\child.cs";

            var info = new ImportInfo($"\"{file}\"", null);

            Assert.Equal(file, info.file);
            Assert.False(info.preserveMain);
        }

        [Fact]
        public void Path_With_MainPreserve()
        {
            var file = @"c:\test dir\child.cs";

            var info = new ImportInfo($"\"{file}\", preserve_main", null);

            Assert.Equal(file, info.file);
            Assert.True(info.preserveMain);
        }

        [Fact]
        public void Path_With_MainPreserve_CompactArgs()
        {
            var file = @"c:\test dir\child.cs";

            var info = new ImportInfo($"\"{file}\",preserve_main", null);

            Assert.Equal(file, info.file);
            Assert.True(info.preserveMain);
        }

        [Fact]
        public void Path_With_Brackets()
        {
            var file = @"c:\Program Files(x86)\test\child.cs";
            var info = new ImportInfo($"{file.EscapeDirectiveDelimiters()},preserve_main", null);

            Assert.Equal(file, info.file);
            Assert.True(info.preserveMain);
        }
    }
}
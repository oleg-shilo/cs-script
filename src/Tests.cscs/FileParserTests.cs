using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;
using System.Text.RegularExpressions;
using csscript;
using CSScripting;
using CSScriptLib;
using Xunit;

namespace Misc
{
    /// <summary>
    /// "!temp.cs" - Git: exclude 'temp.cs'
    /// "!" option does not work for cs-script as git uses a state machine for directives and effectively combines
    /// them all.
    /// While cs-script provides every directive individually and completely. Meaning that it is
    /// impossible to specify in a single //css_ directive something like "include all '.cs' except temp.cs".
    /// </summary>
    public class TestFolder
    {
        public static string root = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("test", "TestFolder", "FileParserTest").EnsureDir();

        public TestFolder()
        {
            Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                     .ToList()
                     .ForEach(File.Delete);

            void create(string dir, string file) => File.WriteAllText(root.PathJoin(dir).EnsureDir().PathJoin(file), "");

            create(@"a", "script_a.cs");
            create(@"a\b\c\d1", "script_d1.cs");
            create(@"a\b\c\d2", "script_d2.cs");
            create(@"a\b1", "script_b1.cs");
            create(@"a\b\c1\d", "script_d1_1.cs");
            create(@"a\b\c2\d", "script_d1_2.cs");

            Environment.CurrentDirectory = root;
        }
    }

    public class FileParserTests : IClassFixture<TestFolder>
    {
        void Test(Action testAction, object logData)
        {
            try
            {
                testAction();
            }
            catch (Exception e)
            {
                var logFile = TestFolder.root.PathJoin($"log{Guid.NewGuid()}.txt");

                string content = logData is string[]?
                    (logData as string[]).JoinBy("\n") :
                    logData.ToString();

                File.WriteAllText(logFile, content);

                throw new Exception($"{e.Message}\nCheck {logFile} file.", e);
            }
        }

        [Fact]
        public void LoadFiles_WildCardMissmatch()
        {
            var matches = FileParser.LocateFiles(TestFolder.root, @"a\*.csx");
            Assert.Empty(matches);
        }

        [Fact]
        public void LoadFiles_SimpleWildcardMatch()
        {
            // *.cs
            var matches = FileParser.LocateFiles(TestFolder.root.PathJoin("a"), "*.cs");
            Assert.Single(matches);
            Assert.Contains(matches, x => x.EndsWith("a.cs"));

            // dir\*.cs
            matches = FileParser.LocateFiles(TestFolder.root, @"a\*.cs");
            Assert.Single(matches);
            Assert.Contains(matches, x => x.EndsWith("a.cs"));

            // dir1\...\dirN\*.cs
            matches = FileParser.LocateFiles(TestFolder.root, @"a\b\c\d1\*.cs");
            Assert.Single(matches);
            Assert.Contains(matches, x => x.EndsWith("d1.cs"));
        }

        [Fact]
        public void LoadFiles_NameMatch()
        {
            var matches = FileParser.LocateFiles(TestFolder.root.PathJoin("a"), "script_a.cs");
            Assert.Single(matches);
            Assert.Contains(matches, x => x.EndsWith("a.cs"));
        }

        [Fact]
        public void LoadFiles_SubDir_Wildcard_AtStart()
        {
            // logs/**/debug.cs

            var matches = FileParser.LocateFiles(TestFolder.root, @"**\d\*.cs");
            this.Test(() =>
            {
                Assert.Equal(2, matches.Count());
                Assert.Contains(matches, x => x.EndsWith("d1_1.cs"));
                Assert.Contains(matches, x => x.EndsWith("d1_2.cs"));
            },
            logData: matches);
        }

        [Fact]
        public void LoadFiles_SubDir_Wildcard_InMiddle()
        {
            // logs/**/april/debug.log
            var matches = FileParser.LocateFiles(TestFolder.root, @"a\**\d\*.cs");

            this.Test(() =>
            {
                Assert.Equal(2, matches.Count());
                Assert.Contains(matches, x => x.EndsWith(@"\d\script_d1_1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"\d\script_d1_2.cs"));
            },
            logData: matches);
        }

        [Fact]
        public void LoadFiles_SubDir_Wildcard_AtEnd()
        {
            // logs/**/debug.cs

            var matches = FileParser.LocateFiles(TestFolder.root, @"a\b\**\*.cs");
            this.Test(() =>
            {
                Assert.Equal(4, matches.Count());
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d1\script_d1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d2\script_d2.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c1\d\script_d1_1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c2\d\script_d1_2.cs"));
            },
            logData: matches);
        }

        [Fact]
        public void LoadFiles_AllSubDirs_Wildcard()
        {
            var matches = FileParser.LocateFiles(TestFolder.root, @".\**\*.cs");

            this.Test(() =>
            {
                Assert.Equal(6, matches.Count());
                Assert.Contains(matches, x => x.EndsWith(@"a\script_a.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d1\script_d1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d2\script_d2.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c1\d\script_d1_1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c2\d\script_d1_2.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b1\script_b1.cs"));
            },
            logData: matches);
        }

        [Fact]
        public void LoadFiles_Everything_Wildcard()
        {
            var matches = FileParser.LocateFiles(TestFolder.root, "**");

            this.Test(() =>
            {
                Assert.Equal(6, matches.Count());
                Assert.Contains(matches, x => x.EndsWith(@"a\script_a.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d1\script_d1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d2\script_d2.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c1\d\script_d1_1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c2\d\script_d1_2.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b1\script_b1.cs"));
            },
            logData: matches);
        }

        [Fact]
        public void LoadFiles_DirName_Wildcard()
        {
            var matches = FileParser.LocateFiles(TestFolder.root, @"a\b\c*\*.cs");
            this.Test(() =>
            {
                Assert.Equal(4, matches.Count());
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d1\script_d1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c\d2\script_d2.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c1\d\script_d1_1.cs"));
                Assert.Contains(matches, x => x.EndsWith(@"a\b\c2\d\script_d1_2.cs"));
            },
            logData: matches);
        }
    }
}
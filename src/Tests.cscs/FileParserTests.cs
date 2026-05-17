using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using csscript;
using CSScripting;
using CSScripting.CodeDom;
using CSScriptLib;
using Xunit;

namespace Misc
{
    public class CSharpCompilerTests
    {
        public static string root = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("test", "TestFolder", "CSharpCompilerTests").EnsureDir();

        [Fact]
        public void IsolateProject()
        {
            var baseDir = root.EnsureDir();
            var scriptFile = baseDir.PathJoin("script.cs");
            var importedScriptFile = baseDir.PathJoin("imported", "util.cs");
            var interferingFile = baseDir.PathJoin("dummy.cs");
            var srcProj = baseDir.PathJoin("script.csproj.buid-dir-version");
            var isolatedProj = baseDir.PathJoin("script.csproj");

            importedScriptFile.EnsureFileDir();

            File.WriteAllText(scriptFile, "//css_inc imported\\util.cs;\r\nSystem.Console.WriteLine(\"Hello World!\");");
            File.WriteAllText(importedScriptFile, "public class Test {}");
            File.WriteAllText(interferingFile, "System.Console.WriteLine(\"Hello World Again!\");");
            File.WriteAllText(srcProj, $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <AssemblyName>test</AssemblyName>
                  </PropertyGroup>
                  <PropertyGroup>
                    <DefineConstants>TRACE;NETCORE;CS_SCRIPT;NET10_0_OR_GREATER;NET10</DefineConstants>
                    <UseSharedCompilation>true</UseSharedCompilation>
                  </PropertyGroup>
                      <ItemGroup>
                       <Reference Include="Newtonsoft.Json.dll">
                        <HintPath>C:\Users\user\.nuget\packages\newtonsoft.json\13.0.4\lib\net6.0\Newtonsoft.Json.dll</HintPath>
                      </Reference>
                      <Reference Include="RestSharp.dll">
                        <HintPath>C:\Users\user\.nuget\packages\restsharp\112.1.0\lib\net8.0\RestSharp.dll</HintPath>
                      </Reference>
                      <Reference Include="cscs.dll">
                        <HintPath>C:\Users\user\.dotnet\tools\.store\cs-script.cli\4.14.5\cs-script.cli\4.14.5\tools\net10.0\any\cscs.dll</HintPath>
                      </Reference>
                    </ItemGroup>
                    <ItemGroup>
                      <Compile Include="{scriptFile}" Link="generate.cs" />
                      <Compile Include="{importedScriptFile}" Link="util.cs" />
                      <Compile Include="C:\ProgramData\cs-script\inc\global-usings.cs" Link="global-usings.cs" />
                  </ItemGroup>
                </Project>
                """);

            var isolatedProject = VSExtensions.IsolateProject(srcProj, scriptFile.GetDirName());

            var projXml = XDocument.Load(isolatedProject);

            bool OnlyOne(string element, (string attribute, string value)[] expectedAttributes) =>
                projXml.Root.Descendants(element).Count(x => expectedAttributes.All(attr => x.Attribute(attr.attribute)?.Value == attr.value)) == 1;

            Assert.Equal(scriptFile.ChangeExtension(".csproj"), isolatedProject);

            // nuget asm refs converted to package refs
            Assert.True(OnlyOne("PackageReference", [("Include", "newtonsoft.json"), ("Version", "13.0.4")]));
            Assert.True(OnlyOne("PackageReference", [("Include", "restsharp"), ("Version", "112.1.0")]));
            Assert.True(OnlyOne("PackageReference", [("Include", "cs-script"), ("Version", "*")]));

            // script.cs and utils.s files included, but dummy.cs is not
            Assert.True(OnlyOne("Compile", [("Remove", "dummy.cs")]));
            Assert.True(OnlyOne("Compile", [("Include", @"C:\ProgramData\cs-script\inc\global-usings.cs"), ("Link", "global-usings.cs")]));
            Assert.False(OnlyOne("Compile", [("Include", @"script.cs")]));
            Assert.False(OnlyOne("Compile", [("Include", @"imported\util.cs")]));
        }
    }

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
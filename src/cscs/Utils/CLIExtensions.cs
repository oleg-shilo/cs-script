using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using csscript;
using CSScripting;
using CSScripting.CodeDom;

/// <summary>
/// Credit to https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
/// </summary>
public static class VSExtensions
{
    public static string IsolateProject(string srcProjectFile, string destDir)
    {
        var projXml = XDocument.Load(srcProjectFile);

        var nugetPackages = projXml
            .Descendants("Reference")
            .Where(x => x.Element("HintPath")?.Value?.Contains(".nuget".PathJoin("packages")) == true)
            .ToList();

        var scriptEngineReference = projXml
            .Descendants("Reference")
            .Where(x => x.Attribute("Include")?.Value?.IsOneOf("cscs.dll", "wscs.dll") == true)
            .ToList();

        var sources = projXml
            .Descendants("Compile")
            .Where(x => x.Attribute("Include")?.Value != null)
            .ToList();

        var sourceFiles = sources.Select(x => x.Attribute("Include").Value).ToList();
        var scriptFile = sourceFiles.First();
        var isolatedProjectFile = destDir.PathJoin(scriptFile.GetFileName().ChangeExtension(".csproj"));

        // replace referencing nuget assemblies with PackageReference
        if (nugetPackages.Any())
        {
            var itemGroup = new XElement("ItemGroup");

            foreach (var nugetRef in nugetPackages)
            {
                var hintPath = nugetRef.Element("HintPath").Value;
                var packageInfo = hintPath.Split(Path.DirectorySeparatorChar).SkipWhile(x => x != "packages").Skip(1).Take(2).ToArray();

                itemGroup.Add(new XElement("PackageReference",
                                  new XAttribute("Include", packageInfo[0]),
                                  new XAttribute("Version", packageInfo[1])));
                nugetRef.Remove();
            }

            if (scriptEngineReference.Any())
            {
                itemGroup.Add(new XElement("PackageReference",
                                  new XAttribute("Include", "cs-script"),
                                  new XAttribute("Version", "*")));

                scriptEngineReference.ForEach(x => x.Remove());
            }

            projXml.Root.Add(itemGroup);
        }

        // replace any source link includes that point to the script dir with the direct includes (not links)
        if (sources.Any())
        {
            foreach (var source in sources)
            {
                var path = source.Attribute("Include");
                var linkName = source.Attribute("Link");

                if (path != null && path.Value.StartsWith(destDir))
                {
                    // will be picked by dotnet.exe anyway simply because it is in the nested folder(s)
                    // this is just to clean up the project file and avoid confusion
                    path.Parent.Remove();
                }
            }
        }

        var sourcesToBeExcluded = Directory.GetFiles(destDir, "*.cs", SearchOption.AllDirectories)
            .Except(sourceFiles)
            .Select(f => f.Substring(destDir.Length).TrimStart(Path.DirectorySeparatorChar))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (sourcesToBeExcluded.Any())
        {
            var itemGroup = new XElement("ItemGroup");
            foreach (var source in sourcesToBeExcluded)
            {
                itemGroup.Add(new XElement("Compile", new XAttribute("Remove", source)));
            }
            projXml.Root.Add(itemGroup);
        }

        projXml.Save(isolatedProjectFile);
        return isolatedProjectFile;
    }
}

public static class CLIExtensions
{
    public static string TrimMatchingQuotes(this string input, char quote)
    {
        if (input.Length >= 2)
        {
            //"-sconfig:My Script.cs.config"
            if (input.First() == quote && input.Last() == quote)
            {
                return input.Substring(1, input.Length - 2);
            }
            //-sconfig:"My Script.cs.config"
            else if (input.Last() == quote)
            {
                var firstQuote = input.IndexOf(quote);
                if (firstQuote != input.Length - 1) //not the last one
                    return input.Substring(0, firstQuote) + input.Substring(firstQuote + 1, input.Length - 2 - firstQuote);
            }
        }
        return input;
    }

    public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
    {
        int nextPiece = 0;

        for (int c = 0; c < str.Length; c++)
        {
            if (controller(str[c]))
            {
                yield return str.Substring(nextPiece, c - nextPiece);
                nextPiece = c + 1;
            }
        }

        yield return str[nextPiece..];
    }

    // [Obsolete]
    // public static string[] Split(this string str, params string[] separators) =>
    //     str.Split(separators, str.Length, StringSplitOptions.None);

    // [Obsolete]
    // public static string[] Split(this string str, string[] separators, int count) =>
    //     str.Split(separators, count, StringSplitOptions.None);

    public static bool IsCustomCommandScript(this string scriptFile)
        => scriptFile.GetFileName().EndsWith("-run.cs") &&
           (scriptFile.StartsWith(Runtime.CustomCommandsDir) || scriptFile.StartsWith(Runtime.DefaultCommandsDir ?? "unknown-dir"));

    public static string NormalizeNewLines(this string str) =>// too simplistic though adequate
        str.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

    public static IEnumerable<XElement> FindDescendants(this XElement element, string localName) =>
        element.Descendants().Where(x => x.Name.LocalName == localName);

    public static bool StartsWith(this string text, string pattern, bool ignoreCase) =>
        text.StartsWith(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : default(StringComparison));

    public static string TakeMax(this string text, int maxCharacters) =>
        text.Substring(0, Math.Min(100, text.Length));

    public static string GetTargetPlatform(this CompilerParameters compilerParams)
    {
        var platform = compilerParams.CompilerOptions?
            .Split(' ')
            .FirstOrDefault(x => x.StartsWith("/platform:"))
            ?.Split(':')
            ?.Last();
        return platform;
    }

    public static string ArgValue(this string[] arguments, string prefix) =>
        (arguments.FirstOrDefault(x => x.StartsWith(prefix + ":"))?.Substring(prefix.Length + 1).TrimMatchingQuotes('"'))
        ?? arguments.Where(x => x == prefix).Select(x => "").FirstOrDefault();

    // [Obsolete]
    // public static string ArgValue(this string argument, string prefix) =>
    //     argument.StartsWith(prefix + ":") == false ? null : argument.Substring(prefix.Length + 1)
    //             .TrimMatchingQuotes('"');

    public static string[] SplitCommandLine(this string commandLine)
    {
        bool inQuotes = false;
        bool isEscaping = false;

        return commandLine.Split(c =>
                                 {
                                     if (c == '\\' && !isEscaping) { isEscaping = true; return false; }

                                     if (c == '\"' && !isEscaping)
                                         inQuotes = !inQuotes;

                                     isEscaping = false;

                                     return !inQuotes && c.IsWhiteSpace()/*c == ' '*/;
                                 })
                          .Select(arg => arg.Trim().TrimMatchingQuotes('\"').Replace("\\\"", "\""))
                          .Where(arg => !string.IsNullOrEmpty(arg))
                          .ToArray();
    }
}
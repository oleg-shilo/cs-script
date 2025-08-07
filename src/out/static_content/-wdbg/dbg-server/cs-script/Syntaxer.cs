using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace wdbg.cs_script
{
    public class Syntaxer
    {
        // Ports:
        // 18000 - Sublime Text 3
        // 18001 - Notepad++
        // 18002 - VSCode CodeMap
        // 18004 - WDBG Intellisense

        public static string syntaxerDir;

        public static Func<string> cscs_asm = () => @"C:\Users\oleg\.dotnet\tools\.store\cs-script.cli\4.9.8\cs-script.cli\4.9.8\tools\net9.0\any\cscs.dll";
        public static Func<string> syntaxer_asm = () => @"C:\Users\oleg\.dotnet\tools\.store\cs-syntaxer\3.2.4\cs-syntaxer\3.2.4\tools\net9.0\any\syntaxer.dll";
        public static Func<int> syntaxer_port = () => 18004;

        internal static string CscsFile => cscs_asm();
        internal static string SyntaxerFile => syntaxer_asm();
        internal static int SyntaxerPort => syntaxer_port();

        static int timeout = 60000;
        static int procId = Process.GetCurrentProcess().Id;
        static Process syntaxer;

        public static void StartServer(bool onlyIfNotRunning)
        {
            Task.Factory.StartNew(() =>
            {
                if (onlyIfNotRunning && IsRunning())
                    return;

                HandeErrors(() =>
                {
                    // Debug.Assert(false);

                    bool hidden = true;
                    if (hidden)
                    {
                        try
                        {
                            syntaxer?.Kill();
                        }
                        catch { }
                        finally { try { syntaxer?.Dispose(); } catch { } }

                        syntaxer = new Process();

                        syntaxer.StartInfo.UseShellExecute = false;
                        syntaxer.StartInfo.CreateNoWindow = true;
                        syntaxer.StartInfo.RedirectStandardOutput = true;
                        syntaxer.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                        syntaxer.StartInfo.FileName = "dotnet";
                        syntaxer.StartInfo.Arguments = $"\"{SyntaxerFile}\" -listen -port:{SyntaxerPort} -timeout:{timeout}";

                        if (CscsFile.HasText())
                            syntaxer.StartInfo.Arguments += $" \"-cscs_path:{CscsFile}\"";

                        syntaxer.Start();
                    }
                    else
                    {
                        var args = $"-listen -port:{SyntaxerPort} -timeout:{timeout}" + (CscsFile.HasText() ? "" : $" \"-cscs_path:{CscsFile}\"");
                        syntaxer = Process.Start(SyntaxerFile, args);
                    }
                });

                for (int i = 0; i < 10; i++)
                {
                    var response = Send($"-op:ping");
                    if (response != null)
                        break;
                    else
                        Thread.Sleep(1000);
                }
            });
        }

        public static bool IsRunning()
        {
            var response = Send($"-client:{procId}");
            return (response != null);
        }

        public static void Exit()
        {
            Send("-exit");
        }

        ////////////////////////////////////////////////

        public static string GetCompletions(string editorText, string file, int location)
        {
            return editorText.WithTempCopy(file,
                tempFile => SendSyntaxCommand(tempFile, location, "completion"));//.ToCompletionData();
        }

        public static string FindReferences(string editorText, string file, int location)
        {
            return editorText.WithTempCopy(file,
                tempFile => SendSyntaxCommand(tempFile, location, "references"));
        }

        public static string GetMapOf(string editorText, string file)
        {
            return editorText.WithTempCopy(file,
                tempFile => SendSyntaxCommand(tempFile, "codemap"));
        }

        public static string Resolve(string editorText, string file, int location)
        {
            return editorText.WithTempCopy(file,
                tempFile => SendSyntaxCommand(tempFile, location, "resolve"));
        }

        public static string GetPossibleNamespaces(string editorText, string file, string word)
        {
            return editorText.WithTempCopy(file,
                tempFile => SendSyntaxCommand(tempFile, $"suggest_usings:{word}"));
        }

        public static string GetTooltip(string editorText, string file, int location)
        {
            var response = editorText.WithTempCopy(file,
                tempFile => SendCommand($"-client:{procId}\n" +
                                        $"-op:tooltip:\n" +
                                        $"-script:{tempFile}\n" +
                                        $"-pos:{location}"));
            return response;
        }

        public static string GetMemberInfo(string editorText, string file, int location, bool collapseOverloads, out int methodStartPosTemp)
        {
            var overloads = (collapseOverloads ? "-collapseOverloads" : "");

            methodStartPosTemp = 0;
            var response = editorText.WithTempCopy(file,
                                                   tempFile => SendCommand($"-client:{procId}\n" +
                                                                     $"-op:memberinfo\n" +
                                                                     $"-script:{tempFile}\n" +
                                                                     $"-pos:{location}\n" +
                                                                     $"-rich\n" +
                                                                     $"{overloads}"));
            return response;
        }

        public static string Format(string editorText, string file, ref int caretPosition)
        {
            int location = caretPosition;

            try
            {
                string response = editorText.WithTempCopy(null,
                                                          tempFile => SendSyntaxCommand(tempFile, location, "format"));

                if (response != null && !response.StartsWith("<error>"))
                {
                    // response: $"{caretPos}\n{formattedCode}"
                    int pos = response.IndexOf("\n");
                    if (pos != -1)
                    {
                        caretPosition = int.Parse(response.Substring(0, pos));

                        var result = response.Substring(pos + 1);
                        return result;
                    }
                }
            }
            catch { }
            return null;
        }

        ////////////////////////////////////////////////

        static string SendSyntaxCommand(string file, string operation, params string[] extraArgs)
        {
            if (extraArgs.Any())
                return SendCommand($"-client:{procId}\n-op:{operation}\n-script:{file}\n-rich\n" + string.Join("\n", extraArgs));
            else
                return SendCommand($"-client:{procId}\n-op:{operation}\n-script:{file}\n-rich");
        }

        static string SendSyntaxCommand(string file, int location, string operation)
        {
            return SendCommand($"-client:{procId}\n-op:{operation}\n-script:{file}\n-pos:{location}");
        }

        static string SendCommand(string command)
        {
            var response = Send(command);
            if (response == null) StartServer(onlyIfNotRunning: false);
            return response;
        }

        static string Send(string command)
        {
            try
            {
                using (var clientSocket = new TcpClient())
                {
                    clientSocket.Connect(IPAddress.Loopback, SyntaxerPort);
                    clientSocket.ReceiveTimeout = 10000; // 10 seconds
                    clientSocket.WriteAllText(command);
                    return clientSocket.ReadAllText();
                }
                ;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // Handle timeout explicitly
                Console.WriteLine($"Timeout communicating with syntax server: {ex.Message}");
                return null;
            }
            catch (Exception)
            {
                // Optional: Log other exceptions
                // Console.WriteLine($"Error communicating with syntax server: {ex.Message}");
                return null;
            }
        }

        public static void HandeErrors(Action action, string logContext = null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (logContext != null)
                    Console.WriteLine(logContext + $": {e}");
            }
        }
    }

    public static class SyntaxerParser
    {
        public static string WithTempCopy(this string editorText, string originalFile, Func<string, string> action)
        {
            Func<string, string> fixTempFileInsertions = null;

            string tempFile;
            if (originalFile == null)
            {
                tempFile = Path.GetTempFileName();
            }
            else
            {
                tempFile = Path.GetTempPath().PathJoin("Roslyn.Intellisense",
                                                       "sources",
                                                       Guid.NewGuid() + Path.GetExtension(originalFile));

                Path.GetDirectoryName(tempFile).EnsureDir();

                var originalName = Path.GetFileName(originalFile);
                var tempName = Path.GetFileName(tempFile);

                fixTempFileInsertions = txt =>
                {
                    if (!string.IsNullOrEmpty(txt))
                        return txt.Replace(tempName, originalName)  // compiler may report errors by file name only
                                  .Replace(tempFile, originalFile); // handle compiler errors by full file path
                    else
                        return txt;
                };
            }

            try
            {
                var code = editorText;

                if (originalFile != null)
                    code = "//css_syntaxer source:" + originalFile + Environment.NewLine + code;

                File.WriteAllText(tempFile, code);

                string response = action(tempFile);

                // LogAnyErrors(response);

                if (fixTempFileInsertions != null)
                    response = fixTempFileInsertions(response);

                return response == "<null>" ? null : response;
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        //    public static CompletionType ToCompletionType(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //        {
        //            string value = data.Replace("event", "_event").Replace("namespace", "_namespace");

        //            CompletionType type;
        //            if (!Enum.TryParse(value, out type))
        //                type = CompletionType.unresolved;

        //            return type;
        //        }
        //        return CompletionType.unresolved;
        //    }

        //    public static CodeMapItem[] ToCodeMapItems(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            try
        //            {
        //                return data.GetSerializedLines()
        //                           .Select(CodeMapItem.Deserialize)
        //                           .ToArray();
        //            }
        //            catch { }
        //        return new CodeMapItem[0];
        //    }

        //    public static Intellisense.Common.MemberInfoData[] ToMemberInfoData(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            try
        //            {
        //                return data.GetSerializedLines()
        //                           .Select(Intellisense.Common.MemberInfoData.Deserialize)
        //                           .ToArray();
        //            }
        //            catch { }

        //        return new Intellisense.Common.MemberInfoData[0];
        //    }

        //    public static void LogAnyErrors(string data)
        //    {
        //        if (data?.StartsWith("<error>") == true)
        //        {
        //            string logFile = Path.Combine(PluginEnv.LogDir, "last_syntaxer_error.txt");
        //            File.WriteAllText(logFile, data);
        //        }
        //    }

        //    public static Intellisense.Common.TypeInfo[] ToTypeInfos(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            try
        //            {
        //                return data.GetSerializedLines()
        //                           .Select(Intellisense.Common.TypeInfo.Deserialize)
        //                           .ToArray();
        //            }
        //            catch { }
        //        return new Intellisense.Common.TypeInfo[0];
        //    }

        //    public static IEnumerable<ICompletionData> ToCompletionData(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            return data.GetSerializedLines()
        //                       .Select(x =>
        //                       {
        //                           var parts = x.Split(new[] { '|', '\t' });

        //                           return new Intellisense.Common.CompletionData
        //                           {
        //                               DisplayText = parts[0],
        //                               CompletionText = parts[2],
        //                               CompletionType = parts[1].ToCompletionType(),
        //                           };
        //                       });

        //        return new List<ICompletionData>();
        //    }

        //    public static string[] ToResultStrings(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            return data.GetSerializedLines();

        //        return new string[0];
        //    }

        //    public static DomRegion ToDomRegion(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            try { return DomRegion.Deserialize(data); }
        //            catch { }
        //        return DomRegion.Empty;
        //    }

        //    public static string ToResultString(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            try { return data; }
        //            catch { }
        //        return null;
        //    }

        //    public static string[] ToReferences(this string data)
        //    {
        //        if (data != null && !data.StartsWith("<error>"))
        //            try { return data.Split('\n'); } // references are simple types and syntaxer never returns them as "rich_serialization" serialized
        //            catch { }
        //        return new string[0];
        //    }
        //}
    }
}
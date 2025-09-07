//css_ng:csc 
//css_inc ..\out\ci\cmd.cs

using System.IO;
using System.Net;
using System;
using System.Linq;

ServicePointManager.Expect100Continue = true;
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

var url = "https://github.com/oleg-shilo/cs-script/releases/download/v4.11.1.0/cs-script.win.v4.11.1.0.7z";

var installScript = @"tools\chocolateyInstall.ps1";

var checksum = calcChecksum(url);
// var checksum = "E1809AD6433A91B2FF4803E7F4B15AE0FA88905A28949EAC5590F7D9FD9BE9C3";
// Console.WriteLine(checksum);

var code = File.ReadAllText(installScript + ".template")
               .Replace("$url = ???", "$url = '" + url + "'")
               .Replace("$checksum = ???", "$checksum = '" + checksum + "'");

File.WriteAllText(installScript, code);

Console.WriteLine("--------------");
Console.WriteLine(code.Split('\n').FirstOrDefault(x => x.Trim().StartsWith("$url =")));
Console.WriteLine("--------------");
Console.WriteLine();
Console.WriteLine("Done...");

string calcChecksum(string url)
{
    var file = "cs-script.7z";
    cmd.download(url, file, (step, total) => Console.Write("\r{0}%\r", (int)(step * 100.0 / total)));
    Console.WriteLine();

    var checksum = cmd.run(@"C:\ProgramData\chocolatey\tools\checksum.exe", "-t sha256 -f \"" + file + "\"", echo: false).Trim();
    return checksum;
}
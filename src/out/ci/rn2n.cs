using System.IO;

var content = File.ReadAllText(args[0]);
File.WriteAllText(args[0], content.Replace("\r\n", "\n"));
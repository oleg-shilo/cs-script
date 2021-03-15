using System.Linq;
using System.IO;
using System;

void move(string mask)
{
    var package = Directory.GetFiles(@"..\bin\Release", mask)
                           .OrderByDescending(x => x)
                           .FirstOrDefault();
    File.Copy(package, Path.GetFileName(package), true);
    Console.WriteLine(package);
}

Directory
    .GetFiles(@".\", "*.*nupkg")
    .ToList()
    .ForEach(x => File.Delete(x));

move("CS-Script.*.nupkg");
move("CS-Script.*.snupkg");
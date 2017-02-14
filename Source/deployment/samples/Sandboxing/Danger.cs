using System;
using System.IO;

namespace Danger
{
   public class ClassA
   {
		static public void SayHello()
		{
			using (FileStream f = File.Open("somefile.txt", FileMode.OpenOrCreate))
				Console.WriteLine("  Ok");
		}
   }
}
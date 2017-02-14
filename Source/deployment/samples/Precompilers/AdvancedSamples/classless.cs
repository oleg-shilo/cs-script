//css_pc Precompilers/auto_class

int Main(string[] args)
{
    Console.WriteLine("args start ----");
	foreach (var arg in args)
		Console.WriteLine(arg);
	Console.WriteLine("args end ----");

	Console.WriteLine("\nStrings...");
	foreach (var str in new string[]{"aaa", "bbb", "ccc"})
		Console.WriteLine("  "+str);

    return 0;
}


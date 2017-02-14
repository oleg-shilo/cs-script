#!/usr/local/bin/mono /utils/cs-script/cscs.exe
//css_pc Precompilers/hashdef
using System;

#define PRINT Console.WriteLine
#define DECLARE_APARTMENT [STAThread]
#define GRITTING "Hello World!"

class Script
{
	DECLARE_APARTMENT
	static public void Main()
	{
		PRINT(GRITTING);
	}
}


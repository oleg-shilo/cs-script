using System;
using System.Xml;

//css_pre wsdl(http://localhost/hello/hello.asmx?WSDL, HelloService);
//css_imp HelloService;

class Script
{
	static void Main(string[] args)
	{
		Console.WriteLine(new HelloWorld().SayHello());
	}
}


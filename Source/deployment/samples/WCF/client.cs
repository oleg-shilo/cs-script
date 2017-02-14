//css_prescript wsdl(http://localhost/hello?wsdl, HelloService, /new);
//css_include HelloService;
using System;
using System.Xml;

namespace HelloClient
{
	class Program
	{
		static void Main(string[] args)
		{
            HelloService p = new HelloService();
			Console.WriteLine(p.SayHello());
		}
	}
}

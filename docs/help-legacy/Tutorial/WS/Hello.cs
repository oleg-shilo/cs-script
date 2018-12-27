using System;
using System.Web;
using System.Web.Services;

public class HelloWorld : System.Web.Services.WebService
{
	[WebMethod]
	public string SayHello()
	{
		return "Hello World (non-VS WebService)";
	}
}


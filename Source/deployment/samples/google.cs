using System;
using System.Net;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;

//css_import credentials;

namespace Scripting
{
	//code sample is taken from  Brian Delahunty article   "Introduction to Mono - Your first Mono app" (http://www.codeproject.com/cpnet/introtomono1.asp)
   class GoogleSearch
   {
	  const string usage =  "Usage: cscscript google...\nPerforms online search by using the Google search engine.\n"+
							"The original file was provided by Brian Delahunty article \"Introduction to Mono - Your first Mono app\" (http://www.codeproject.com/cpnet/introtomono1.asp).\n";

	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine(usage);
		}
		else
		{
			string userName = Environment.UserName, password = "";
			if (AuthenticationForm.GetCredentials(ref userName, ref password, "Proxy Authentication"))
				GlobalProxySelection.Select.Credentials = new NetworkCredential(userName, password);
				
			 Console.Write("Please enter string to search google for: ");
			 string searchString = HttpUtility.UrlEncode(Console.ReadLine());
			 
			 Console.WriteLine();
			 Console.Write("Please wait...\r");
			
			 // Query google.
			 WebClient webClient = new WebClient();
			 byte[] response = webClient.DownloadData("http://www.google.com/search?&num=5&q=" + searchString);
	
			 // Check response for results
			 string regex  = "class=r><a\\shref=\"?(?<URL>[^\">]*)[^>]*>(?<Name>.*?)</a>";
			 MatchCollection matches = Regex.Matches(Encoding.ASCII.GetString(response), regex);
	
			 // Output results
			 Console.WriteLine("===== Results =====");
			 if(matches.Count > 0)
			 {
				foreach(Match match in matches)
				{
				   Console.WriteLine(HttpUtility.HtmlDecode(match.Groups["Name"].Value) + " - " + match.Groups["URL"].Value);
				}
			 }
			 else
			 {
				Console.WriteLine("0 results found");
			 }
		 }
	  }
   }
}

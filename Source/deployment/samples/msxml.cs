//This is a ported VBScript XML example from the MSDN article "Working with XML Document Parts"
//http://msdn.microsoft.com/library/default.asp?url=/library/en-us/xmlsdk/html/89121802-adeb-4c14-9ed9-f4cd63c6619c.asp

using System;
using System.Windows.Forms;
using Msxml2;

//css_prescript com(Msxml2.DOMDocument, Msxml2);

class Script
{
	const string usage = "Usage: cscscript msxml...\nThis is the example of using a COM server (MS XML Parser) from the C# script.\n";
	[STAThread]
	static public void Main(string[] args)
	{
		if (args.Length == 0 || (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			try
			{
				DOMDocumentClass xmlDoc = new DOMDocumentClass();
				IXMLDOMElement rootElement = xmlDoc.createElement("memo");
				IXMLDOMAttribute memoAttribute = xmlDoc.createAttribute("author");
				IXMLDOMText memoAttributeText = xmlDoc.createTextNode("Pat Coleman");
				IXMLDOMElement toElement = xmlDoc.createElement("to");
				IXMLDOMText toElementText = xmlDoc.createTextNode("Carole Poland");
				xmlDoc.appendChild(rootElement);
				memoAttribute.appendChild(memoAttributeText);
				rootElement.setAttributeNode(memoAttribute);
				rootElement.appendChild(toElement);
				toElement.appendChild(toElementText);
				xmlDoc.save("memo.xml");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}


using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Xml;
using System.Diagnostics;

class Script
{
	static string htmlDir = @"../";
	static string helpTitle = "";
	static string firstTopicFile = "";

	const string usage = "Usage: csc update [/t:title] [/c:<HH content file>] [/nw]...\n"+
						 "Converts HTML Help Workshop project (.hhp) to HTML Help.\n"+
						 "Project has to be located one level above in the directory tree with respect to location of the update.cs script.\n"+
						 "title - optional title for the Oline Help page\n"+
						 "/c - optional .hhc (content) file to parse. By default the very first .hhc file found is used.\n"+
						 "/nw - 'no wait' mode";

	[STAThread]
	static public void Main(string[] args)
	{
		if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
		{
			Console.WriteLine(usage);
		}
		else
		{
			bool wait = true;
			try
			{
				//prepare input data
				string contentFile = "";
				foreach (string arg in args)
				{
					if (arg.StartsWith("/t"))
						helpTitle = arg.Substring(3);
					else if (arg.StartsWith("/c"))
						contentFile = arg.Substring(3);
					else if (arg.ToLower() == "/nw")
						wait = false;
				}

				if (contentFile == "")
				{
					string[] files = Directory.GetFiles(htmlDir, "*.hhc");

					if (files.Length == 0)
					{
						Console.WriteLine("Cannot find any *.hhc file");
						return;
					}
					contentFile = files[0];
				}
				
				if (helpTitle == "")
					helpTitle = Path.GetFileNameWithoutExtension(contentFile)+" Online Help";

				//parse content file
				Console.WriteLine("Parsing "+contentFile+"...");
				ArrayList items = ParseHHC(contentFile);
								
				//prepare content.html
				using (StreamWriter sw = new StreamWriter("contents.html"))
				{
					sw.Write(contentTemplate.Replace("HHP2HTML_TREE", ComposeHTMLTree(items)));
				}

				//prepare index.html
				using (StreamWriter sw = new StreamWriter("index.html"))
				{
					sw.Write(indexTemplate.Replace("HHP2HTML_DEFAULT_TOPIC", firstTopicFile).Replace("HHP2HTML_TITLE", helpTitle));
				}

				//prepare indexNoFrame.html
				using (StreamWriter sw = new StreamWriter("indexNoFrame.html"))
				{
					sw.Write(indexNoFrameTemplate.Replace("HHP2HTML_TITLE", helpTitle).Replace("HHP2HTML_NO_FRAME_TREE", ComposeHTMLNoFrameTree(items)));
				}

				Console.WriteLine("'"+helpTitle+"' has been prepared: "+Path.GetFullPath("index.html")+"\n");
				Process.Start(Path.GetFullPath("index.html"));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			if (wait)
			{
				Console.WriteLine("Press Enter to continue...");
				Console.ReadLine();
			}
		}
	}

	static string Multiply(string str, int times)
	{
		string retval = "";
		for(int i = 0; i < times; i++)
		{	
			retval += str;
		}
		return retval;
	}

	static string ComposeHTMLTree(ArrayList items)
	{
        string htmlItemLink = "\t<img src=\"treenodeplus.gif\" class=\"treeLinkImage\" onclick=\"expandCollapse(this.parentNode)\" />"+
							  "\t<a href=\"HHP2HTML_FILE\" target=\"main\" class=\"treeUnselected\" onclick=\"clickAnchor(this)\">HHP2HTML_NAME</a>\n";
		string htmlItem		= "\t<img src=\"treenodedot.gif\" class=\"treeNoLinkImage\" />"+
								"<a href=\"HHP2HTML_FILE\" target=\"main\" class=\"treeUnselected\" onclick=\"clickAnchor(this)\">HHP2HTML_NAME</a>\n";
		StringBuilder sb = new StringBuilder();

		for(int i = 0; i < items.Count; i++)
		{
			HelpItem item = (HelpItem)items[i];
			
			sb.Append(Multiply("\t", item.depth) + "<div class=\"treeNode\">\n");
			if (items.Count > (i+1)) //therea are some more items
			{
				HelpItem nextItem = (HelpItem)items[i+1];
				if (nextItem.depth > item.depth) //has a child
				{
					sb.Append(Multiply("\t", item.depth) + htmlItemLink.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
					sb.Append(Multiply("\t", item.depth) + "\t<div class=\"treeSubnodesHidden\">\n");
				}
				else
				{
					sb.Append(Multiply("\t", item.depth) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
					sb.Append(Multiply("\t", item.depth) + "</div>\n");
				}

				if (nextItem.depth < item.depth) //is a last child
				{
					sb.Append(Multiply("\t", item.depth-1) + "</div>\n");
					sb.Append(Multiply("\t", item.depth-1) + "</div>\n");
				}
			}
			else
			{
				sb.Append(Multiply("\t", item.depth) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
				sb.Append(Multiply("\t", item.depth) + "</div>\n");
				sb.Append(Multiply("\t", item.depth-1) + "</div>\n");
				sb.Append(Multiply("\t", item.depth-1) + "</div>\n");
			}
		}
		return sb.ToString();
	}
	static string ComposeHTMLNoFrameTree(ArrayList items)
	{
		//string htmlItemLink = "\t<img src=\"treenodeplus.gif\" class=\"treeLinkImage\" onclick=\"expandCollapse(this.parentNode)\" />"+
		//	"\t<a href=\"HHP2HTML_FILE\" target=\"main\" class=\"treeUnselected\" onclick=\"clickAnchor(this)\">HHP2HTML_NAME</a>\n";
		//string htmlItem		= "\t<img src=\"treenodedot.gif\" class=\"treeNoLinkImage\" />"+
		//	"<a href=\"HHP2HTML_FILE\" target=\"main\" class=\"treeUnselected\" onclick=\"clickAnchor(this)\">HHP2HTML_NAME</a>\n";

		string htmlItem = "<a href=\"HHP2HTML_FILE\">HHP2HTML_NAME</a><br>";


		StringBuilder sb = new StringBuilder();

		for(int i = 0; i < items.Count; i++)
		{
			HelpItem item = (HelpItem)items[i];
			
			//sb.Append(Multiply("&nbsp;", item.depth*4) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
			if (items.Count > (i+1)) //therea are some more items
			{
				HelpItem nextItem = (HelpItem)items[i+1];
				if (nextItem.depth > item.depth) //has a child
				{
					sb.Append(Multiply("&nbsp;", item.depth*4) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
				}
				else
				{
					sb.Append(Multiply("&nbsp;", item.depth*4) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
				}

				if (nextItem.depth < item.depth) //is a last child
				{
					//sb.Append(Multiply("&nbsp;", item.depth*4) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
				}
			}
			else
			{
				sb.Append(Multiply("&nbsp;", item.depth*4) + htmlItem.Replace("HHP2HTML_NAME", item.name).Replace("HHP2HTML_FILE", item.file));
			}
		}
		return sb.ToString();
	}

	class HelpItem
	{
		public HelpItem(string name, string file, int depth)
		{
			this.name = name;
			this.file = Path.Combine(htmlDir, Path.GetFileName(file));
			this.depth = depth;
		}
		public string name = "";
		public string file = "";
		public int depth = 0;
	}

	static HelpItem ParseHHCItem(string data, int depth)
	{
		string tag = "<param name=\"Name\" value=\"";
		int posStart = data.IndexOf(tag);
		int posEnd = data.IndexOf("\"", posStart + tag.Length);
		string name = data.Substring(posStart + tag.Length, posEnd - (posStart + tag.Length));
		
		tag = "<param name=\"Local\" value=\"";
		posStart = data.IndexOf(tag);
		posEnd = data.IndexOf("\"", posStart + tag.Length);
		string file = data.Substring(posStart + tag.Length, posEnd- (posStart + tag.Length));

		return new HelpItem(name, file, depth);
	}

	static public ArrayList ParseHHC(string file) 		
	{	
		string text = "";
		using (StreamReader sr = new StreamReader(file))
		{
			text = sr.ReadToEnd();
		}

		string startTag = "<OBJECT type=\"text/sitemap\">";
		string endTag = "</OBJECT>";

		int itemStart = -1, itemEnd = -1, currentSearchPos = text.IndexOf(startTag), currDepth = 0;


		ArrayList items = new ArrayList();
			
		while (true)
		{
			itemStart = text.IndexOf(startTag, currentSearchPos);
			if (itemStart == -1)
				break;
			itemEnd = text.IndexOf(endTag, currentSearchPos);
			
			string itemData = text.Substring(itemStart + startTag.Length, itemEnd - (itemStart + startTag.Length));
			string interItemData = text.Substring(currentSearchPos, itemStart - currentSearchPos); 

			if (interItemData.IndexOf("<UL>") != -1)
				currDepth++;
			if (interItemData.IndexOf("</UL>") != -1)
				currDepth--;

			HelpItem item = ParseHHCItem(itemData, currDepth);
			
			if (item.file == @"..\CSScript.html")
			{
				currentSearchPos = itemEnd + endTag.Length;
				continue;
			}

			if (firstTopicFile == "")
			{
				firstTopicFile = item.file;
			}
			items.Add(item);

			for (int i = 0; i < currDepth; i++)
				Console.Write("\t");
			Console.WriteLine(item.name);

			currentSearchPos = itemEnd + endTag.Length;
		}
		return items;
	}

	static string indexTemplate = 
						"<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Frameset//EN\">\n"+ 
						"  <html>\n"+
						"    <head>\n"+
						"      <meta name=\"Robots\" content=\"noindex\">\n"+
						"      <title>HHP2HTML_TITLE</title>\n"+
						"      <script language=\"JavaScript\">\n"+
						"        // ensure this page is not loaded inside another frame\n"+
						"      if (top.location != self.location)\n"+
						"      {\n"+
						"        top.location = self.location;\n"+
						"      }\n"+
						"      </script>\n"+
						"    </head>\n"+
						"    <frameset cols=\"250,*\" framespacing=\"6\" bordercolor=\"#6699CC\">\n"+
						"      <frame name=\"contents\" src=\"contents.html\" frameborder=\"0\" scrolling=\"no\">\n"+
						"      <frame name=\"main\" src=\"HHP2HTML_DEFAULT_TOPIC\" frameborder=\"1\">\n"+
						"      <noframes>\n"+
						"        <p>This page requires frames, but your browser does not support them.</p>\n"+
						"      </noframes>\n"+
						"   </frameset>\n"+
						"</html>";

		static string indexNoFrameTemplate = 
						"<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">\n"+
						"<html>\n"+
						"<head>\n"+
						"<body>\n"+
						"<div style=\"margin: 6px 4px 8px 2px; font-family: verdana; font-size: 8pt; cursor: pointer; text-decoration: none; text-align: left;\"><span style=\"font-weight: bold;\">HHP2HTML_TITLE</span><br>\n"+
						"</div>\n"+
						"<title>Contents</title>\n"+
						"<meta name=\"vs_targetSchema\" content=\"http://schemas.microsoft.com/intellisense/ie5\">\n"+
						"<link rel=\"stylesheet\" type=\"text/css\" href=\"tree.css\">\n"+
						"</head>\n"+
						"<div style=\"margin: 6px 4px 8px 2px; font-family: verdana; font-size: 8pt; cursor: pointer; text-align: right; text-decoration: none;\" ><br></div>\n"+
						"<div id=\"tree\" style=\"top: 35px; left: 0px;\" class=\"treeDiv\">\n"+
						"</span>\n"+
						//"&nbsp;&nbsp;&nbsp;&nbsp;Introduction<br>\n"+
						"HHP2HTML_NO_FRAME_TREE\n"+
						"</span></div>\n"+
						"</div>\n"+
						"</body>\n"+
						"</html>";
	
		static string contentTemplate = 
						"<html>\n"+
						"  <head>\n"+
						"    <title>Contents</title>\n"+
						"    <meta name=\"vs_targetSchema\" content=\"http://schemas.microsoft.com/intellisense/ie5\" />\n"+
						"    <link rel=\"stylesheet\" type=\"text/css\" href=\"tree.css\" />\n"+
						"    <script src=\"tree.js\" language=\"javascript\" type=\"text/javascript\">\n"+
						"    </script>\n"+
						"  </head>\n"+
						"  <body id=\"docBody\" style=\"background-color: #6699CC; color: White; margin: 0px 0px 0px 0px;\" onload=\"resizeTree()\" onresize=\"resizeTree()\" onselectstart=\"return false;\">\n"+
						"     <table style=\"width:100%\">\n"+
						"         <tr>\n"+
						"             <td>\n"+
						"             &nbsp;\n"+
						"                 <a style=\"font-family: verdana; font-size: 8pt; cursor: pointer; text-align: left\" \n"+
						"                 onmouseover=\"this.style.textDecoration='underline'\" \n"+
						"                 onmouseout=\"this.style.textDecoration='none'; this.style.background-color='red';\"\n"+ 
						"                  href=\"../../search.aspx\" target=\"main\" ><img style=\"width: 20px; height: 16px;\" alt=\"\" src=\"search.bmp\"></a>\n"+
						"             </td>\n"+
						"             <td>\n"+
						"             <div style=\"font-family: verdana; font-size: 8pt; cursor: pointer; margin: 6 4 8 2; text-align: right\" \n"+
						"                 onmouseover=\"this.style.textDecoration='underline'\" \n"+
						"                 onmouseout=\"this.style.textDecoration='none'\" \n"+
						"                 onclick=\"syncTree(window.parent.frames[1].document.URL)\">sync toc <img style=\"width: 16px; height: 16px;\" alt=\"\" src=\"synch.bmp\"></div>\n"+
						"             </td>\n"+
						"         </tr>\n"+
						"     </table>\n"+						
						"      <div id=\"tree\" style=\"top: 35px; left: 0px;\" class=\"treeDiv\">\n"+
						"        <div id=\"treeRoot\" onselectstart=\"return false\" ondragstart=\"return false\">\n"+
						"          HHP2HTML_TREE\n"+
						"        </div>\n"+
						"      </div>\n"+
						"    </div>\n"+
						"  </body>\n"+
						"</html>";
}


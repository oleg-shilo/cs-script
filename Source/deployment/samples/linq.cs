//css_ref System.Core;
using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;

class Script
{
    [STAThread]
    static public void Main(string[] args)
    {
        var data = new[]
		{
			new {Id = 1, Identifier = "A", Index = 3},
			new {Id = 2, Identifier = "A", Index = 2},
			new {Id = 3, Identifier = "A", Index = 1},
			new {Id = 4, Identifier = "B", Index = 2},
			new {Id = 5, Identifier = "B", Index = 1},
			new {Id = 6, Identifier = "C", Index = 2},
			new {Id = 7, Identifier = "C", Index = 3},
			new {Id = 8, Identifier = "C", Index = 1},
		};

        var query = from item in data
                    group item by item.Identifier into g
                    select (
                                new
                                {
                                    Data = (from itemOfGroup in g
                                            orderby itemOfGroup.Index descending
                                            select itemOfGroup).First(),
                                    Count = g.Count()
                                }
                           );

        foreach (var i in query)
            Console.WriteLine("Identifier: " + i.Data.Identifier + "; Id: " + i.Data.Id + "; Count: " + i.Count);

    }
}

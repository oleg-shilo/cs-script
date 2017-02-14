//css_pre precompile($this);
//css_inc ($this.name).g.cs;
//css_inc linq.includes.cs;

using System;
using System.Collections.Generic;

partial class MyData
{
    /* css_extend_class MyData
    NEW()
    PROP(static, bool, Direction, false)
    PROP(      , string, Text, "test")
    */
    public override string ToString()
    {
        return Direction + ", " + Text;
    }
}
public partial class Log
{
    /* css_extend_class Log 
     * USING(System.IO)
     * PROP(static, bool, Empty, true)
     * PROP(static, StreamWriter, Writer, new StreamWriter(@"c:\MyApp.log"))
     */
}

/* css_extend_str_enum public Numbers
VALUE(One, "One unit")
VALUE(Two, "Two units")
VALUE(Tree, "Three units")
*/


class Script
{
    static public void Main(string[] args)
    {
        Console.WriteLine(MyData.New().ToString());

        foreach (Numbers num in Enum.GetValues(typeof(Numbers)))
            Console.WriteLine(num.ToStringEx());
    }
}


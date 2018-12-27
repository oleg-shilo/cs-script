//css_pre precompile($this);
//css_inc ($this.name).g.cs;

using System;

partial class MyData
{
    /* css_extend_class MyData
    PROP(static, string, Description, "MyData class")
    */
}
class Script
{
    static public void Main(string[] args)
    {
        Console.WriteLine(MyData.Description);
    }
}

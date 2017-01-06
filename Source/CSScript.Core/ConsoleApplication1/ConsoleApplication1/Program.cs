using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using static csscript.Dump;
//using csscript.Dump;

class Test
{
    public int MyProperty { get; set; }
    public string Name { get; set; }
    int Index = 777;
    public StringComparison Comp = StringComparison.CurrentCultureIgnoreCase;
    public FileInfo info { get; set; }
    public FileInfo info1;
    public Test1 t1 { get; set; }
    public Test test_obj { get; set; }
    public Test t3;
    public override string ToString()
    {
        return $"Test object '{Name}'";
    }
}

class Test1
{
    public int DisplayIndex { get; set; } = 777;
}

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            //new DirectoryInfo(Environment.CurrentDirectory)
            //print(new DirectoryInfo(Environment.CurrentDirectory));
            //print(Assembly.GetExecutingAssembly());
            //Assembly.GetExecutingAssembly().Dump("dddd");

            var obj = new Test()
            {
                Name = "a",
                //t1 = new Test1(),
                test_obj = new Test()
                {
                    Name = "b",
                    test_obj = new Test()
                    {
                        //DisplayIndex = 999
                        Name = "c",
                        test_obj = new Test()
                        {
                            Name = "d"
                            //DisplayIndex = 999
                        }
                    }
                }
            };
            //dbg.print(obj);
            //dbg.print(new DirectoryInfo(Environment.CurrentDirectory));
            dbg.print(Assembly.GetExecutingAssembly(), 4);

        }
    }
}


namespace csscript
{
    // Based on ObjectDumper from MS 101-LINQ samples. 
    // ***********************************************
    // * Copyright © Microsoft Corporation.  All Rights Reserved.
    // * This code released under the terms of the 
    // * Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
    // *
    // * Copyright (C) Microsoft Corporation.  All rights reserved.
    // ***********************************************
    // Changes:
    // - Added extension methods
    // - Handled exceptions on prop/field GetValue calls
    // - Added line breaks
    // - Code refactoring


    public class ObjectDumper
    {
        public static void Write(object o)
        {
            Write(o, 0);
        }

        public static void Write(object o, int depth)
        {
            Write(o, depth, Console.Out);
        }

        public static void Write(object element, int depth, TextWriter log)
        {
            var dumper = new ObjectDumper(1);
            dumper.writer = log;
            dumper.Write("{" + element + "}");
            dumper.WriteLine();
            dumper.WriteTab();
            dumper.WriteObject(null, element);
        }

        TextWriter writer;
        int pos;
        int level;
        int depth;

        ObjectDumper(int depth)
        {
            this.depth = depth;
        }

        void Write(string s)
        {
            if (s != null)
            {
                writer.Write(s);
                pos += s.Length;
            }
        }

        void WriteIndent()
        {
            for (int i = 0; i < level; i++) writer.Write("  ");
        }

        void WriteLine()
        {
            writer.WriteLine();
            pos = 0;
        }

        void WriteTab()
        {
            Write("  ");
            while (pos % 4 != 0) Write(" ");
        }

        void WriteObject(string prefix, object element)
        {
            if (element == null || element is ValueType || element is string)
            {
                WriteIndent();
                Write(prefix);
                WriteValue(element);
                WriteLine();
            }
            else
            {
                var enumerableElement = element as IEnumerable;


                if (enumerableElement != null)
                {
                    foreach (object item in enumerableElement)
                    {
                        return; //temp zos
                        if (item is IEnumerable && !(item is string))
                        {
                            WriteIndent();
                            Write(prefix);
                            Write("...");
                            WriteLine();
                            if (level < depth)
                            {
                                level++;
                                WriteObject(prefix, item);
                                level--;
                            }
                        }
                        else
                        {
                            WriteObject(prefix, item);
                        }
                    }
                }
                else
                {
                    bool publicOnly = true;
                    bool propsOnly = true;


                    Func<MemberInfo, bool> relevant_types = x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property;
                    if (propsOnly)
                        relevant_types = x => x.MemberType == MemberTypes.Property;



                    MemberInfo[] members = element.GetType()
                                                  .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                                                  .Where(relevant_types)
                                                  .OrderBy(x => x.Name)
                                                  .ToArray();

                    var private_members = new MemberInfo[0];
                    if (!publicOnly)
                        private_members = element.GetType()
                                                          .GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)
                                                          .Where(relevant_types)
                                                          .OrderBy(x => x.Name)
                                                          .OrderBy(x => char.IsLower(x.Name[0]))
                                                          .OrderBy(x => x.Name.StartsWith("_"))
                                                          .ToArray();


                    WriteIndent();
                    Write(prefix);
                    bool propWritten = false;

                    var items = members.Concat(private_members);


                    foreach (MemberInfo m in items)
                    {
                        FieldInfo f = m as FieldInfo;
                        PropertyInfo p = m as PropertyInfo;

                        if (f != null || p != null)
                        {
                            if (propWritten)
                            {
                                WriteTab();
                            }
                            else
                            {
                                propWritten = true;
                            }
                            Write(m.Name);
                            Write(": ");

                            Type t = f != null ? f.FieldType : p.PropertyType;

                            object value = null;
                            try
                            {
                                value = f != null ? f.GetValue(element) : p.GetValue(element, null);

                                if (t.IsValueType || t == typeof(string))
                                {
                                    WriteValue(value);
                                }
                                else
                                {
                                    if (typeof(IEnumerable).IsAssignableFrom(t))
                                    {
                                        Write("{" + value + "}"); //may need to print collection size
                                    }
                                    else
                                    {
                                        Write("{" + value + "}");
                                    }
                                }
                            }
                            catch
                            {
                                Write("{???}");
                            }

                            WriteLine();
                        }
                    }
                    if (propWritten) WriteLine();
                    if (level < depth)
                    {
                        foreach (MemberInfo m in members)
                        {
                            FieldInfo f = m as FieldInfo;
                            PropertyInfo p = m as PropertyInfo;

                            if (f != null || p != null)
                            {
                                Type t = f != null ? f.FieldType : p.PropertyType;

                                if (!(t.IsValueType || t == typeof(string)))
                                {
                                    object value = "{???}";
                                    try
                                    {
                                        value = f != null ? f.GetValue(element) : p.GetValue(element, null);
                                    }
                                    catch { }

                                    if (value != null)
                                    {
                                        level++;
                                        WriteObject(m.Name + ": ", value);
                                        level--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void WriteValue(object o)
        {
            if (o == null)
                Write("{null}");
            else if (o is DateTime)
                Write("{" + o + "}");
            else if (o is ValueType || o is string)
                Write("\"" + o + "\"");
            else if (o is IEnumerable)
                Write("...");
            else
                Write("{ }");
        }
    }

    public static class Dump
    {
        public static void print(object obj)
        {
            ObjectDumper.Write(obj);
        }

        public static void print(object obj, TextWriter writer)
        {
            ObjectDumper.Write(obj, 0, writer);
        }
    }

    public static class Extensions
    {

        internal static object GetMemberValue(object element, MemberInfo m)
        {
            FieldInfo f = m as FieldInfo;
            PropertyInfo p = m as PropertyInfo;

            if (f != null || p != null)
            {
                try
                {
                    Type t = f != null ? f.FieldType : p.PropertyType;

                    if (!(t.IsValueType || t == typeof(string)))
                        return f != null ? f.GetValue(element) : p.GetValue(element, null);
                    else
                    {
                        if (typeof(IEnumerable).IsAssignableFrom(t))
                        {
                            return "...";
                        }
                        else
                        {
                            return "{ }";
                        }
                    }
                }
                catch
                {
                    return "???";
                }
            }
            return null;
        }
    }
}
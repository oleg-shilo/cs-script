using System.Diagnostics;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

public static class dbg_extensions
{
    static public T dump<T>(this T @object, params object[] args)
    {
        dbg.print(@object, args);
        return @object;
    }

    static public T print<T>(this T @object, params object[] args)
    {
        dbg.print(@object, args);
        return @object;
    }
}

partial class dbg
{
    public static bool publicOnly = true;
    public static bool propsOnly = false;
    public static int max_items = 25;
    public static int depth = 1;

    public static void printf(string format, params object[] args)
    {
        try
        {
            print(string.Format(format, args));
        }
        catch { }
    }

    public static void print(object @object, params object[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                new dbg().WriteObject(@object);
            }
            else
            {
                var sb = new StringBuilder();

                foreach (var o in new[] { @object }.Concat(args))
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append((o ?? "{null}").ToString());
                }
                new dbg().writeLine(sb.ToString());
            }
        }
        catch { }
    }

    //===============================
    int level = 0;

    string indent = "  ";

    void write(object @object = null)
    {
        if (@object != null)
            Console.Out.Write(@object.ToString().ReplaceClrAliaces());
    }

    void writeLine(object @object = null)
    {
        write(@object);
        Console.Out.WriteLine();
    }

    string Indent
    {
        get { return new string('0', level).Replace("0", indent); }
    }

    string DisplayName(IEnumerable obj)
    {
        if (obj is Array)
        {
            var arr = obj as Array;
            return "{" + obj + "} - Length: " + arr.Length + " item" + (arr.Length == 1 ? "" : "s");
        }
        else if (obj is IList)
        {
            var arr = obj as IList;
            return "{IList} - Count: " + arr.Count;
        }
        else
        {
            var count = obj.Cast<object>().Count();
            return "{IEnumerable} - " + count + " item" + (count == 1 ? "" : "s");
        }
    }

    static public string CustomPrimitiveTypes = "Newtonsoft.Json.Linq.JValue;";

    static bool isPrimitive(object obj)
    {
        if (obj == null || obj.GetType().IsPrimitive || obj is decimal || obj is string)
            return true;
        else if (CustomPrimitiveTypes != null)
            return CustomPrimitiveTypes.Split(new char[] { ';' }).Contains(obj.GetType().ToString());
        return false;
    }

    void WriteObject(object obj)
    {
        var enumerableElement = obj as IEnumerable;
        level++;
        if (isPrimitive(obj))
        {
            writeLine(obj);
        }
        else if (enumerableElement != null)
        {
            writeLine(DisplayName(enumerableElement));

            int index = 0;

            foreach (object item in enumerableElement)
            {
                write(Indent);
                if (index > max_items) //need to have some limit
                {
                    writeLine("... truncated ...");
                    break;
                }
                write("[" + (index++) + "]: ");
                if (level < (depth + 1))
                {
                    level++;
                    WriteValue(item);
                    // WriteObject(item);
                    level--;
                }
                writeLine("");
            }
        }
        else
        {
            writeLine("{" + obj + "}");

            foreach (MemberInfo m in GetMembers(obj))
            {
                write(Indent);
                write("." + m.Name);
                write(" = ");

                object value = GetMemberValue(obj, m);

                if (isPrimitive(value) || (level >= depth))
                {
                    WriteValue(value);
                    writeLine("");
                }
                else
                    WriteObject(value);
            }
        }
        level--;
    }

    object GetMemberValue(object element, MemberInfo m)
    {
        FieldInfo f = m as FieldInfo;
        PropertyInfo p = m as PropertyInfo;

        if (f != null || p != null)
        {
            try
            {
                Type t = f != null ? f.FieldType : p.PropertyType;
                return f != null ? f.GetValue(element) : p.GetValue(element, null);
            }
            catch
            {
                return "{???}";
            }
        }
        return null;
    }

    void WriteValue(object o)
    {
        if (o == null)
            write("{null}");
        else if (o is DateTime)
            write("{" + o + "}");
        else if (o is ValueType)
            write(o);
        else if (o is string)
            write("\"" + o + "\"");
        else
            write("{" + o.ToString().TrimStart('{').TrimEnd('}') + "}");
    }

    MemberInfo[] GetMembers(object obj)
    {
        Func<MemberInfo, bool> relevant_types = x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property;

        if (propsOnly)
            relevant_types = x => x.MemberType == MemberTypes.Property;

        MemberInfo[] members = obj.GetType()
                                      .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(relevant_types)
                                      .OrderBy(x => x.Name)
                                      .ToArray();

        var private_members = new MemberInfo[0];

        if (!publicOnly)
            private_members = obj.GetType()
                                      .GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)
                                      .Where(relevant_types)
                                      .OrderBy(x => x.Name)
                                      .OrderBy(x => char.IsLower(x.Name[0]))
                                      .OrderBy(x => x.Name.StartsWith("_"))
                                      .ToArray();

        var items = members.Concat(private_members);
        return items.ToArray();
    }
}

static class Extension
{
    static public string ReplaceWholeWord(this string text, string pattern, string replacement)
    {
        return Regex.Replace(text, @"\b(" + pattern + @")\b", replacement);
    }

    static public string ReplaceClrAliaces(this string text, bool hideSystemNamespace = false)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        else
        {
            var retval = text.ReplaceWholeWord("System.Object", "object")
                             .ReplaceWholeWord("System.Boolean", "bool")
                             .ReplaceWholeWord("System.Byte", "byte")
                             .ReplaceWholeWord("System.SByte", "sbyte")
                             .ReplaceWholeWord("System.Char", "char")
                             .ReplaceWholeWord("System.Decimal", "decimal")
                             .ReplaceWholeWord("System.Double", "double")
                             .ReplaceWholeWord("System.Single", "float")
                             .ReplaceWholeWord("System.Int32", "int")
                             .ReplaceWholeWord("System.UInt32", "uint")
                             .ReplaceWholeWord("System.Int64", "long")
                             .ReplaceWholeWord("System.UInt64", "ulong")
                             .ReplaceWholeWord("System.Object", "object")
                             .ReplaceWholeWord("System.Int16", "short")
                             .ReplaceWholeWord("System.UInt16", "ushort")
                             .ReplaceWholeWord("System.String", "string")
                             .ReplaceWholeWord("System.Void", "void")
                             .ReplaceWholeWord("Void", "void");

            if (hideSystemNamespace && retval.StartsWith("System."))
            {
                string typeName = retval.Substring("System.".Length);

                if (!typeName.Contains('.')) // it is not a complex namespace
                    retval = typeName;
            }

            return retval.Replace("`1", "<T>")
                         .Replace("`2", "<T, T1>")
                         .Replace("`3", "<T, T1, T2>")
                         .Replace("`4", "<T, T1, T2, T3>");
        }
    }

    // for reflecting dynamic objects look at dbg.dynamic.cs
}
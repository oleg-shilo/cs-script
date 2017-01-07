using System;
using System.Collections;
using System.IO;
using System.Reflection;

class dbg
{
    public static bool publicOnly = false;
    public static bool propsOnly = false;
    public static int depth = 1;

    int level = 0;
    string indent = "  ";

    TextWriter writer = Console.Out;

    public static void print(object o)
    {
        new dbg().WriteObject(o);
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
            return "{" + obj + "} - " + arr.Length + " item" + (arr.Length == 1 ? "" : "s");
        }
        return "{" + obj + "}";
    }
    bool isPrimitive(object obj) { return (obj == null || obj is ValueType || obj is string); }

    void WriteObject(object obj)
    {
        var enumerableElement = obj as IEnumerable;
        level++;
        if (enumerableElement != null)
        {
            writer.WriteLine(DisplayName(enumerableElement));
            if (false)
            {
                foreach (object item in enumerableElement)
                {
                    if (item is IEnumerable && !(item is string))
                    {
                        writer.Write(Indent);
                        writer.Write("...");
                        writer.WriteLine();
                        if (level < depth)
                        {
                            level++;
                            WriteObject(item);
                            level--;
                        }
                    }
                    else
                    {
                        WriteObject(item);
                    }
                }
            }
            else
            {
                //temp
                //if (enumerableElement is Array)
                //{
                //    var arr = enumerableElement as Array;
                //    writer.Write(Indent);
                //    WriteValue("" + arr.Length + " items(s)");
                //    writer.WriteLine();
                //}
            }
        }
        else
        {
            writer.WriteLine("{" + obj + "}");

            foreach (MemberInfo m in GetMembers(obj))
            {
                writer.Write(Indent);
                writer.Write("." + m.Name);
                writer.Write(" = ");
                object value = GetMemberValue(obj, m);

                if (isPrimitive(value) || (level >= depth))
                {
                    WriteValue(value);
                    writer.WriteLine();
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
            writer.Write("{null}");
        else if (o is DateTime)
            writer.Write("{" + o + "}");
        else if (o is ValueType)
            writer.Write(o);
        else if (o is string)
            writer.Write("\"" + o + "\"");
        //else if (o is IEnumerable)
        //    writer.Write("...");
        else
            writer.Write("{" + o + "}");
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
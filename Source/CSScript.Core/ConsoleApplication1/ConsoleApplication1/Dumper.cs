using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Text;

namespace ObjectDumper
{
    public class DumpOptions
    {
        public static DumpOptions Default = new DumpOptions();

        public bool NoFields { get; set; }

    }

    /// <summary>
    /// This class implements the core dumping algorithm.
    /// </summary>
    public static class Dumper
    {
        /// <summary>
        /// Dumps the specified value to the <see cref="TextWriter"/> using the
        /// specified <paramref name="name"/>.
        /// </summary>
        /// <param name="value">
        /// The value to dump to the <paramref name="writer"/>.
        /// </param>
        /// <param name="name">
        /// The name of the <paramref name="value"/> being dumped.
        /// </param>
        /// <param name="writer">
        /// The <see cref="TextWriter"/> to dump the <paramref name="value"/> to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="name"/> is <c>null</c> or empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="writer"/> is <c>null</c>.</para>
        /// </exception>
        public static void Dump(object value, string name, TextWriter writer)
        {
            Dump(value, name, writer, DumpOptions.Default);
        }

        public static void Dump(object value, string name, TextWriter writer, DumpOptions options)
        {
            if (StringEx.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (options == null)
                throw new ArgumentNullException("options");

            var idGenerator = new ObjectIDGenerator();
            InternalDump(0, name, value, writer, idGenerator, true, options);
        }

        private static void InternalDump(int indentationLevel, string name, object value, TextWriter writer, ObjectIDGenerator idGenerator, bool recursiveDump, DumpOptions options)
        {
            var indentation = new string(' ', indentationLevel * 3);

            if (value == null)
            {
                writer.WriteLine("{0}{1} = <null>", indentation, name);
                return;
            }

            Type type = value.GetType();

            // figure out if this is an object that has already been dumped, or is currently being dumped
            string keyRef = string.Empty;
            string keyPrefix = string.Empty;
            if (!type.IsValueType)
            {
                bool firstTime;
                long key = idGenerator.GetId(value, out firstTime);
                if (!firstTime)
                    keyRef = string.Format(CultureInfo.InvariantCulture, " (see #{0})", key);
                else
                {
                    keyPrefix = string.Format(CultureInfo.InvariantCulture, "#{0}: ", key);
                }
            }

            // work out how a simple dump of the value should be done
            bool isString = value is string;
            string typeName = value.GetType().FullName;
            string formattedValue = value.ToString();

            var exception = value as Exception;
            if (exception != null)
            {
                formattedValue = exception.GetType().Name + ": " + exception.Message;
            }

            if (formattedValue == typeName)
                formattedValue = string.Empty;
            else
            {
                // escape tabs and line feeds
                formattedValue = formattedValue.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");

                // chop at 80 characters
                int length = formattedValue.Length;
                if (length > 80)
                    formattedValue = formattedValue.Substring(0, 80);
                if (isString)
                    formattedValue = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", formattedValue);
                if (length > 80)
                    formattedValue += " (+" + (length - 80) + " chars)";
                formattedValue = " = " + formattedValue;
            }

            writer.WriteLine("{0}{1}{2}{3} [{4}]{5}", indentation, keyPrefix, name, formattedValue, value.GetType(), keyRef);

            // Avoid dumping objects we've already dumped, or is already in the process of dumping
            if (keyRef.Length > 0)
                return;

            // don't dump strings, we already got at around 80 characters of those dumped
            if (isString)
                return;

            // don't dump value-types in the System namespace
            if (type.IsValueType && type.FullName == "System." + type.Name)
                return;

            // Avoid certain types that will result in endless recursion
            if (type.FullName == "System.Reflection." + type.Name)
                return;

            if (value is System.Security.Principal.SecurityIdentifier)
                return;

            if (!recursiveDump)
                return;

            PropertyInfo[] properties =
                (from property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 where property.GetIndexParameters().Length == 0
                       && property.CanRead
                 select property).ToArray();
            IEnumerable<FieldInfo> fields = options.NoFields ? Enumerable.Empty<FieldInfo>() : type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (!properties.Any() && !fields.Any())
                return;

            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}{{", indentation));
            if (properties.Any())
            {
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}   properties {{", indentation));
                foreach (PropertyInfo pi in properties)
                {
                    try
                    {
                        object propertyValue = pi.GetValue(value, null);
                        InternalDump(indentationLevel + 2, pi.Name, propertyValue, writer, idGenerator, true, options);
                    }
                    catch (TargetInvocationException ex)
                    {
                        InternalDump(indentationLevel + 2, pi.Name, ex, writer, idGenerator, false, options);
                    }
                    catch (ArgumentException ex)
                    {
                        InternalDump(indentationLevel + 2, pi.Name, ex, writer, idGenerator, false, options);
                    }
                    catch (RemotingException ex)
                    {
                        InternalDump(indentationLevel + 2, pi.Name, ex, writer, idGenerator, false, options);
                    }
                }
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}   }}", indentation));
            }
            if (fields.Any())
            {
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}   fields {{", indentation));
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object fieldValue = field.GetValue(value);
                        InternalDump(indentationLevel + 2, field.Name, fieldValue, writer, idGenerator, true, options);
                    }
                    catch (TargetInvocationException ex)
                    {
                        InternalDump(indentationLevel + 2, field.Name, ex, writer, idGenerator, false, options);
                    }
                }
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}   }}", indentation));
            }
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}}}", indentation));
        }
    }


    /// <summary>
    /// This class adds extension methods to all types to facilitate dumping of
    /// object contents to various outputs.
    /// </summary>
    public static class ObjectDumperExtensions
    {
        /// <summary>
        /// Dumps the contents of the specified <paramref name="value"/> to the
        /// <see cref="Debug"/> output.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to dump.
        /// </typeparam>
        /// <param name="value">
        /// The object to dump.
        /// </param>
        /// <param name="name">
        /// The name to give to the object in the dump;
        /// or <c>null</c> to use a generated name.
        /// </param>
        /// <returns>
        /// The <paramref name="value"/>, to facilitate easy usage in expressions and method calls.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="name"/> is <c>null</c> or empty.</para>
        /// </exception>
        public static T Dump<T>(this T value, string name)
        {
            if (StringEx.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            using (var writer = new DebugWriter())
            {
                return Dump(value, name, writer);
            }
        }

        /// <summary>
        /// Dumps the contents of the specified <paramref name="value"/> to a file
        /// with the specified <paramref name="filename"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to dump.
        /// </typeparam>
        /// <param name="value">
        /// The object to dump.
        /// </param>
        /// <param name="name">
        /// The name to give to the object in the dump;
        /// or <c>null</c> to use a generated name.
        /// </param>
        /// <param name="filename">
        /// The full path to and name of the file to dump the object contents to.
        /// </param>
        /// <returns>
        /// The <paramref name="value"/>, to facilitate easy usage in expressions and method calls.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="name"/> is <c>null</c> or empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="filename"/> is <c>null</c> or empty.</para>
        /// </exception>
        public static T Dump<T>(this T value, string name, string filename)
        {
            // Error-checking in called method

            return Dump(value, filename, name, Encoding.Default);
        }

        /// <summary>
        /// Dumps the contents of the specified <paramref name="value"/> to a file
        /// with the specified <paramref name="filename"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to dump.
        /// </typeparam>
        /// <param name="value">
        /// The object to dump.
        /// </param>
        /// <param name="name">
        /// The name to give to the object in the dump;
        /// or <c>null</c> to use a generated name.
        /// </param>
        /// <param name="filename">
        /// The full path to and name of the file to dump the object contents to.
        /// </param>
        /// <param name="encoding">
        /// The <see cref="Encoding"/> to use for the file.
        /// </param>
        /// <returns>
        /// The <paramref name="value"/>, to facilitate easy usage in expressions and method calls.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="name"/> is <c>null</c> or empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="filename"/> is <c>null</c> or empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="encoding"/> is <c>null</c></para>
        /// </exception>
        public static T Dump<T>(this T value, string name, string filename, Encoding encoding)
        {
            if (StringEx.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            if (StringEx.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException("filename");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            using (var writer = new StreamWriter(filename, false, encoding))
            {
                return Dump(value, name, writer);
            }
        }

        /// <summary>
        /// Dumps the contents of the specified <paramref name="value"/> to
        /// the specified <paramref name="writer"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to dump.
        /// </typeparam>
        /// <param name="value">
        /// The object to dump.
        /// </param>
        /// <param name="name">
        /// The name to give to the object in the dump;
        /// or <c>null</c> to use a generated name.
        /// </param>
        /// <param name="writer">
        /// The <see cref="TextWriter"/> to dump the object contents to.
        /// </param>
        /// <returns>
        /// The <paramref name="value"/>, to facilitate easy usage in expressions and method calls.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="name"/> is <c>null</c> or empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="writer"/> is <c>null</c></para>
        /// </exception>
        public static T Dump<T>(this T value, string name, TextWriter writer)
        {
            return Dump(value, name, writer, DumpOptions.Default);
        }

        public static T Dump<T>(this T value, string name, TextWriter writer, DumpOptions options)
        {
            if (StringEx.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (options == null)
                throw new ArgumentNullException("options");

            Dumper.Dump(value, name, writer, options);

            return value;
        }

        /// <summary>
        /// Dumps the contents of the specified <paramref name="value"/> and
        /// returns the dumped contents as a string.
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to dump.
        /// </typeparam>
        /// <param name="value">
        /// The object to dump.
        /// </param>
        /// <param name="name">
        /// The name to give to the object in the dump;
        /// or <c>null</c> to use a generated name.
        /// </param>
        /// <returns>
        /// The dumped contents of the object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="name"/> is <c>null</c> or empty.</para>
        /// </exception>
        public static string DumpToString<T>(this T value, string name)
        {
            return DumpToString(value, name, DumpOptions.Default);
        }

        public static string DumpToString<T>(this T value, string name, DumpOptions options)
        {
            // Error-checking in called method

            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                Dump(value, name, writer, options);
                return writer.ToString();
            }
        }
    }
}



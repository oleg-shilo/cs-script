using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting;
using System.Text;
using System.Diagnostics;

namespace ObjectDumper
{

    /// <summary>
    /// This class implements <see cref="TextWriter"/> by writing to <see cref="Debug"/>.
    /// </summary>
    public sealed class DebugWriter : TextWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugWriter"/> class.
        /// </summary>
        public DebugWriter()
            : base(CultureInfo.InvariantCulture)
        {
            // Do nothing here
        }

        /// <summary>
        /// Returns the <see cref="Encoding"/> in which the output is written.
        /// </summary>
        /// <returns>
        /// The Encoding in which the output is written.
        /// </returns>
        public override Encoding Encoding
        {
            get
            {
                return Encoding.Default;
            }
        }

        /// <summary>
        /// Writes a character to the text stream.
        /// </summary>
        /// <param name="value">
        /// The character to write to the text stream. 
        /// </param>
        public override void Write(char value)
        {
            Debug.Write(value);
        }

        /// <summary>
        /// Writes a string to the text stream.
        /// </summary>
        /// <param name="value">
        /// The string to write. 
        /// </param>
        public override void Write(string value)
        {
            Debug.Write(value);
        }
    }

    internal static class StringEx
    {
        /// <summary>
        /// Compares the <paramref name="value"/> against <c>null</c> and checks if the
        /// string contains only whitespace.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the string <paramref name="value"/> is <c>null</c>, <see cref="string.Empty"/>,
        /// or contains only whitespace; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return value == null || value.Trim().Length == 0;
        }
    }
}
using System;
using System.IO;
using System.Reflection;
using System.Text;

/// <summary>
/// Provides basic information about an Assembly derived from its metadata.
/// Based on "https://stackoverflow.com/questions/58025730/how-to-determine-if-a-net-assembly-was-built-with-platform-target-anycpu-anycp/58025731#58025731"
/// Codeproject licence is actually quite permissive: (https://www.codeproject.com/info/cpol10.aspx)
/// If explicitly states: "Source Code and Executable Files can be used in commercial applications;"
/// </summary>
public class CorFlagsReader
{
    /// <summary>
    /// Gets the major version of the CLI runtime required for the assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typical runtime versions:
    /// <list type="bullet">
    /// <item>0 for unmanaged PE Files.</item>
    /// <item>2.0: .Net 1.0 / .Net 1.1</item>
    /// <item>2.5: .Net 2.0 / .Net 3.0 / .Net 3.5 / .Net 4.0</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int MajorRuntimeVersion { get; }

    /// <summary>
    /// Gets the minor version of the CLI runtime required for the assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typical runtime versions:
    /// <list type="bullet">
    /// <item>0 for unmanaged PE Files.</item>
    /// <item>2.0: .Net 1.0 / .Net 1.1</item>
    /// <item>2.5: .Net 2.0 / .Net 3.0 / .Net 3.5 / .Net 4.0</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int MinorRuntimeVersion { get; }

    /// <summary>
    /// Gets the processor architecture required for the assembly or
    /// </summary>
    /// <returns>Possible return values: X86, Amd64, MSIL </returns>
    public ProcessorArchitecture ProcessorArchitecture { get; }

    /// <summary>
    /// If true the PE files does not contain any unmanaged parts. Otherwise it is a managed C++ Target.
    /// </summary>
    public bool IsPureIL { get; }

    /// <summary>
    /// Gets information whether Is32BitRequired flag is set in PE header
    /// </summary>
    public bool Is32BitReq { get; }

    /// <summary>
    /// Gets information whether Is32BitPrefered flag is set in PE header
    /// </summary>
    public bool Is32BitPref { get; }

    /// <summary>
    /// Returns true when the assembly is signed.
    /// </summary>
    public bool IsSigned { get; }

    private enum PEFormat : ushort
    {
        PE32 = 0x10b,
        PE32Plus = 0x20b
    }

    [Flags]
    private enum CorFlags : uint
    {
        ILOnly = 0x00000001,
        Requires32Bit = 0x00000002,
        ILLibrary = 0x00000004,
        StrongNameSigned = 0x00000008,
        NativeEntryPoint = 0x00000010,
        TrackDebugData = 0x00010000,
        Prefers32Bit = 0x00020000,
    }

    private class Section
    {
        public uint VirtualAddress;
        public uint VirtualSize;
        public uint Pointer;
    }

    private CorFlagsReader(ushort majorRuntimeVersion, ushort minorRuntimeVersion, CorFlags corflags, PEFormat peFormat)
    {
        MajorRuntimeVersion = majorRuntimeVersion;
        MinorRuntimeVersion = minorRuntimeVersion;

        IsPureIL = (corflags & CorFlags.ILOnly) == CorFlags.ILOnly;
        Is32BitReq = (corflags & CorFlags.Requires32Bit) == CorFlags.Requires32Bit;
        Is32BitPref = (corflags & CorFlags.Prefers32Bit) == CorFlags.Prefers32Bit;
        IsSigned = (corflags & CorFlags.StrongNameSigned) == CorFlags.StrongNameSigned;

        ProcessorArchitecture = peFormat == PEFormat.PE32Plus
            ? ProcessorArchitecture.Amd64
            : (corflags & CorFlags.Requires32Bit) == CorFlags.Requires32Bit || !IsPureIL
                ? ProcessorArchitecture.X86
                : ProcessorArchitecture.MSIL;
    }

    /// <summary>
    /// Reads the PE file
    /// </summary>
    /// <param name="fileName">PE file to read from</param>
    /// <returns>null if the PE file was not valid, an instance of the CorFlagsReader class containing the requested data.</returns>
    /// <exception cref="FileNotFoundException">When the file could not be found.</exception>
    public static CorFlagsReader ReadAssemblyMetadata(string fileName)
    {
        using (var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
            return ReadAssemblyMetadata(fStream);
        }
    }

    /// <summary>
    /// Reads the PE file
    /// </summary>
    /// <param name="assemblyAsByteArray"></param>
    /// <returns></returns>
    public static CorFlagsReader ReadAssemblyMetadata(byte[] assemblyAsByteArray)
    {
        using (var stream = new MemoryStream(assemblyAsByteArray))
        {
            return ReadAssemblyMetadata(stream);
        }
    }

    /// <summary>
    /// Reads the PE file
    /// </summary>
    /// <param name="stream">PE file stream to read from.</param>
    /// <returns>null if the PE file was not valid, an instance of the CorFlagsReader class containing the requested data.</returns>
    public static CorFlagsReader ReadAssemblyMetadata(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        long length = stream.Length;
        if (length < 0x40)
            return null;

        using (var reader = new BinaryReader(stream, new UTF8Encoding(), true))
        {
            // Read the pointer to the PE header.
            stream.Position = 0x3c;
            uint peHeaderPtr = reader.ReadUInt32();
            if (peHeaderPtr == 0)
                peHeaderPtr = 0x80;

            // Ensure there is at least enough room for the following structures:
            //     24 byte PE Signature & Header
            //     28 byte Standard Fields         (24 bytes for PE32+)
            //     68 byte NT Fields               (88 bytes for PE32+)
            // >= 128 byte Data Dictionary Table
            if (peHeaderPtr > length - 256)
                return null;

            // Check the PE signature.  Should equal 'PE\0\0'.
            stream.Position = peHeaderPtr;
            var peSignature = reader.ReadUInt32();
            if (peSignature != 0x00004550)
                return null;

            // Read PE header fields.
            var machine = reader.ReadUInt16();
            var numberOfSections = reader.ReadUInt16();
            var timeStamp = reader.ReadUInt32();
            var symbolTablePtr = reader.ReadUInt32();
            var numberOfSymbols = reader.ReadUInt32();
            var optionalHeaderSize = reader.ReadUInt16();
            var characteristics = reader.ReadUInt16();

            // Read PE magic number from Standard Fields to determine format.
            PEFormat peFormat = (PEFormat)reader.ReadUInt16();
            if (peFormat != PEFormat.PE32 && peFormat != PEFormat.PE32Plus)
                return null;

            // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
            // When this is non-zero then the file contains CLI data otherwise not.
            stream.Position = peHeaderPtr + (peFormat == PEFormat.PE32 ? 232 : 248);
            var cliHeaderRva = reader.ReadUInt32();
            if (cliHeaderRva == 0)
                return new CorFlagsReader(0, 0, 0, peFormat);

            // Read section headers.  Each one is 40 bytes.
            //    8 byte Name
            //    4 byte Virtual Size
            //    4 byte Virtual Address
            //    4 byte Data Size
            //    4 byte Data Pointer
            //  ... total of 40 bytes
            var sectionTablePtr = peHeaderPtr + 24 + optionalHeaderSize;
            var sections = new Section[numberOfSections];
            for (int i = 0; i < numberOfSections; i++)
            {
                stream.Position = sectionTablePtr + i * 40 + 8;

                Section section = new Section { VirtualSize = reader.ReadUInt32(), VirtualAddress = reader.ReadUInt32() };
                reader.ReadUInt32();
                section.Pointer = reader.ReadUInt32();

                sections[i] = section;
            }

            // Read parts of the CLI header.
            var cliHeaderPtr = ResolveRva(sections, cliHeaderRva);
            if (cliHeaderPtr == 0)
                return null;

            stream.Position = cliHeaderPtr + 4;
            var majorRuntimeVersion = reader.ReadUInt16();
            var minorRuntimeVersion = reader.ReadUInt16();
            var metadataRva = reader.ReadUInt32();
            var metadataSize = reader.ReadUInt32();
            CorFlags corflags = (CorFlags)reader.ReadUInt32();

            // Done.
            return new CorFlagsReader(majorRuntimeVersion, minorRuntimeVersion, corflags, peFormat);
        }
    }

    private static uint ResolveRva(Section[] sections, uint rva)
    {
        foreach (var section in sections)
        {
            if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.VirtualSize)
                return rva - section.VirtualAddress + section.Pointer;
        }

        return 0;
    }
}
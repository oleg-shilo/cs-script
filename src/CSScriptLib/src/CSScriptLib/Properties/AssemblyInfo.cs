using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Oleg Shilo")]
[assembly: AssemblyProduct("CS-Script")]
[assembly: AssemblyTrademark("CS-Script")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7cea89c9-e906-4852-ba40-16be33bcc2d1")]

// Signed with sgKey.snk
// The key is published in the repo according the MS recommendation
// (https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/strong-naming#create-strong-named-net-libraries)
// ✔️ CONSIDER strong naming your library's assemblies.
// ✔️ CONSIDER adding the strong naming key to your source control system.

[assembly: InternalsVisibleTo("cscs.tests, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100d17b106c0897f2" +
    "962c69449792d3b89ce228b8413184d66934ead688f75e8526ba162c7e6d4a32a48abdccbc8863" +
    "ec9ae6ca453f05d148a968e217a0e22805ce9b5138d3f4f7f004c8074fa7e001c2a3bb0b8c275a" +
    "40967efff2aa574ca04339df8fdcdcb6eb01afa1b28f0ad1d6498d0e17ba555c8c2535c5d6e1e7" +
    "121bd7a0")]
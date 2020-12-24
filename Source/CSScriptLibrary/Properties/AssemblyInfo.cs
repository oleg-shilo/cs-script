using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CSScriptLibrary")]
[assembly: AssemblyDescription("C# Script engine Class Library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Oleg Shilo")]
[assembly: AssemblyProduct("C# Script engine Class Library")]
[assembly: AssemblyCopyright("(C) 2004-2018 Oleg Shilo")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8afbd3fc-0a39-4db5-b592-b2981982b33d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("3.30.5.2")]
[assembly: AssemblyFileVersion("3.30.5.2")]

//
// In order to sign your assembly you must specify a key to use. Refer to the
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing.
//
// Notes:
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//	   Provider (CSP) on your machine. KeyFile refers to a file which contains
//	   a key.
//   (*) If the KeyFile and the KeyName values are both specified, the
//	   following processing occurs:
//	   (1) If the KeyName can be found in the CSP, that key is used.
//	   (2) If the KeyName does not exist and the KeyFile does exist, the key
//		   in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//	   When specifying the KeyFile, the location of the KeyFile should be
//	   relative to the project output directory which is
//	   %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//	   located in the project directory, you would specify the AssemblyKeyFile
//	   attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//	   documentation for more information on this.
//
[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]
#if !CSSLib_BuildUnsigned
[assembly: AssemblyKeyFile("..\\..\\sgKey.snk")]
#endif
[assembly: AssemblyKeyName("")]

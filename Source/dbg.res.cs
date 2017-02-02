// Having truly embedded resources imposes unnecessary constrain on compatibility. 
// Thus cscs.exe compiled for .NET4.5 cannot be even started under .NET4.0 while the only 
// v4.5 specific operation cscs.exe does is the initializing the resource manager for loading resources.
// Avoiding using resource managed completely eliminates the constrain. 
// Good read: http://stackoverflow.com/questions/13748055/could-not-load-type-system-runtime-compilerservices-extensionattribute-from-as
class embedded_strings
{
    public static string dbg_source = "";
}
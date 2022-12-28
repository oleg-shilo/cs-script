using System.Linq;
using static dbg;
using static System.Reflection.Assembly;

if (args.Contains("-?") || args.Contains("-help"))
    print("Prints path to the script engine CLI executable.");
else
    GetEntryAssembly().Location.print();
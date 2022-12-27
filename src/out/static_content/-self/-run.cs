using static System.Reflection.Assembly; 
using static dbg; 
        
if (args.Contains("-?") || args.Contains("-help"))
	print("Prints path to the script engine CLI executable.");
else
	GetEntryAssembly().Location.print();
// CSI: A simple C# interpreter
// Copyright, Steve Donovan 2005
// Use freely, but please acknowledge!
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

public interface IConsole {
    string ReadLine();
    void Write(string s);
}

public class Utils {
    static Type lastClass = null;
    static int screenWidth = 70;
    static int maxLines = 80;
	public static Interpreter interpreter;

    // vs 0.5 It's possible to load scripts from within the interpreter.
	public static void Include(string file) {
		interpreter.ReadIncludeFile(file);		
	}
    
    public static void MInfo(object ctype, string mname) {
        Type t;
        if (ctype == null) {
            if (lastClass != null)
                ctype = lastClass;
            else
                return;
        }
        if (ctype is String) {
            string cname = (string)ctype;
            if (cname.Length < 7 || cname.Substring(0,7) != "System.")
                cname = "System." + cname;
            t = Type.GetType(cname);
             if (t == null)  throw(new Exception("is not a type"));
        } else
            t = (Type)ctype;
        lastClass = t;
        try {
            string lastName = "";
            int k = 0;
            if (! t.IsClass && ! t.IsInterface)
                throw(new Exception("is not a class, struct or interface"));            
            foreach (MethodInfo mi in t.GetMethods()) {
                if (mi.IsPublic && mi.DeclaringType == t) 
                    if (mname == null) {
                        if (mi.Name != lastName && mi.Name.IndexOf('_') == -1) {
                            lastName = mi.Name;
                            Write(lastName);
                            if (++k == 5) {
                                Print();
                                k = 0;
                            } else
                                Write(" ");
                        }
                    } else {
                        if (mi.Name == mname) {
                            string proto = mi.ToString();
                            proto = proto.Replace("System.","");
                            if (mi.IsStatic)
                                proto = "static " + proto;
                            if (mi.IsVirtual)
                                proto = "virtual " + proto;
                            Print(proto);                            
                        }                            
                    }
            }
            if (k > 0)
                Print();
        } catch(Exception e) {
            Print("Error: " + ctype,e.Message);
        }
    }    
	
    // vs 0.8. This is a smart version of Printl, which tries to keep to a reasonable
    // line width, and won't go on forever. Also, strings and chars are quoted.
    public static void Dumpl(IEnumerable c) {
		Write("{");
        int nlines = 0;
        StringBuilder sb = new StringBuilder();
        foreach(object o in c) {
            string s;
            if (o != null) {
                s = o.ToString();
				if (o is string) s = "\""+s+"\""; else
				if (o is char)  s = "\'"+s+"\'";
			} else 
				s =  "<null>";
            if (sb.Length + s.Length < screenWidth) {                
                sb.Append(s);
                sb.Append(',');
            } else {
                Write(sb.ToString());
                Write("\n");
                sb = new StringBuilder();
                if (nlines++ > maxLines) {                    
                    sb.Append(".....");
                    break;
                }
            }
        }
         Write(sb.ToString() + "}\n");     
    }	
 
    public static void Printl(IEnumerable c) {
        foreach(object o in c) {
            if (o != null) Write(o.ToString());
                        else Write("<null>");
            Write(" ");
        }
         Write("\n");     
    }
    // a very convenient function for quick output ('Print' is easier to type than 'Console.WriteLine')
    public static void Print(params object []obj) {
        Printl(obj);
    }
    
    public static string ReadLine() {
        return Interpreter.Console.ReadLine();
    }    
    
    public static void Write(string s) {
        Interpreter.Console.Write(s);
    }
    
}

public class CodeChunk : Utils {
    public static bool DumpingValue = true;
    
    // the generated assemblies will have to override this method
    public virtual void Go(Hashtable V) {
    }
    
    // here's the template used to generate the assemblies
    public const string Template =
         @"$USES$
       class CsiChunk : CodeChunk { 
        public override void Go(Hashtable V) {
          $BODY$;
        }
      }";       
    
    public static void Instantiate(Assembly a, Interpreter interp) {
		Hashtable table = interp.VarTable;
        try {
            CodeChunk chunk = (CodeChunk)a.CreateInstance("CsiChunk");
            chunk.Go(table);
            // vs 0.8 we display the type and value of expressions.  The variable $_ is 
            // always set, which is useful if you want to save the result of the last
            // calculation.
			if (interp.returnsValue && DumpingValue) {
				object val = table["_"];
				Type type = val.GetType();
				string stype = type.ToString();
				if (stype.StartsWith("System."))  // to simplify things a little bit...
					stype = stype.Substring(7);
				stype = "("+stype+")";
                if (val is string) {
                    Print(stype,"'"+val+"'");
                } else
				if (val is IEnumerable) {
					Print(stype);
					Dumpl((IEnumerable)val);
				} else
					Print(stype,val);
			}
        }  catch(Exception ex) {
            Print(ex.GetType() + " was thrown: " + ex.Message);
        }	    
    }
}

public class CsiFunctionContext : Utils { 
    public Hashtable V;    

    public const string Template =
         @"$USES$
       public class $CLASS$ : CsiFunctionContext { 
         public $BODY$
      }";       
    
    public static void Instantiate(Assembly a, Hashtable table, string className, string funName) {
        try {
            CsiFunctionContext dll = (CsiFunctionContext)a.CreateInstance(className);
            dll.V = table;
            table[className] = dll;
        }  catch(Exception ex) {
            Print(ex.GetType() + " was thrown: " + ex.Message);
        }	    
    }    
}

public class Interpreter {
    Hashtable varTable = new Hashtable();
    string namespaceString = "";
    ArrayList referenceList = new ArrayList();
    CSharpCodeProvider prov = new CSharpCodeProvider(new System.Collections.Generic.Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
    ICodeCompiler compiler;      
    bool mustDeclare = false;
    bool showCode = false;
    StringBuilder sb = new StringBuilder();    
    int bcount = 0;	
    public bool returnsValue;
    public static IConsole Console;
	static string[] keywords = {"for","foreach","while","using","if","switch","do"};
    enum CHash { Expression, Assignment, Function, Class };
    
    MacroSubstitutor macro = new MacroSubstitutor();    
    
    public Interpreter() {
        AddNamespace("System");
        AddNamespace("System.Collections");
        AddReference("system.dll"); 
        SetValue("interpreter",this);
		SetValue("_",this);
		Utils.interpreter = this;
        AddReference(FullExecutablePath());
        compiler = prov.CreateCompiler();     
    }    
    
    // vs 0.8 (fix by reinux) abosolute path of our executable, so it can always be found!    
    public string FullExecutablePath() {
        Assembly thisAssembly = Assembly.GetAssembly(typeof(Interpreter));
        return new Uri(thisAssembly.CodeBase).LocalPath;        
    }
    
    // vs 0.8 (reinux) the default .csi file is now found with the executable
    public string DefaultIncludeFile() {
        return  Path.ChangeExtension(FullExecutablePath(), ".csi");
    }    
    
    public void ReadIncludeFile(string file) {
        if (File.Exists(file)) {
            CodeChunk.DumpingValue = false;
            using(TextReader tr = File.OpenText(file)) {
                while (ProcessLine(tr.ReadLine()))
                    ;
            }
            CodeChunk.DumpingValue = true;
        }
    }
    
    public void SetValue(string name, object val) {
        varTable[name] = val;
    }   
    
    public bool ProcessLine(string line) {
     // Statements inside braces will be compiled together
        if (line == null)
            return false;
        if (line == "")
            return true;        
        if (line[0] == '/') {
            ProcessCommand(line);
            return true;
        }
        sb.Append(line);
        // ignore {} inside strings!  Otherwise keep track of our block level
        bool insideQuote = false;
        for (int i = 0; i < line.Length; i++) {
            if (line[i] == '\"')
                insideQuote = ! insideQuote;
            if (! insideQuote) {
                if (line[i] == '{') bcount++; else
                if (line[i] == '}') bcount--;
            }
        }
        if (bcount == 0) {            
            string code = sb.ToString();
            sb = new StringBuilder();
            if (code != "")
                ExecuteLine(code);            
        }
        return true;
    }
    
    static Regex cmdSplit = new Regex(@"(\w+)($|\s+.+)");
    static Regex spaces = new Regex(@"\s+");
    
    void ProcessCommand(string line) {
        Match m = cmdSplit.Match(line);
        string cmd = m.Groups[1].ToString();
        string arg  = m.Groups[2].ToString().TrimStart(null);
        switch(cmd) {
        case "n":
            AddNamespace(arg); 
            break;
        case "r":
            AddReference(arg);
            break;
        case "v":
            foreach(string v in varTable.Keys)
                Utils.Print(v + " = " + varTable[v]);
            break;
        case "dcl":
            MustDeclare = ! MustDeclare;
            break;
        case "code": //  show code sent to compiler!
            showCode = ! showCode;
            break;
        default: 
            // a macro may be used as a command; the line is split up and
            // and supplied as arguments.
            // For macros taking one argument, the whole line is supplied.
            MacroEntry me = macro.Lookup(cmd);
            if (me != null && me.Parms != null) {
                string[] parms;
                if (me.Parms.Length > 1)
                    parms = spaces.Split(arg);
                else
                    parms = new string[] { arg };
                string s = macro.ReplaceParms(me,parms);                
                ExecuteLine(s);
            } else
                Utils.Print("unrecognized command, or bad macro");
            break;
        }
    }
    
    // the actual dynamic type of an object may not be publically available
    // (e.g. Type.GetMethods() returns an array of RuntimeMethodInfo)
    // so we look for the first public base class.
    Type GetPublicRuntimeType(object symVal) {                
        Type symType = null;
        if (symVal != null) {
            symType = symVal.GetType();
            while (! symType.IsPublic)
                symType = symType.BaseType;            
        }
        return symType;        
    }
    
    static Regex dollarWord = new Regex(@"\$\w+");
    static Regex dollarAssignment = new Regex(@"\$\w+\s*=[^=]");  
	static Regex plainWord = new Regex(@"[a-zA-Z_]\w*");
    static Regex plainAssignment = new Regex(@"[a-zA-Z_]\w*\s*=[^=]");  
    static Regex assignment = dollarAssignment;
	static Regex wordPattern = dollarWord;   		
	
    // 'session variables' like $x will be replaced by ((LastType)V["x"]) where
    // LastType is the current type associated with the last value of V["x"].    
    // vs 0.8 The 'MustDeclare' mode; session variables don't need '$', but they must be
    // previously declared using var; declarations must look like this 'var <var> = <expr>'.	
    string MassageInput(string s, out bool wasAssignment) {
        // vs 0.8 (fix by toolmakerSteve2) process the words in reverse order when looking for assignments!        
		MatchCollection words = wordPattern.Matches(s);
		Match[] wordArray = new Match[words.Count];
		words.CopyTo(wordArray,0);		
		Array.Reverse(wordArray);
        wasAssignment = false;
		bool varDeclaration = false;
        for (int i = 0; i < wordArray.Length; i++) {
			Match m = wordArray[i];
            // exclude matches found inside strings
            if (s.LastIndexOf('"',m.Index) != -1 && s.IndexOf('"',m.Index) != -1)
                continue;
            string sym = m.Value;			
            if (! mustDeclare)     // strip the '$'
                sym = sym.Substring(1);   
            else { // either it's a declaration, or the var was previously declared.
				if (sym == "var")
					continue;
                // are we preceded by 'var'?  If so, this is a declaration				
				if (i+1 < wordArray.Length && wordArray[i+1].Value == "var") 
					varDeclaration = true;
				else if (varTable[sym] == null)
					continue;
			}
            string symRef = "V[\"" + sym + "\"]";     // will index our hashtable
            // are we followed by an assignment operator?
            Match lhs = assignment.Match(s,m.Index);
            wasAssignment = lhs != Match.Empty && lhs.Index == m.Index;           
            Type symType = GetPublicRuntimeType(varTable[sym]);
             // unless we're on the LHS, try to strongly type this variable reference.
            if (symType != null && ! wasAssignment)
                symRef = "((" + symType.ToString() + ")" + symRef + ")";            
            s = wordPattern.Replace(s,symRef,1,m.Index);
        }        
		if (varDeclaration)
			s = s.Replace("var ","");
        return s;
    }
    
    static Regex funDef = new Regex(@"\s*[a-zA-Z]\w*\s+([a-zA-Z]\w*)\s*\(.*\)\s*{");
    static int nextAssembly = 1;
    
    void ExecuteLine(string codeStr) {
        // at this point we either have a line to be immediately compiled and evaluated,
        // or a function definition.
        CHash type = CHash.Expression;
        string className=null,assemblyName=null,funName=null;
        Match funMatch = funDef.Match(codeStr);
        if (funMatch != Match.Empty)
            type = CHash.Function;
        if (type == CHash.Function) {
            funName = funMatch.Groups[1].ToString();
            macro.RemoveMacro(funName);
            className = "Csi" + nextAssembly++;
            assemblyName = className + ".dll";                        
            codeStr = codeStr.Insert(funMatch.Groups[1].Index,"_");
        }
        codeStr = macro.ProcessLine(codeStr);
        if (codeStr == "")  // may have been a prepro statement!
            return;
        bool wasAssignment;
        codeStr = MassageInput(codeStr, out wasAssignment); 
        if (wasAssignment)
            type = CHash.Assignment;
        CompilerResults cr = CompileLine(codeStr.TrimStart(),type,assemblyName,className);
        if (cr != null) {
            Assembly ass = cr.CompiledAssembly;
            if (type != CHash.Function)
                CodeChunk.Instantiate(ass,this);
            else {                
                CsiFunctionContext.Instantiate(ass,varTable,className,funName);
                string prefix = mustDeclare ? "" : "$";
                macro.AddMacro(funName,prefix+className+"._"+funName,null);                
                AddReference(Path.GetFullPath(assemblyName));
            }
        }
    }
	
	CompilerResults CompileTemplate(CompilerParameters cp,string codeStr,CHash type,string className) { 
        if (showCode)
            Utils.Print("code:",codeStr);
        string finalSource = CodeChunk.Template;
        if (type == CHash.Function)
            finalSource = CsiFunctionContext.Template;
        finalSource = finalSource.Replace("$USES$",namespaceString);
        finalSource = finalSource.Replace("$BODY$",codeStr);                  
        if (type == CHash.Function)
            finalSource = finalSource.Replace("$CLASS$",className);
		return compiler.CompileAssemblyFromSource(cp, finalSource);        
	}
	
	static Regex beginWord = new Regex(@"^\w+");
	
	string firstToken(string s) {
		Match m = beginWord.Match(s);
		return m.ToString();
	}
	
	bool word_within(string s, string[] strs) {
		return Array.IndexOf(strs,s) != -1;
	}
    
    CompilerResults CompileLine(string codeStr,CHash type,string assemblyName, string className) {
        CompilerParameters cp = new CompilerParameters();
        if (type == CHash.Function)
            cp.OutputAssembly = assemblyName;
        else
            cp.GenerateInMemory = true;        
        
        foreach(string r in referenceList) {
            //Utils.Print(r); //zos
            cp.ReferencedAssemblies.Add(r);
        }

		string exprStr = codeStr;
		returnsValue = false;
		if (type == CHash.Expression) {
			if (codeStr[0] != '{' && ! word_within(firstToken(codeStr),keywords)) {
				returnsValue = true;
				exprStr =  "V[\"_\"] = " + codeStr;
			}
		}		
		CompilerResults cr = CompileTemplate(cp,exprStr,type,className);
		if (cr.Errors.HasErrors) {			
			if (returnsValue) {
				// we assumed that this expression did return a value; we were wrong.
				// Try it again, without assignment to $_
				returnsValue = false;
				cr = CompileTemplate(cp,codeStr,CHash.Expression,"");
				if (! cr.Errors.HasErrors)
					return cr;
			}
			ShowErrors(cr,codeStr);			
			return null;
		}
        else
            return cr;
    }
    
    public void AddNamespace(string ns) {
        namespaceString = namespaceString + "using " + ns + ";\n";        
    }
    
    public void AddReference(string r) {
        referenceList.Add(r);
    }
    
    void ShowErrors(CompilerResults cr, string codeStr) {
		StringBuilder sbErr;
		sbErr = new StringBuilder("Compiling string: ");
		sbErr.AppendFormat("'{0}'\n\n", codeStr);
		foreach(CompilerError err in cr.Errors) {
			sbErr.AppendFormat("{0}\n",err.ErrorText);
		}
		Utils.Print(sbErr.ToString());		
    }
	
	public Hashtable VarTable {
		get { return varTable; }
	}
	
	public int BlockLevel {
		get { return bcount; }
	}	
	
	public bool MustDeclare {
		get { return mustDeclare; }
		set {
			mustDeclare = value;
			wordPattern = mustDeclare ? plainWord : dollarWord; 
            assignment = mustDeclare ? plainAssignment : dollarAssignment;
		}
	}
}


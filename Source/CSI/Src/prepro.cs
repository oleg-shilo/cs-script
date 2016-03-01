using System;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

public class MacroEntry {
    public string   Subst;
    public string[] Parms;
}

public class MacroSubstitutor {
    Hashtable macroTable = new Hashtable();    
   
    public void AddMacro(string name, string subst, string[] parms) {
        MacroEntry me = new MacroEntry();
        string pstr = "";
        if (parms != null)
            pstr = string.Join(",",parms);
        me.Subst = subst;
        me.Parms = parms;
        macroTable[name] = me;
    }
    
    public void RemoveMacro(string name) {
        macroTable[name] = null;
    }
    
    public MacroEntry Lookup(string s) {
        return (MacroEntry)macroTable[s];
    }
    
    static Regex iden = new Regex(@"[a-zA-Z_]\w*");            
    
    public string ReplaceParms(MacroEntry me, string[] actual_parms) {
        Match m;
        int istart = 0;
        string subst = me.Subst;
        while ((m = iden.Match(subst,istart)) != Match.Empty) {
            int idx = Array.IndexOf(me.Parms,m.Value);
            int len = m.Length;
            if (idx != -1) {
                string actual = actual_parms[idx];
                // A _single_ # before a token  means the 'stringizing' operator
                if (m.Index > 0 && subst[m.Index-1] == '#') {
                    // whereas ## means 'token-pasting'!  #s will be removed later!
                    if (! (m.Index > 1 && subst[m.Index-2] == '#'))
                        actual = '\"' + actual + '\"';                
                }
                subst = iden.Replace(subst,actual,1,istart);                
                len = actual.Length;
            }
            istart = m.Index + len;
        }
        subst = subst.Replace("#","");
        return subst;
    }
    
    public string Substitute(string str) {
        Match m;
        int istart = 0;
        while ((m = iden.Match(str,istart)) != Match.Empty) {
            MacroEntry me = (MacroEntry)macroTable[m.Value];
            if (me != null) {
                string subst = me.Subst;
                if (me.Parms != null) {
                    int i = m.Index + m.Length;  // points to char just beyond match
                    while (i < str.Length && str[i] != '(')
                        i++;
                    i++; // just past '('
                    int parenDepth = 1;
                    string [] actuals = new string[me.Parms.Length];
                    int idx = 0, isi = i;
                    while (parenDepth > 0 && i < str.Length) {
						char ch = str[i];
                        if (parenDepth == 1 && (ch == ',' || ch == ')')) {
                            actuals[idx] = str.Substring(isi,i - isi);
                            idx++;
                            isi = i+1;  // past ',' or ')'
                        }
						// *fix 0.8 now understands commas within braces or square brackets (e.g. 'matrix' indexing)
                        if (ch == '(' || ch == '{' || ch == '[') parenDepth++;  else 
                        if (ch == ')' || ch == '}' || ch == ']') parenDepth--; 
                        i++;
                    }
                    if (parenDepth != 0) {
                        return "**Badly formed macro call**";
                    }                    
                    subst = ReplaceParms(me,actuals);
                    istart = m.Index;
                    str = str.Remove(istart,i - istart);
                    str = str.Insert(istart,subst);                    
                } else {                    
                    str = iden.Replace(str,subst,1,istart);
                }
            } else
            istart = m.Index + m.Length;
        }
        return str;
    }
    
    static Regex define =  new Regex(@"#def (\w+)($|\s+|\(.+\)\s+)(.+)");    
    
    public string ProcessLine(string line) {
        Match m = define.Match(line);
        if (m != Match.Empty) {
            string [] parms = null;
            string sym = m.Groups[1].ToString();
            string subst = m.Groups[3].ToString();
            string arg = m.Groups[2].ToString();
            if (arg != "") {
                arg = arg.ToString();
                if (arg[0] == '(') {
                    arg = arg.TrimEnd(null);
                    arg = arg.Substring(1,arg.Length-2);
                    parms = arg.Split(new char[]{','});
                }
            }
            AddMacro(sym,subst,parms);
            return "";
        } else
            return Substitute(line);       
    }    
}



// See https://aka.ms/new-console-template for more information
using CSScripting;
using CSScriptLib;
using Microsoft.CodeAnalysis;

// dotnet publish --configuration Release --output .\publish

// ---------------------------
// evaluation of a simple expression
var div = CSScript.Evaluator.Eval("6/3");

Console.WriteLine(div);

// ---------------------------
// evaluation of a complex script
var calc = CSScript.Evaluator
                   .Eval(@"using System;
                           public class Script
                           {
                               public int Sum(int a, int b)
                               {
                                   return a+b;
                               }
                           }
                           return new Script();");

int sum = calc.Sum(1, 2);
Console.WriteLine(sum);

// ---------------------------
// compilation of a regular C# code
var asm = CSScript.Evaluator
                  .CompileCode(@"using System;
                                 public class Script
                                 {
                                     public int Div(int a, int b)
                                     {
                                         return a/b;
                                     }
                                 }", new CompileInfo { CodeKind = SourceCodeKind.Script });

dynamic script = asm.CreateObject("*.Script");

Console.WriteLine(script.Div(16, 2));
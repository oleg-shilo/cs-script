# Release v4.5.0.0 

---

Due to the quick progression v4.4.9->v4.5.0 this document contains v4.4.9 changes too.

## Changes

### CLI

v4.5.0
- Rebuild for .NET7
- Improved CLI documentation (triggered by #314)
- Added an extra CLI `//css_co` help sample for enabling nullable reference types (#313)
- Improved responsiveness of the "build-type" scripting scenarios (e.g. `css -check`)

v4.4.9
-  Enhancement #310: Add the possibility to use variables in css_include and css_import statements


### CSScriptLib

v4.4.9

- It's possible to define a custom algorithm of how to expand script directives.

  Hosting code:
  ```C#
  CSScript.EvaluatorConfig.ExpandStatementAlgorithm += 
                  statement => statement.Replace("%secret_folder%", Config.PrivateFolder);
  ```
  
  Script code:
  ```c#
  //css_include %secret_folder%/inputdata.cs
  . . .
  ```






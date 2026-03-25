# Release v4.14.4-Pre

---

## Changes 

========================================


### CLI

- <no changes>


### CSScriptLib

- #458: Type 'CSScriptLib.Project' in Assembly 'CSScriptLib, Version=4.14.3.0, Culture=neutral, PublicKeyToken=4c30df19402bb442' is not marked as serializable
- #459: Possibility to specify default search dirs, assemblies and namespaces in a hosted environment?
  ProjectBuilder is made public and documented.
- #444: IEvaluator API update
  - `IEvaluator.Check(string code)` is marked as `[obsolete]`
  - `IEvaluator.CheckCode(string scriptCode)`
  - `IEvaluator.CheckFile`(string scriptFile)`
  - `CodeDomEvaluator.CompileAssemblyFromFile(string scriptFile, string outputFile, out Project project)
  - `CodeDomEvaluator.CompileAssemblyFromCode(string scriptCode, string outputFile, out Project project)





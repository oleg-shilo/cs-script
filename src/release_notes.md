# Release v4.4.9.0

---

## Changes

### CLI

- No changes


### CSScriptLib

- Define a custom algorithm how to expand script code.
  Hosting code:

  ```C#
  CSScript.EvaluatorConfig.ExpandStatementAlgorithm += 
                statement => statement.Replace("%secret_folder%", Config.PrivateFolder);
  ```

  Script code:

  ```cs
  //css_include %secret_folder%/inputdata.cs
  . . .
  ```










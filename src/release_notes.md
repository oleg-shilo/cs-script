# Release v4.4.5.0

---

## Changes

### CLI

- no changes

### CSScriptLib

- Added `CSScript.EvaluatorConfig.CompilerOptions` for defining global compiler options for CodeDomEvaluator (RoslynEvaluator does not support string compiler options)
- Issue #291: CSScriptLib.CompilerException: error CS2021: File name '' is empty
- Issue #295: add compiler option -define:DEBUG if debug is set
- Add support for generic types to LoadMethod<T>










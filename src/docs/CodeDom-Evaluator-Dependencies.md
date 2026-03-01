# CodeDom Evaluator Dependencies

## Core Dependency

CodeDom evaluator requires a C# compiler (csc.exe) at runtime because it compiles code dynamically using the full compiler toolchain, not an in-process API like Roslyn.
Dependency Hierarchy

```txt
CodeDom Evaluator
    └─ csc.exe (C# compiler executable)
        ├─ System.dll (reference assemblies)
        ├─ System.Core.dll
        └─ Other .NET reference assemblies
```

### Acquisition Methods (Priority Order)

1. .NET SDK Installed ✅ Best<br>
   Location: C:\Program Files\dotnet\sdk\{version}\Roslyn\bincore\csc.exe<br>
   Method: Globals.FindSdKCompiler()<br>
   Status: Automatic if SDK installed
2. NuGet Package: Microsoft.Net.Sdk.Compilers.Toolset ✅ <br>
    Package: Microsoft.Net.Sdk.Compilers.Toolset<br>
    Location: %USERPROFILE%\.nuget\packages\microsoft.net.sdk.compilers.toolset\{version}\<br>
    Method: Globals.FindSdkToolsetPackageCompiler()<br>
    Download: NugetPackageDownloader.DownloadLatestSdkCompiler()<br>
    Size: ~6 MB
3. Legacy: Microsoft.Net.Compilers.Toolset (.NET Framework)<br>
    Package: Microsoft.Net.Compilers.Toolset<br>
    For: .NET Framework projects only<br>
    Method: Globals.FindFrameworkToolsetPackageCompiler()

#### How to Bring Dependencies to Runtime

- Option A: Automatic Download (Programmatic)
    ```c#
    // In your application startup
    NugetPackageDownloader.OnProgressOutput = Console.WriteLine; // optional
    NugetPackageDownloader.DownloadLatestSdkToolset(includePrereleases: false);

    // Initialize compiler path
    Globals.csc = Globals.FindSdkToolsetPackageCompiler() ?? 
                  Globals.FindSdKCompiler();
    ```
- Option B: NuGet Package Reference
  Note: the code below will only install the package in the build environment but does not copy them into the build folder. 
  If you want to aggregate the package binaries for distribution you will need to do it manually from the environment NuGet store.
    ```xml
    <!-- Add to your .csproj -->
    <ItemGroup>
      <PackageReference Include="Microsoft.Net.Sdk.Compilers.Toolset" 
                        Version="10.0.*" 
                        PrivateAssets="all" />
      <PackageReference Include="microsoft.AspNetCore.app.ref" 
                        Version="10.0.*" 
                        PrivateAssets="all" />
     <PackageReference Include="microsoft.netcore.app.ref" 
                        Version="10.0.*" 
                        PrivateAssets="all" />
    </ItemGroup>
    ```
- Option C: Manual Deployment
  1. Download from: https://www.nuget.org/packages/Microsoft.Net.Sdk.Compilers.Toolset/ (as well as the other *.ref packages)
  2. Extract to: 
        - `*Sdk.Compilers.Toolset` - bin\compilers\ or C:\Program Files\dotnet\packs\{package name}\{version}\ref\net{version}\
        - `*.app.ref` - C:\Program Files\dotnet\packs\{package name}\{version}\ref\net{version}\
  3. Set path (if it is not standard NuGet location):
     `Globals.csc = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "compilers", "csc.exe");`
     `Globals.csc_AsmRefs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetCore.app.ref");`
     Or set environment variables `css_csc_file` and `css_csc_asm_refs_dir`   


### Runtime Resolution Algorithm

```C#
// Globals.csc initialization logic
Globals.csc = 
    Globals.FindSdkToolsetPackageCompiler() ??  // 1. NuGet package
    Globals.FindSdKCompiler() ??                // 2. .NET SDK
    throw new Exception("C# compiler not found");
```

### Key Differences: CodeDom vs Roslyn

|Feature	|CodeDom	|Roslyn|
|:--- |:---	|:---|
|Compiler	|External csc.exe	|In-process API|
|Dependencies	|❌ Requires SDK/Package	|✅ NuGet only|
|Deployment	|Complex	|Simple|
|Performance	|Process spawn overhead	|Fast|
|Use |Case	Legacy, .NET Framework	|Modern, recommended|

Production |Deployment Checklist
- [ ] Reference package in Client.NET472.csproj (Option B)
- [ ] OR Implement auto-download (Option A)
- [ ] OR Bundle csc.exe manually (Option C)
- [ ] Verify csc is set correctly
- [ ] Test on target environment (no SDK assumption)
- [ ] Consider switching to Roslyn for simpler deployment

### Example: Full Setup

```c#
static void PrepareCodeDomCompilers()
{
    // Strategy 1: Try NuGet package first
    Globals.csc = Globals.FindSdkToolsetPackageCompiler(includePrereleases: false);
    
    // Strategy 2: Fallback to .NET SDK
    if (Globals.csc == null)
        Globals.csc = Globals.FindSdKCompiler();
    
    // Strategy 3: Download if missing
    if (Globals.csc == null)
    {
        NugetPackageDownloader.OnProgressOutput = Console.WriteLine; // optional: for progress feedback
        NugetPackageDownloader.DownloadLatestSdkToolset(includePrereleases: false);
        Globals.csc = Globals.FindSdkToolsetPackageCompiler();
    }
    
    if (Globals.csc == null)
        throw new Exception("Cannot locate C# compiler. Install .NET SDK or Microsoft.Net.Sdk.Compilers.Toolset package.");
}
```

### Bottom Line

CodeDom = External Compiler Dependency
- ✅ Works when SDK installed
- ⚠️ Requires NuGet package for runtime-only deployments
- 🔄 Consider migrating to Roslyn evaluator for zero-dependency in-process compilation
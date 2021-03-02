# CS-Script.Core
.NET Core port of CS-Script. Currently this repository is only for source code, release and issue management. The full wiki documentation can be found at https://github.com/oleg-shilo/cs-script.


CS-Script.Core has some dramatic improvements comparing to the .NET Full. However there are some limitations associated with .NET Core being a young and constantly evolving platform. 

Also, some of the early CS-Script features, which demonstrated little traction with the developers have been deprecated. See Limitations section 

<hr/>

_**For the roadmap for .NET 5 development see this [article](https://github.com/oleg-shilo/cs-script/wiki/Roadamap).**_
<hr/>
 
### Limitations

#### Imposed by .NET Core:
  - No support for script app.config file
    Support for custom app.config files is not available for .NET Core due to the API limitations
  - No building "*.exe" (to be fixed in .NET5 edition)

#### CS-Script obsolete features, limitations and constrains:
  - All scripts are compiled with debug symbols generated.
    _This has no practical impact on user experience though._
  - No surrogate process support (`//css_host`) for x86 vs. x64 execution
    _The CPU-specific engine executable must be used._
  - No support for deprecated settings:
    - Settings.DefaultApartmentState 
    - Settings.UsePostProcessor
    - Settings.TargetFramework
    - Settings.CleanupShellCommand
    - Settings.DoCleanupAfterNumberOfRuns
  - No automatic elevation via arg '-elevate'<br>
    _Elevation must be done via shell_
    
----


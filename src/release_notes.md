# Release v4.13.1.0

---

## Changes

### CLI
- #432: "Restoring packages..." hangs
- Added compiler `NET10_0_OR_GREATER` and `NET10` for .NET10 scripts.
- Added public ConsoleExtensions
  ```c#
  Console.Print("Running on .NET 10 or greater", DarkGreen);
  Console.Print("Hello");
  var name = Console.Prompt("Enter your name: ", DarkYellow);
  ```

### CSScriptLib
- no changes
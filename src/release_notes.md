# Release v4.8.21.0

---

## Changes

### CLI
- Rebuild for .NET 9.0
- Secondary "start build server" commands are made asynchronous to match the primary `css -server:start` behaver. The impacted commands are:
  - `css -servers:start`
  - `css -server_r:start`

### CSScriptLib
- Rebuild for .NET 9.0

using System;
using System.IO;
using System.Reflection;

var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

var engine = Path.Combine(dir, "cscs.dll");
AppDomain.CurrentDomain.ExecuteAssembly(engine, args);


#region Licence...
//-----------------------------------------------------------------------------
// Date:	10/11/04    Time: 3:00p
// Module:	Form1.cs
// Classes:	CSExecutionClient
//			AppInfo
//
// This module contains the definition of the CSExecutionClient class. Which implements 
// compiling C# code and executing 'Main' method of compiled assembly
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2016 Oleg Shilo
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
// and associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial 
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------
#endregion

using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace csscript
{
	delegate void PrintDelegate(string msg);

	/// <summary>
	/// Wrapper class that runs CSExecutor witin console application context.
	/// </summary>
	class CSExecutorClient
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) 
		{
			CSExecutor.Execute(args, new PrintDelegate(Print));
		}
		
		static void Print(string msg)
		{
			MessageBox.Show(msg, "cs-script");
		}
	}
	/// <summary>
	/// Repository for application specific data
	/// </summary>
	class AppInfo
	{
		public static string appName = "cswscript";
		public static string appLogo = "C# Script execution engine;\nCopyright (C) 2004 Oleg Shilo.\n";
		public static string appParamsHelp = "";
	}
}

using System;

using System.IO; 
using System.Data; 
using System.Data.SqlClient;
using System.Xml;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;

class Script
{
	const string usage = "Usage: cscscript importData file srever.database.table username.pw ...\nImports data from specified file in SQL Server table.\nFirst row in a file is a list of filed names.\n";

	static public void Main(string[] args)
	{
		if (args.Length != 3 || 
			(args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help")))
		{
			Console.WriteLine(usage);
		}
		else
		{
			string[] connectInfo = args[1].Split(".".ToCharArray(), args[1].Length / 2 + 1);
			string[] loginInfo = args[2].Split(".".ToCharArray(), args[1].Length / 2 + 1);
			if (connectInfo.Length == 3 && loginInfo.Length == 2)
			{
				ImportData(args[0], connectInfo, loginInfo);
			}
			else
			{
				Console.WriteLine("Error: some parameters are incorrect\n" + usage);
			}
		}
	}

	static void ImportData(string file, string[] connectInfo, string[] loginInfo)
	{
		string server = connectInfo[0];
		string dbName = connectInfo[1];
		string tableName = connectInfo[2];
		string userID = loginInfo[0];
		string pw = loginInfo[1];
		
		
		SqlConnection mySqlConnection = new SqlConnection("server="+server+";Trusted_Connection=false;database="+dbName+";Pwd="+pw+";User ID="+userID+";");
		mySqlConnection.Open();
				
		using (StreamReader sr = new StreamReader(file)) 
		{
			String line = sr.ReadLine();
			if (line != null)
			{
				//the first line contains field names
				if (CreateTable(dbName, tableName, GetItems(line), mySqlConnection))
				{
					Console.WriteLine("Importing data to " + tableName);
					while ((line = sr.ReadLine()) != null) 
					{
						if (InsertDataInTable(dbName, tableName, GetItems(line), mySqlConnection))
						{
							Console.Write(".");
						}
						else
						{
							Console.WriteLine("\nData could not b imported into table " + tableName);
							break;
						}
					}
					Console.WriteLine("\n\nData from\n"+file+"\nhas been imported to\n"+server+"."+dbName+".dbo."+tableName+" table." );
				}
			}
		}
	}	

	static bool CreateTable(string dbName, string tableName, string[] fields, SqlConnection connection)
	{
		try
		{
			//create table
			ExecuteCommand("USE master", connection);
			ExecuteCommand("IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'"+dbName+"') " +
							"	CREATE DATABASE ["+dbName+"]  ", connection);
			ExecuteCommand("USE " + dbName, connection);
			ExecuteCommand("IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].["+tableName+"]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) " +
							"	DROP TABLE [dbo].["+tableName+"] ", connection);
			
			string crteateTableSQL =	
			"CREATE TABLE [dbo].["+tableName+"] ( ";
			for (int i = 0; i < fields.Length; i++)
			{
				crteateTableSQL += "["+fields[i]+"] [nvarchar] (255) COLLATE Latin1_General_CI_AS NULL " + ((i+1 < fields.Length) ? ", " : "");
			}
			crteateTableSQL += ") ON [PRIMARY]";
			ExecuteCommand(crteateTableSQL, connection);
			
			Console.WriteLine("Table " + tableName + " created");
			return true;
		}
		catch(Exception e)
		{
			Console.WriteLine("Cannot create table " + tableName + "\n" + e);
		}
		return false;
	}

	static bool InsertDataInTable(string dbName, string tableName, string[] values, SqlConnection connection)
	{
		try
		{
			//insert data into table table
			ExecuteCommand("USE " + dbName, connection);
			
			string insertDataSQL =
			"INSERT INTO " + tableName +
			" VALUES ( ";
			for (int i = 0; i < values.Length; i++)
			{
				insertDataSQL += "'" + values[i] + "'" + ((i+1 < values.Length) ? ", " : "");
			}
			insertDataSQL += ")";
			ExecuteCommand(insertDataSQL, connection);
			
			return true;
		}
		catch(Exception e)
		{
			Console.WriteLine("Cannot insert data into table " + tableName + "\n" + e);
		}
		return false;
	}

	static public void ExecuteCommand(string myExecuteQuery, SqlConnection myConnection) 
	{
		SqlCommand myCommand = new SqlCommand(myExecuteQuery, myConnection);
		myCommand.ExecuteNonQuery();
	}

	static public void ExecuteSQLFile(string sqlFile, string server) 
	{
		Process myProcess = new Process();
		myProcess.StartInfo.FileName = "osql.exe";
		myProcess.StartInfo.Arguments = "-S " + server + " -U sa -P sadmin -n -i " + sqlFile;
		myProcess.Start();
		myProcess.WaitForExit();
	}

	static string[] GetItems(string line)
	{	
		return line.Split(",".ToCharArray(), line.Length / 2 + 1);
	}		
}


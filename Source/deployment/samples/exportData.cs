using System;
using System.IO; 
using System.Data; 
using System.Data.SqlClient;
using System.Xml;

class Script
{
	const string usage = "Usage: exportData.cs server.database.table username.pw file [separator.verbose] ...\nExports data from SQL Server table to specified file.\n"; 

	static public void Main(string[] args)
	{
		if (args.Length < 3 || 
			(args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))) 
		{
			Console.WriteLine(usage);
		}
		else
		{
			try
			{
				string[] connectInfo = args[0].Split(".".ToCharArray(), args[1].Length / 2 + 1);
				string[] loginInfo = args[1].Split(".".ToCharArray(), args[1].Length / 2 + 1); 
				string[] optInfo = null;
				if (args.Length == 4)
					optInfo = args[3].Split(".".ToCharArray(), args[3].Length / 2 + 1);

				if (connectInfo.Length == 3 && loginInfo.Length == 2)
				{
					ExportData(args[2], connectInfo, loginInfo, optInfo);
				}
				else
				{
					Console.WriteLine("Error: some parameters are incorrect\n" + usage); 
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}

	static void ExportData(string file, string[] connectInfo, string[] loginInfo, string[] optInfo)
	{
		string server = connectInfo[0];
		string dbName = connectInfo[1];
		string tableName = connectInfo[2];
		string userID = loginInfo[0];
		string pw = loginInfo[1];
  		string separator = ",";
		bool verbose = true;
	
		if (optInfo != null)
		{
			if (optInfo[0] != "" && optInfo[0] != " ")
				separator = optInfo[0];
			verbose = optInfo[1].ToUpper() == "Y";
		}

		if (verbose)
		{
			Console.WriteLine("\n--- start verbose --- "); 
			Debugging(loginInfo);
			Debugging(connectInfo);
			if (optInfo != null)
				Debugging(optInfo); 
			Console.WriteLine("--- start verbose --- \n");	
		}
	
		SqlConnection mySqlConnection = new SqlConnection("server="+server+";Trusted_Connection=false;database="+ dbName +";Pwd=" + pw + ";User ID=" + userID + ";"); 
		mySqlConnection.Open();
	
		SqlDataAdapter mySqlDataAdapter = new SqlDataAdapter("SELECT * FROM "+tableName, mySqlConnection);
		
		try
		{
			if (verbose)
				Console.WriteLine("Exporting data to " + file);

			DataSet myDataSet = new DataSet();

			mySqlDataAdapter.Fill(myDataSet, tableName);

			string[] fieldNames = new string[myDataSet.Tables[tableName].Columns.Count];
			for (int i = 0; i < fieldNames.Length; i++)
			{
				fieldNames[i] = myDataSet.Tables[tableName].Columns[i].ColumnName;
			}

			using (StreamWriter sw = new StreamWriter(file)) 
			{
				string line = CombineValues(fieldNames, separator);
				sw.WriteLine(line); 

				foreach (DataRow myDataRow in myDataSet.Tables[tableName].Rows)
				{
					line = "";
					for (int i = 0; i < fieldNames.Length; i++)
					{
						// line += ((i != 0) ? ";" : "") + myDataRow[fieldNames[i]].ToString(); 
						line += ((i != 0) ? separator : "") + myDataRow[fieldNames[i]].ToString();
					}
					// debug
					// Console.WriteLine(line);
					sw.WriteLine(line);
				}
			}
			
			if (verbose) 
				Console.WriteLine("\n\nData from\n"+server+"."+dbName+".dbo."+tableName+"\nhas been exported to\n"+file+" file." );

		}
		catch(Exception e)
		{
			Console.WriteLine(e.ToString());
		}
		finally
		{
			if (mySqlConnection.State == ConnectionState.Open)
				mySqlConnection.Close();
		}
	} 

	static string CombineValues(string[] values, string separator)
	{
		string line = null;
		foreach (string val in values)
		{
			line += val.Trim() + separator;
		}
		line = line.Remove (line.Length - 1, 1);
		return line;
	}


	static void Debugging(string[] values)
	{
		for (int i = 0; i < values.Length; i++) 
		{
			Console.WriteLine("Debugging parameter: " + values[i]);
		}
	}
}

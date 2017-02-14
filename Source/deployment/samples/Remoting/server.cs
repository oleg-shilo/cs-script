using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;

public class CountryList : MarshalByRefObject
{
	private String[] Countries = {"Spain","France","Italy"};

	public String[] GetList()
	{
		return Countries;
	}

	public void AddCountry(string data)
	{
		List<string> list = new List<string>();
		list.AddRange(Countries);
		list.Add(data); 
		Countries = list.ToArray();
		Console.WriteLine("Added: "+data);
	}
}


class Script
{
	[STAThread]
	static public void Main(string[] args)
	{
		ChannelServices.RegisterChannel(new HttpChannel(8086));
		RemotingConfiguration.ApplicationName = "MyRemotingApp";
		RemotingConfiguration.RegisterWellKnownServiceType(typeof(CountryList),
															"CountryList",
															WellKnownObjectMode.Singleton);

		Console.WriteLine("Press [Enter] to exit...");
		Console.ReadLine();
	}
}
//css_pre soapsuds(http://localhost:8086//MyRemotingApp/CountryList?WSDL, CountryList, -new); 
//css_ref CountryList.dll;
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;

class Script
{
	static public void Main(string[] args)
	{
		CountryList cLst = (CountryList)Activator.GetObject(typeof(CountryList), 
															  "http://localhost:8086/CountryList",
															  WellKnownObjectMode.Singleton);
		cLst.AddCountry("Australia");
	}
}

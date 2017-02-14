//css_pre elevate(); 
using System;
using System.ServiceModel;

namespace HelloService
{
    class program
    {
        static void Main(string[] args)
        {
            Uri baseUri = new Uri("http://localhost/hello");
            ServiceHost hService = new ServiceHost(typeof(HelloService), baseUri);
            hService.AddServiceEndpoint(typeof(HelloService), new BasicHttpBinding(), baseUri);
            hService.Open();
            Console.WriteLine("Service started...");
            Console.ReadLine();
            hService.Close();
        }
    }
    [ServiceContract]
    class HelloService
    {
        [OperationContract]
        string SayHello()
        {
            return ("Hello!");
        }
    }
}

using System;

namespace MyCompany
{
    public interface IHost
    {
        void Log(string message);
    }
    
    public interface IPlugin
    {
        void Init(IHost host);
        void Log(string message);
    }
}
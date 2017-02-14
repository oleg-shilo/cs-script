//css_nuget Caliburn.Micro,Caliburn.Micro.Core;
//css_ref PresentationCore;
//css_ref PresentationFramework;
//css_ref System.Core;
//css_ref System.Xaml;
//css_ref System.Linq.Expressions;
//css_ref System.Xml;
//css_ref WindowsBase;

using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace Caliburn.Micro
{
    public class App
    {
        static public void Run<T>(System.Action onstartup) where T: SimpleBootstrapper, new()
        {
            var app = new Application();
            var bs = new T();
            bs.onstartup = onstartup;
            app.Run();
        }
        
        static public void Run(System.Action onstartup)
        {
            var app = new Application();
            var bs = new SimpleBootstrapper();
            bs.onstartup = onstartup;
            app.Run();
        }
        
        
        static public Window LoadWindow(string xamlFile)
        {
             using (var s = new FileStream(xamlFile, FileMode.Open))
                return (Window)XamlReader.Load(s);
        }
    }

    public class SimpleBootstrapper : BootstrapperBase
    {
        public System.Action onstartup;
        
        public SimpleBootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            onstartup();
        }
    }
}

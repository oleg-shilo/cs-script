//css_nuget Caliburn.Micro,Caliburn.Micro.Core;
//css_inc Caliburn.App.cs;
//css_ref System.Runtime
//css_ref System.ObjectModel
using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Caliburn.Micro;

public class Program
{
    public static void Main()
    {
        App.Run(() =>
        {
            var view = App.LoadWindow("hello.xaml");
            var model = new HelloViewModel();

            ViewModelBinder.Bind(model, view, null);

            view.ShowDialog();
        });
    }
}

public class HelloViewModel : PropertyChangedBase
{
    string name;

    public string Name
    {
        get { return name; }
        set
        {
            name = value;
            NotifyOfPropertyChange(() => Name);
            NotifyOfPropertyChange(() => CanSayHello);
        }
    }

    public bool CanSayHello
    {
        get { return !string.IsNullOrWhiteSpace(Name); }
    }

    public void SayHello()
    {
        MessageBox.Show(string.Format("Hello {0}!", Name));
    }
}
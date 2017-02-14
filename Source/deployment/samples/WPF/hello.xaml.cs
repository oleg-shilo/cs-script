//css_args /provider:CSSCodeProvider.v4.0.dll 
//css_inc hello.xaml;
//css_ref WindowsBase;
//css_ref PresentationCore;
//css_ref PresentationFramework;
using System;
using System.Xaml;
using System.Windows;

public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("***** Hello World! *****");
    }

    [STAThread]
    public static void Main()
    {
        Window1 wnd = new Window1();
        wnd.ShowDialog();
    }
}
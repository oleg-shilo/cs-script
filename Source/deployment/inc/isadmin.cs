//css_args /ac, /nl
using System.Security.Principal;
using System;
using System.Diagnostics;

void main()
{
    Console.WriteLine(
        new WindowsPrincipal(WindowsIdentity.GetCurrent()) 
            .IsInRole(WindowsBuiltInRole.Administrator));
}
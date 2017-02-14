//css_ref PostSharp.Public.dll;
//css_ref PostSharp.Laos.dll;
using System;
using PostSharp.Laos;
using System.Threading;

[assembly: PostSharp.Laos.Test.MyOnMethodInvocationAspect(
    AttributeTargetAssemblies = "mscorlib", 
    AttributeTargetTypes = "System.Threading.*")]

namespace PostSharp.Laos.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Main is executed");
            Thread.Sleep(1000);
			Console.ReadLine();
        }
    }
}


namespace PostSharp.Laos.Test
{
    [Serializable]
    public class MyOnMethodInvocationAspect : OnMethodInvocationAspect
    {
        public override void OnInvocation(MethodInvocationEventArgs context)
        {
            Console.WriteLine("Calling {0}", context.Delegate.Method);
            context.Proceed();
        }
    }
}

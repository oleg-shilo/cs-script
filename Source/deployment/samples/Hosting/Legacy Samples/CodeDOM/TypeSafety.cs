//css_inc linq.includes.cs;
using System;
using System.Reflection;
using System.Diagnostics;
using CSScriptLibrary;

public interface IClaculator
{
    void Sum(int a, int b);
    void Multiply(int a, int b);
}

public interface ISummer
{
    void Sum(int a, int b);
}

public class Host
{
    static void Main()
    {
        Assembly assembly = CSScript.LoadCode(
            @"using System;
			  public class Claculator : IClaculator
			  {
                  public void Sum(int a, int b)
				  {
					  Console.WriteLine(a + b);
				  }
                  
                  public void Multiply(int a, int b)
				  {
					  Console.WriteLine(a * b);
				  }
			  }");

        AsmHelper script = new AsmHelper(assembly);
        object instance = script.CreateObject("Claculator");

        TypeUnsafeReflection(script, instance);
        TypeSafeInterface(instance);
        TypeSafeAlignedInterface(instance);
        TypeSafeDelegate(script, instance);
        TypeUnSafeDelegate(script, instance);
    }

    static void TypeUnsafeReflection(AsmHelper script, object instance)
    {
        script.InvokeInst(instance, "Claculator.Sum", 1, 2);
        script.InvokeInst(instance, "Claculator.Sum", 4, 7);
    }
    
    static void TypeSafeInterface(object instance)
    {
        IClaculator calc = (IClaculator)instance;
        calc.Sum(1, 2);
        calc.Sum(4, 7);
    }

    static void TypeSafeAlignedInterface(object instance)
    {
        ISummer calc = instance.AlignToInterface<ISummer>();
        calc.Sum(1, 2);
        calc.Sum(4, 7);
    }

    static void TypeSafeDelegate(AsmHelper script, object instance)
    {
        FastInvokeDelegate sumInvoker = script.GetMethodInvoker("Claculator.Sum", 0, 0); //type unsafe delegate
        Action<int, int> Sum = delegate(int a, int b) { sumInvoker(instance, a, b); }; //type safe delegate
        
        Sum(1, 2);
        Sum(4, 7);
    }
    static void TypeUnSafeDelegate(AsmHelper script, object instance)
    {
        FastInvokeDelegate Sum = script.GetMethodInvoker("Claculator.Sum", 0, 0); 
        
        //streakly speaking the following calls are type safe but they are not subject of 
        //arguments checking
        
        Sum(instance, 1, 2);
        Sum(instance, 4, 7);
        
        //incorrect number of arguments
        //Sum(instance, 4, 7, 9); //runtime - OK, compiletime - OK
        //Sum(instance, 4);       //runtime - Error, compiletime - OK
    }
}


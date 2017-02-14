// unsafe_keyword.cs
// compile with: /unsafe

//css_co /unsafe;
using System;

class UnsafeTest 
{
   // unsafe method: takes pointer to int:
   unsafe static void SquarePtrParam (int* p) 
   {
      *p *= *p;
   }

   unsafe public static void Main() 
   {
      int i = 5;
      // unsafe method: uses address-of operator (&)
      SquarePtrParam (&i);
      Console.WriteLine (i);
   }
}

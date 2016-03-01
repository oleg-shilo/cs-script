using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace CSScriptLibrary
{
    /// <summary>
    /// Extension methods for asynchronous invocation of <see cref="CSScriptLibrary.IEvaluator"/> methods 
    /// with Async and Await available in C# 5. 
    /// </summary>
    public static class EvaluatorAsync
    {
        //good reading: http://blog.stephencleary.com/2012/02/async-and-await.html
        //              https://msdn.microsoft.com/en-us/library/hh873177(v=vs.110).aspx

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.CompileCode"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     Assembly script = await CSScript.Evaluator
        ///                                     .CompileCodeAsync(
        ///                                                  @"using System;
        ///                                                    public class Calc
        ///                                                    {
        ///                                                        public int Sum(int a, int b)
        ///                                                        {
        ///                                                            return a+b;
        ///                                                        }
        ///                                                    }");
        ///     dynamic calc = script.CreateObject("*");
        ///
        ///     textBox1.Text = calc.Sum(3, 2).ToString();
        /// }
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptText">The C# code.</param>
        /// <returns></returns>
        public static async Task<Assembly> CompileCodeAsync(this IEvaluator evaluator, string scriptText)
        {
            return await Task.Run(() => evaluator.CompileCode(scriptText));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.CompileMethod"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///      Assembly script = await CSScript.Evaluator
        ///                                      .CompileMethodAsync(
        ///                                                   @"int Sum(int a, int b)
        ///                                                     {
        ///                                                         return a+b;
        ///                                                     }");
        ///      dynamic calc = script.CreateObject("*");
        ///      
        ///      textBox1.Text = calc.Sum(3, 7).ToString();
        /// }
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        public static async Task<Assembly> CompileMethodAsync(this IEvaluator evaluator, string code)
        {
            return await Task.Run(() => evaluator.CompileMethod(code));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.CreateDelegate{T}"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     var product = await CSScript.Evaluator
        ///                                 .CreateDelegateAsync&lt;int&gt;(
        ///                                           @"int Product(int a, int b)
        ///                                             {
        ///                                                 return a * b;
        ///                                             }");
        ///
        ///     textBox1.Text = product(3, 2).ToString();        
        /// }
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The C# script text.</param>
        /// <returns></returns>
        public static async Task<MethodDelegate<T>> CreateDelegateAsync<T>(this IEvaluator evaluator, string code)
        {
            return await Task.Run(() => evaluator.CreateDelegate<T>(code));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.CreateDelegate"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     var log = await CSScript.Evaluator
        ///                             .CreateDelegateAsync(
        ///                                 @"void Log(string message)
        ///                                   {
        ///                                       Console.WriteLine(message);
        ///                                   }");
        ///
        ///     log("Test message");
        /// }
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The C# script text.</param>
        /// <returns></returns>
        public static async Task<MethodDelegate> CreateDelegateAsync(this IEvaluator evaluator, string code)
        {
            return await Task.Run(() => evaluator.CreateDelegate(code));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadCode"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     dynamic calc = await CSScript.Evaluator
        ///                                  .LoadCodeAsync(
        ///                                           @"using System;
        ///                                             public class Script
        ///                                             {
        ///                                                 public int Sum(int a, int b)
        ///                                                 {
        ///                                                     return a+b;
        ///                                                 }
        ///                                             }");
        ///                                             
        ///     textBox1.Text = calc.Sum(1, 2).ToString();
        /// }
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="args">The non default type <c>T</c> constructor arguments.</param>
        /// <returns></returns>
        public static async Task<object> LoadCodeAsync(this IEvaluator evaluator, string scriptText, params object[] args)
        {
            return await Task.Run(() => evaluator.LoadCode(scriptText, args));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadCode{T}"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     ICalc calc = await CSScript.Evaluator
        ///                                .LoadCodeAsync&lt;ICalc&gt;(
        ///                                           @"using System;
        ///                                             public class Script
        ///                                             {
        ///                                                 public int Sum(int a, int b)
        ///                                                 {
        ///                                                     return a+b;
        ///                                                 }
        ///                                             }");
        ///     textBox1.Text = calc.Sum(1, 2).ToString();
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="args">The non default type <c>T</c> constructor arguments.</param>
        /// <returns></returns>
        public static async Task<T> LoadCodeAsync<T>(this IEvaluator evaluator, string scriptText, params object[] args) where T : class
        {
            return await Task.Run(() => evaluator.LoadCode<T>(scriptText, args));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadFile{T}"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     ICalc script = await CSScript.Evaluator
        ///                                  .LoadFileAsync&lt;ICalc&gt;("calc.cs");
        ///
        ///     textBox1.Text = script.Sum(1, 2).ToString();
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptFile">The C# script file.</param>
        /// <returns></returns>
        public static async Task<T> LoadFileAsync<T>(this IEvaluator evaluator, string scriptFile) where T : class
        {
            return await Task.Run(() => evaluator.LoadFile<T>(scriptFile));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadFile"/>.
        /// </summary>
        /// <example>
        ///<code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     dynamic script = await CSScript.Evaluator
        ///                                    .LoadFileAsync("calc.cs");
        ///
        ///     textBox1.Text = script.Sum(1, 2).ToString();
        /// }
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptFile">The C# script file.</param>
        /// <returns></returns>
        public static async Task<object> LoadFileAsync(this IEvaluator evaluator, string scriptFile)
        {
            return await Task.Run(() => evaluator.LoadFile(scriptFile));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadMethod"/>.
        /// </summary>
        /// <example><code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     ICalc script = await CSScript.Evaluator
        ///                                  .LoadMethodAsync&lt;ICalc&gt;(
        ///                                       @"public int Sum(int a, int b)
        ///                                         {
        ///                                             return a + b;
        ///                                         }
        ///                                         public int Div(int a, int b)
        ///                                         {
        ///                                             return a/b;
        ///                                         }");
        ///
        ///    textBox1.Text = script.Div(15, 3).ToString();
        ///}
        /// </code></example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        public static async Task<object> LoadMethodAsync(this IEvaluator evaluator, string code)
        {
            return await Task.Run(() => evaluator.LoadMethod(code));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadMethod{T}"/>.
        /// </summary>
        /// <example><code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     ICalc script = await CSScript.Evaluator
        ///                                  .LoadMethodAsync&lt;ICalc&gt;(
        ///                                      @"public int Sum(int a, int b)
        ///                                        {
        ///                                            return a + b;
        ///                                        }
        ///                                        public int Div(int a, int b)
        ///                                        {
        ///                                            return a/b;
        ///                                        }");
        ///
        ///    textBox1.Text = script.Div(15, 3).ToString();
        ///}
        /// </code></example>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        public static async Task<T> LoadMethodAsync<T>(this IEvaluator evaluator, string code) where T : class
        {
            return await Task.Run(() => evaluator.LoadMethod<T>(code));
        }

        /// <summary>
        /// Asynchronous version of <see cref="CSScriptLibrary.IEvaluator.LoadDelegate{T}"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// async void button1_Click(object sender, EventArgs e)
        /// {
        ///     var product = await CSScript.Evaluator
        ///                                 .LoadDelegateAsync&lt;Func&lt;int, int, int&gt;&gt;(
        ///                                  @"int Product(int a, int b)
        ///                                    {
        ///                                        return a * b;
        ///                                    }");
        ///
        ///     textBox1.Text = product(3, 2).ToString();
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        public static async Task<T> LoadDelegateAsync<T>(this IEvaluator evaluator, string code) where T : class
        {
            return await Task.Run(() => evaluator.LoadDelegate<T>(code));
        }
    }
}
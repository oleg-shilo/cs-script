/* Copyright(c) 2009, Rubenhak
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *   -> Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *
 *   -> Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 */

/*
 * 29.4.09 Changes to the original implementation by Oleg Shilo.
 *  - Added: optional referencing to the additional dependency assemblies
 *  - Changed: add assembly location instead of assembly name when adding referenced assemblies to compileUnit.ReferencedAssemblies
 */

using System;
using csscript;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

/*
 * This file is the part of Rubenhak.Utils library.
 * New versions are be found at http://rubenhak.com/.
 */

namespace CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils
{
    /// <summary>
    ///     A utility class which converts an object to specified interface T. The object does
    ///     not necessarily need to implement the interface formally.
    /// </summary>
    /// <typeparam name="T">Interface definition to convert to.</typeparam>
    /// <remarks>
    /// <para>
    ///     Class Information:
    ///	    <list type="bullet">
    ///         <item name="authors">Authors: Ruben Hakopian</item>
    ///         <item name="date">March 2009</item>
    ///         <item name="originalURL">http://rubenhak.com/?p=167</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public static class ObjectCaster<T>
                        where T : class
    {
        /// <summary>
        /// Represents a map from a object type to the type of its proxy class.
        /// </summary>
        static Dictionary<Type, Type> _proxyCache = new Dictionary<Type, Type>();

        /// <summary>
        /// Converts specified object to an another object which implements interface T.
        /// </summary>
        /// <param name="o">The object to convert.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional dependency assemblies
        /// the interface depends in. </param>
        /// <returns>Converted object if succeeded; null otherwise.</returns>
        public static T As(object o, params string[] refAssemblies)
        {
            Type interfaceType = typeof(ObjectCaster<T>).GetGenericArguments()[0];
            Type sourceType = o.GetType();

            if (!interfaceType.IsInterface)
                throw new ArgumentException("The generic type for ObjectCaster should be interface only.");

            if ((interfaceType.Attributes & TypeAttributes.Public) == 0 && (interfaceType.Attributes & TypeAttributes.NestedPublic) == 0)
                throw new ArgumentException("The interface should have a public access.");

            if (o == null)
                return null;

            if (o is T)
                return o as T;

            if (CheckSourceObject(interfaceType, sourceType, refAssemblies))
                return CastToInterface(interfaceType, o, refAssemblies);

            return null;
        }

        /// <summary>
        /// Checks if the source type can be convert to destination interface type.
        /// </summary>
        /// <param name="interfaceType">The source type to convert from.</param>
        /// <param name="sourceType">The destination interface type to convert to.</param>
        /// <param name="refAssemblies">The string array containing file nemes to the additional dependency assemblies
        /// the interface depends in. </param>
        /// <returns>true if conversion can be performed; false otherwise</returns>
        static bool CheckSourceObject(Type interfaceType, Type sourceType, params string[] refAssemblies)
        {
            // Check Properties
            foreach (PropertyInfo prop in interfaceType.GetProperties())
            {
                // Checking existence of property
                PropertyInfo sourceProp = sourceType.GetProperty(prop.Name);
                if (sourceProp == null)
                    return false;

                // Checking property type
                if (prop.PropertyType != sourceProp.PropertyType)
                    return false;

                // Checking getter existence
                if (prop.CanRead && !sourceProp.CanRead)
                    return false;

                // Checking setter existence
                if (prop.CanWrite && !sourceProp.CanWrite)
                    return false;

                // Checking indexer
                ParameterInfo[] origParams = prop.GetIndexParameters();
                ParameterInfo[] sourceParams = sourceProp.GetIndexParameters();

                if (origParams.Length != sourceParams.Length)
                    return false;

                for (int i = 0; i < origParams.Length; i++)
                {
                    if (origParams[i].ParameterType != sourceParams[i].ParameterType)
                        return false;
                }
            }

            // Check Methods
            foreach (MethodInfo method in interfaceType.GetMethods())
            {
                // Skipping special methods(property get_xxx and set_xxx methods)
                if ((method.Attributes & MethodAttributes.SpecialName) != 0)
                    continue;

                // building list of method parameter types
                List<Type> types = new List<Type>();
                ParameterInfo[] iParams = method.GetParameters();
                foreach (ParameterInfo param in iParams)
                {
                    types.Add(param.ParameterType);
                }

                // Checking method existence
                MethodInfo sourceMethod = sourceType.GetMethod(method.Name, types.ToArray());

                if (sourceMethod == null)
                    return false;

                // Checking return type
                if (sourceMethod.ReturnType != method.ReturnType)
                    return false;

                // Checking out and ref parameters
                ParameterInfo[] sourceParams = method.GetParameters();

                if (iParams.Length != sourceParams.Length)
                    return false;

                for (int i = 0; i < iParams.Length; i++)
                {
                    if (iParams[i].IsIn != sourceParams[i].IsIn)
                        return false;

                    if (iParams[i].IsOut != sourceParams[i].IsOut)
                        return false;
                }
            }

            // Check Events
            foreach (EventInfo eventInfo in interfaceType.GetEvents())
            {
                // Checking event existence
                EventInfo sourceEvent = sourceType.GetEvent(eventInfo.Name);
                if (sourceEvent == null)
                    return false;

                // Checking event handler type
                if (sourceEvent.EventHandlerType != eventInfo.EventHandlerType)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates an instance of an proxy object which implements interface T and wraps specified object.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="o">The object to convert.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional dependency assemblies
        /// the interface depends in. </param>
        /// <returns>Instance of the proxy object.</returns>
        static T CastToInterface(Type interfaceType, object o, params string[] refAssemblies)
        {
            Type sourceType = o.GetType();

            Type proxyType = null;
            if (_proxyCache.ContainsKey(sourceType))
            {
                proxyType = _proxyCache[sourceType];
            }
            else
            {
                proxyType = BuildProxyClass(interfaceType, sourceType, true, refAssemblies);
                _proxyCache[sourceType] = proxyType;
            }

            var constructors = proxyType.GetConstructors();
            //var proxyObj = constructors[0].Invoke(new object[] { o });
            return Activator.CreateInstance(proxyType, o) as T;
        }

        /// <summary>
        /// Builds the proxy class for specified interface and source types.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="injectNamespace">if set to <c>true</c> the <c>CSScriptLibrary.DynamicTypes</c>
        /// will be injected the proxy class name.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional dependency assemblies
        /// the interface depends in.</param>
        /// <returns>Type definition for proxy class.</returns>
        static Type BuildProxyClass(Type interfaceType, Type sourceType, bool injectNamespace, params string[] refAssemblies)
        {
            CodeCompileUnit compileUnit = GenerateProxyClass(interfaceType, sourceType, injectNamespace);
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

            if (sourceType.Assembly.Location() == "" || interfaceType.Assembly.Location() == "")
                throw new Exception("Error compiling proxy class:" + Environment.NewLine + "The dependency assembly is dynamic and doesn't have an assembly " +
                   "file associated with it. You will need to ensure the all engaged assemblies are loaded from the file system or " +
                   "script assemblies are compiled with CSScript.GlobalSettings.InMemoryAssembly set to `false`. Another alternative is " +
                   "to avoid duck typing by directly inheriting the sourceType from the interfaceType.");

            compileUnit.ReferencedAssemblies.Add(sourceType.Assembly.Location());
            compileUnit.ReferencedAssemblies.Add(interfaceType.Assembly.Location());
            compileUnit.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location());

            foreach (var asmFile in refAssemblies)
                compileUnit.ReferencedAssemblies.Add(asmFile);

            // Generate the code (for testing)
            using (StringWriter wr = new StringWriter())
            {
                provider.GenerateCodeFromCompileUnit(compileUnit, wr, new CodeGeneratorOptions());
                string code = wr.ToString();
            }

            // Compile the code
            var res = provider.CompileAssemblyFromDom(new CompilerParameters(), compileUnit);
            if (res.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in res.Errors)
                {
                    sb.AppendLine(error.Line + "\t" + error.ErrorNumber + "\t" + error.ErrorText);
                }

                throw new Exception("Error compiling proxy class:" + Environment.NewLine + sb.ToString());
            }

            return res.CompiledAssembly.GetTypes()[0];
        }

        /// <summary>
        /// Added by Oleg Shilo
        /// </summary>
        internal static string BuildProxyClassCode(Type interfaceType, Type sourceType, out string typeFuleName, bool injectNamespace)
        {
            CodeCompileUnit compileUnit = GenerateProxyClass(interfaceType, sourceType, injectNamespace, out typeFuleName);
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

            // Generate the code
            using (StringWriter wr = new StringWriter())
            {
                bool verbose = injectNamespace;

                provider.GenerateCodeFromCompileUnit(compileUnit, wr, new CodeGeneratorOptions());
                string code = wr.ToString();

                if (!verbose)
                {
                    var codeStart = code.IndexOf("using System;");
                    if (codeStart != -1)
                        code = code.Substring(codeStart)
                                   .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                                   .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine); // deflate
                }
                return code;
            }
        }

        /// <summary>
        /// Generates the proxy class.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="injectNamespace">if set to <c>true</c> the <c>CSScriptLibrary.DynamicTypes</c>
        /// will be injected the proxy class name.</param>
        /// <returns></returns>
        static CodeCompileUnit GenerateProxyClass(Type interfaceType, Type sourceType, bool injectNamespace)
        {
            string dummy;
            return GenerateProxyClass(interfaceType, sourceType, injectNamespace, out dummy);
        }

        static CodeCompileUnit GenerateProxyClass(Type interfaceType, Type sourceType, bool injectNamespace, out string typeFuleName)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            // Namespace
            //CodeNamespace ns = new CodeNamespace("Rubenhak.Utils.Gen");

            //Oleg Shilo: move "usings" into global namespace as Roslyn doesn't handle namespaces
            CodeNamespace global = new CodeNamespace();
            compileUnit.Namespaces.Add(global);
            global.Imports.Add(new CodeNamespaceImport("System"));

            CodeNamespace ns = global;

            if (injectNamespace)
            {
                ns = new CodeNamespace("CSScriptLibrary.DynamicTypes");
                compileUnit.Namespaces.Add(ns);
            }

            // Class
            string className = sourceType.Name;
            //className = className.Replace("`", "").Replace(">", "").Replace("<", ""); //zos future sanitizing for generic types
            className += "To" + interfaceType.Name + "Proxy";

            if (injectNamespace)
                typeFuleName = ns.Name + "." + className;
            else
                typeFuleName = className;

            CodeTypeDeclaration genClass = new CodeTypeDeclaration(className);
            // genClass.BaseTypes.Add(new CodeTypeReference(typeof(MarshalByRefObject))); // zos
            genClass.BaseTypes.Add(new CodeTypeReference(typeof(MarshalByRefObjectWithInfiniteLifetime)));
            genClass.BaseTypes.Add(new CodeTypeReference(interfaceType));
            ns.Types.Add(genClass);

            // Constructor
            CodeConstructor constructor = new CodeConstructor();
            genClass.Members.Add(constructor);
            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(sourceType), "obj"));
            constructor.Statements.Add(new CodeAssignStatement(
                            new CodeVariableReferenceExpression("_obj"),
                            new CodeVariableReferenceExpression("obj")
                            ));

            // Members
            genClass.Members.Add(new CodeMemberField(new CodeTypeReference(sourceType), "_obj") { Attributes = MemberAttributes.Public });

            // Properties
            GenerateProxyClassProperties(genClass, interfaceType, sourceType);

            // Methods
            GenerateProxyClassMethods(genClass, interfaceType, sourceType);

            // Events
            GenerateProxyClassEvents(genClass, constructor, interfaceType, sourceType);

            return compileUnit;
        }

        static List<Type> GetInterfaceTypes(Type interfaceType)
        {
            List<Type> retval = new List<Type>();
            retval.Add(interfaceType);
            retval.AddRange(interfaceType.GetInterfaces());
            return retval;
        }

        /// <summary>
        /// Generates the proxy class properties.
        /// </summary>
        /// <param name="genClass">The generated class.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="sourceType">Type of the source.</param>
        static void GenerateProxyClassProperties(CodeTypeDeclaration genClass, Type interfaceType, Type sourceType)
        {
            List<string> processed = new List<string>();

            foreach (Type type in GetInterfaceTypes(interfaceType)) //zos
                foreach (PropertyInfo prop in type.GetProperties())
                {
                    if (processed.Contains(prop.Name))
                        continue;

                    processed.Add(prop.Name);

                    CodeMemberProperty genProp = new CodeMemberProperty();
                    genProp.Name = prop.Name;
                    genProp.Type = new CodeTypeReference(prop.PropertyType);
                    genProp.HasGet = prop.CanRead;
                    genProp.HasSet = prop.CanWrite;
                    genProp.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                    ParameterInfo[] indexParams = prop.GetIndexParameters();
                    foreach (ParameterInfo param in indexParams)
                    {
                        genProp.Parameters.Add(new CodeParameterDeclarationExpression(param.ParameterType, param.Name));
                    }

                    if (prop.CanRead)
                    {
                        if (indexParams.Length == 0)
                        {
                            genProp.GetStatements.Add(new CodeMethodReturnStatement(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("_obj"),
                                    prop.Name)));
                        }
                        else
                        {
                            CodeArrayIndexerExpression indexExpr =
                                new CodeArrayIndexerExpression(
                                    new CodeVariableReferenceExpression("_obj"));

                            foreach (ParameterInfo param in indexParams)
                                indexExpr.Indices.Add(new CodeVariableReferenceExpression(param.Name));

                            genProp.GetStatements.Add(new CodeMethodReturnStatement(indexExpr));
                        }
                    }

                    if (prop.CanWrite)
                    {
                        if (indexParams.Length == 0)
                        {
                            genProp.SetStatements.Add(
                                new CodeAssignStatement(
                                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("_obj"), prop.Name),
                                    new CodeVariableReferenceExpression("value")
                                    ));
                        }
                        else
                        {
                            CodeArrayIndexerExpression indexExpr =
                                new CodeArrayIndexerExpression(
                                    new CodeVariableReferenceExpression("_obj"));

                            foreach (ParameterInfo param in indexParams)
                                indexExpr.Indices.Add(new CodeVariableReferenceExpression(param.Name));

                            genProp.SetStatements.Add(
                                new CodeAssignStatement(
                                    indexExpr,
                                    new CodeVariableReferenceExpression("value")
                                    ));
                        }
                    }

                    genClass.Members.Add(genProp);
                }
        }

        /// <summary>
        /// Generates the proxy class methods.
        /// </summary>
        /// <param name="genClass">The generated class.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="sourceType">Type of the source.</param>
        static void GenerateProxyClassMethods(CodeTypeDeclaration genClass, Type interfaceType, Type sourceType)
        {
            List<string> processed = new List<string>();

            foreach (Type type in GetInterfaceTypes(interfaceType)) //zos
                foreach (MethodInfo method in type.GetMethods())
                {
                    if ((method.Attributes & MethodAttributes.SpecialName) != 0)
                        continue;

                    if (processed.Contains(method.Name))
                        continue;

                    processed.Add(method.Name);

                    CodeMemberMethod genMethod = new CodeMemberMethod();
                    genMethod.Name = method.Name;
                    genMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    genMethod.ReturnType = new CodeTypeReference(method.ReturnType);

                    CodeMethodReturnStatement returnStatement = null;
                    CodeMethodInvokeExpression genMethodStatement = new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression("_obj"), method.Name);

                    if (method.ReturnType != typeof(void))
                    {
                        returnStatement = new CodeMethodReturnStatement(genMethodStatement);
                    }

                    foreach (ParameterInfo param in method.GetParameters())
                    {
                        FieldDirection dir = FieldDirection.In;
                        Type paramType;
                        if (param.ParameterType.IsByRef)
                        {
                            paramType = param.ParameterType.GetElementType();
                            if (param.IsOut)
                            {
                                dir = FieldDirection.Out;
                            }
                            else
                            {
                                dir = FieldDirection.Ref;
                            }
                        }
                        else
                        {
                            paramType = param.ParameterType;
                        }

                        genMethod.Parameters.Add(
                            new CodeParameterDeclarationExpression(paramType, param.Name) { Direction = dir }
                            );

                        genMethodStatement.Parameters.Add(new CodeDirectionExpression(dir, new CodeArgumentReferenceExpression(param.Name)));
                    }

                    if (returnStatement == null)
                    {
                        genMethod.Statements.Add(genMethodStatement);
                    }
                    else
                    {
                        genMethod.Statements.Add(returnStatement);
                    }

                    genClass.Members.Add(genMethod);
                }
        }

        /// <summary>
        /// Generates the proxy class events.
        /// </summary>
        /// <param name="genClass">The generated class.</param>
        /// <param name="constructor">The generated class constructor.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="sourceType">Type of the source.</param>
        static void GenerateProxyClassEvents(CodeTypeDeclaration genClass, CodeConstructor constructor, Type interfaceType, Type sourceType)
        {
            List<string> processed = new List<string>();

            foreach (Type type in GetInterfaceTypes(interfaceType)) //zos
                foreach (EventInfo eventInfo in type.GetEvents())
                {
                    if (processed.Contains(eventInfo.Name))
                        continue;

                    processed.Add(eventInfo.Name);

                    // Event Definition
                    CodeMemberEvent genEvent = new CodeMemberEvent();
                    genEvent.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    genEvent.Name = eventInfo.Name;
                    genEvent.Type = new CodeTypeReference(eventInfo.EventHandlerType);

                    genClass.Members.Add(genEvent);

                    // Create event handler
                    CodeMemberMethod genMethod = new CodeMemberMethod();
                    genMethod.Name = "On" + eventInfo.Name;
                    genMethod.Attributes = MemberAttributes.Private | MemberAttributes.Final;
                    genMethod.ReturnType = new CodeTypeReference(typeof(void));

                    CodeDelegateInvokeExpression genEventDalegate = new CodeDelegateInvokeExpression(new CodeVariableReferenceExpression("eventDelegate"));

                    foreach (ParameterInfo paramInfo in eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters())
                    {
                        FieldDirection dir = FieldDirection.In;
                        Type paramType;
                        if (paramInfo.ParameterType.IsByRef)
                        {
                            paramType = paramInfo.ParameterType.GetElementType();
                            if (paramInfo.IsOut)
                            {
                                dir = FieldDirection.Out;
                            }
                            else
                            {
                                dir = FieldDirection.Ref;
                            }
                        }
                        else
                        {
                            paramType = paramInfo.ParameterType;
                        }

                        genMethod.Parameters.Add(new CodeParameterDeclarationExpression(paramType, paramInfo.Name) { Direction = dir });

                        if (paramInfo.ParameterType == typeof(object) && paramInfo.Name == "sender" && !paramInfo.ParameterType.IsByRef)
                        {
                            genEventDalegate.Parameters.Add(new CodeThisReferenceExpression());
                        }
                        else
                        {
                            genEventDalegate.Parameters.Add(new CodeDirectionExpression(dir, new CodeArgumentReferenceExpression(paramInfo.Name)));
                        }
                    }

                    genMethod.Statements.Add(new CodeVariableDeclarationStatement(eventInfo.EventHandlerType,
                                                                                    "eventDelegate",
                                                                                    new CodeVariableReferenceExpression(eventInfo.Name)));

                    genMethod.Statements.Add(new CodeConditionStatement(
                                                    new CodeBinaryOperatorExpression(
                                                        new CodeVariableReferenceExpression("eventDelegate"),
                                                        CodeBinaryOperatorType.IdentityInequality,
                                                        new CodePrimitiveExpression(null)
                                                        ),
                                                        new CodeExpressionStatement(genEventDalegate)
                                                        ));

                    genClass.Members.Add(genMethod);

                    // Subscribe to source event
                    constructor.Statements.Add(
                        new CodeAttachEventStatement(
                            new CodeEventReferenceExpression(
                                new CodeVariableReferenceExpression("_obj"),
                                                                    eventInfo.Name),
                                new CodeMethodReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    genMethod.Name
                                )));
                }
        }
    }
}
Option Infer On
Option Strict On

Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports CSScriptLibrary
Imports CSScriptClass = CSScriptLibrary.CSScript

Namespace Global.CSScriptNativeApi

    Public Class CodeDom_Roslyn

        Public Shared Sub Test()
            CSScriptClass.GlobalSettings.UseAlternativeCompiler = LocateRoslynCSSProvider()
            CSScriptClass.GlobalSettings.RoslynDir = LocateRoslynCompilers()

            Dim myCode As String = "static void SayHello(string greeting) {
                                        var tuple = (1, 2);
                                        void test() {
                                            Console.WriteLine(""Hello from C#7!"");
                                            Console.WriteLine(tuple);
                                        }
                                        test();
                                        Console.WriteLine(greeting);
                                    }"
            Dim sayHello = CSScriptClass.LoadMethod(myCode).GetStaticMethod()
            sayHello("Hello World!")
        End Sub

        Private Shared ReadOnly Property Root() As String
            Get
                Return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            End Get
        End Property

        Private Shared Function LocateRoslynCompilers() As String
            Try
                ' Roslyn compilers are distributed as a NuGet package but the binaries are never copied into the output folder.
                ' Thus the path tho the compilers needs to be discovered dynamically.
                ' Takes the highest version if multiple packages are found.  

                Dim packageDir As String = Path.Combine(Root, "..", "..", "..", "packages")
                packageDir = Path.GetFullPath(packageDir)

                Dim roslynDir As String = Nothing
                Dim roslynDirs As String() = Directory.GetDirectories(packageDir, "Microsoft.Net.Compilers.*").ToArray()
                If roslynDirs.Length > 1 Then
                    roslynDir = (From e In roslynDirs Select New PackageInfo(e)).OrderByDescending(Function(x) x).First().FullPath
                Else
                    roslynDir = roslynDirs(0)
                End If
                Return Path.Combine(roslynDir, "tools")
            Catch
                Throw New Exception("Cannot locate Roslyn compiler (csc.exe). You can set it manually ")
            End Try
        End Function

        Private Shared Function LocateRoslynCSSProvider() As String
            Return Path.Combine(Root, "CSSRoslynProvider.dll")
        End Function


        '-----------------------------------------------------------------------------------------------------------------------
        ' Inner Class: PackageInfo
        '-----------------------------------------------------------------------------------------------------------------------

        Private Class PackageInfo
            Implements IComparable(Of PackageInfo)

            'Private Fields
            Private _Name As String
            Private _NameAndVersion As String
            Private _Version As Version

            'Constructors

            Public Sub New(directoryPath As String)
                If (directoryPath Is Nothing) Then Throw New ArgumentNullException(NameOf(directoryPath))
                FullPath = directoryPath
            End Sub

            'Public Properties


            Public ReadOnly Property Name() As String
                Get
                    'Return from cache
                    Dim myResult As String = _Name
                    If myResult IsNot Nothing Then
                        Return myResult
                    End If
                    'Initialize
                    Dim myPair As Tuple(Of [String], Version) = ParseNameAndVersion(NameAndVersion)
                    _Name = myPair.Item1
                    _Version = myPair.Item2
                    Return myPair.Item1
                End Get
            End Property

            Public ReadOnly Property Version() As Version
                Get
                    'Return from cache
                    Dim myResult As Version = _Version
                    If myResult IsNot Nothing Then
                        Return myResult
                    End If
                    'Initialize
                    Dim myPair As Tuple(Of [String], Version) = ParseNameAndVersion(NameAndVersion)
                    _Name = myPair.Item1
                    _Version = myPair.Item2
                    Return myPair.Item2
                End Get
            End Property

            Public ReadOnly Property FullPath() As String

            'Public Methods

            Public Function CompareTo(other As PackageInfo) As Int32 Implements IComparable(Of PackageInfo).CompareTo
                If other Is Nothing Then Return 1
                Dim myResult As Integer = StringComparer.InvariantCultureIgnoreCase.Compare(Name, other.Name)
                If myResult <> 0 Then Return myResult
                Return Version.CompareTo(other.Version)
            End Function

            'Private Properties

            Private ReadOnly Property NameAndVersion() As String
                Get
                    Dim myResult As String = _NameAndVersion
                    If myResult Is Nothing Then
                        Try
                            myResult = Path.GetFileName(FullPath)
                        Catch
                            myResult = ""
                        End Try
                        _NameAndVersion = myResult
                    End If
                    Return myResult
                End Get
            End Property

            'Private Methods

            Private Shared Function ParseNameAndVersion(nameAndVersion As String) As Tuple(Of [String], Version)
                Debug.Assert(nameAndVersion IsNot Nothing)
                Dim myTokens As String() = nameAndVersion.Split("."c)
                Dim myVersion As Version = Nothing
                Dim myName As [String] = Nothing
                If myTokens.Length > 3 Then
                    Try
                        'Return name and version separately
                        myVersion = New Version(Int32.Parse(myTokens(myTokens.Length - 3)), Int32.Parse(myTokens(myTokens.Length - 2)), Int32.Parse(myTokens(myTokens.Length - 1)))
                        myName = [String].Join(".", myTokens.Take(myTokens.Length - 3))
                        Return New Tuple(Of [String], Version)(myName, myVersion)
                    Catch
                    End Try
                End If
                'Default to name without version
                myVersion = New Version(0, 0, 0)
                myName = nameAndVersion
                Return New Tuple(Of [String], Version)(myName, myVersion)
            End Function

        End Class

    End Class

End Namespace

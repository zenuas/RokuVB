Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Node
Imports Roku.Util.Extensions


Namespace Parser

    Public Class Loader

        Public Overridable Property CurrentDirectory As String
        Public Overridable Property Root As New RootNode
        Public Overridable ReadOnly Property Assemblies As New List(Of Assembly)

        Public Overridable Function GetExactFullPath(name As String) As String

            Dim fname = Path.GetFileName(name)
            For Each f In New FileInfo(name).Directory.GetFiles

                If String.Compare(fname, f.Name, True) = 0 Then Return f.FullName
            Next

            Throw New FileNotFoundException
        End Function

        Public Overridable Function GetRelativePath(name As String, current As String) As String

            Return If(name.Length > current.Length AndAlso name.StartsWith(current, True, Nothing) AndAlso name(current.Length) = Path.DirectorySeparatorChar, name.Substring(current.Length + 1), name)
        End Function

        Public Overridable Function GetFileName(name As String) As String

            If name.Length = 0 Then Return name
            If File.Exists(name) Then Return Me.GetExactFullPath(name)
            If Path.GetExtension(name).Length = 0 AndAlso File.Exists($"{name}.rk") Then Return Me.GetExactFullPath($"{name}.rk")

            Throw New FileNotFoundException
        End Function

        Public Overridable Function FileNameToNamespace(name As String) As String

            Dim fn = Me.GetRelativePath(Me.GetFileName(name), Me.CurrentDirectory)
            If fn.Length = 0 Then Return fn
            Return Path.GetFileNameWithoutExtension(fn).Replace(Path.DirectorySeparatorChar, "."c)
        End Function

        Public Overridable Function AddUse(name As String) As String

            Return Util.Errors.These(
                    Function()
                        Me.LoadModule(name)
                        Return Me.FileNameToNamespace(name)
                    End Function,
                    Function() name
                )

        End Function

        Public Overridable Sub LoadModule(name As String)

            Me.AddNode(Me.FileNameToNamespace(name),
                Function()

                    Using reader As New StreamReader(Me.GetFileName(name))

                        Return Me.Parse(reader)
                    End Using
                End Function)
        End Sub

        Public Overridable Sub LoadModule(ns As String, reader As TextReader)

            Me.AddNode(ns, Function() Me.Parse(reader))
        End Sub

        Public Overridable Function Parse(reader As TextReader) As ProgramNode

            Dim parser As New MyParser With {.Loader = Me}
            Dim lex As New MyLexer(reader) With {.Parser = parser}

            Return Util.Errors.Logging(Function() CType(parser.Parse(lex), ProgramNode),
                Sub(ex As SyntaxErrorException)

                    Dim src = lex.ReadLine
                    Console.WriteLine(ex.Message)
                    Console.WriteLine(src)
                    If lex.StoreToken IsNot Nothing Then

                        Dim store = CType(lex.StoreToken, Token)
                        Dim indent = src.Substring(0, Math.Max(store.LineColumn.Value - 1, 0)).FoldLeft(Function(acc, c) If(c = Convert.ToChar(9), ((acc + 1) \ 8 + If((acc + 1) Mod 8 > 0, 1, 0)) * 8, acc + 1), 0)
                        Console.Write("".PadLeft(indent))
                        Console.WriteLine("".PadLeft(If(store.Name Is Nothing, 1, store.Name.Length), "~"c))
                    End If
                End Sub)
        End Function

        Public Overridable Function AddNode(ns As String, node As Func(Of ProgramNode)) As ProgramNode

            If Not Me.Root.Namespaces.ContainsKey(ns) Then

                Me.Root.Namespaces(ns) = Nothing
                Me.Root.Namespaces(ns) = node()
            End If

            Return Me.Root.Namespaces(ns)
        End Function

        Public Overridable Function LoadAssembly(path As String) As Assembly

            Dim asm = If(File.Exists(path), Assembly.LoadFrom(path), Me.LoadWithPartialName(path))
            If Not Me.Assemblies.Contains(asm) Then Me.Assemblies.Add(asm)
            Return asm
        End Function

#Disable Warning BC40000
        Public Overridable Function LoadWithPartialName(dll As String) As Assembly

            Return Assembly.LoadWithPartialName(dll)
        End Function
    End Class

End Namespace

Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Node
Imports Roku.Util

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

            Return name
        End Function

        Public Overridable Function GetNamespace(name As String) As String

            Dim fn = Me.GetRelativePath(Me.GetFileName(name), Me.CurrentDirectory)
            If fn.Length = 0 Then Return fn
            Return Path.GetFileNameWithoutExtension(fn).Replace(Path.DirectorySeparatorChar, "."c)
        End Function

        Public Overridable Sub LoadModule(name As String)

            If Me.Assemblies.FindFirstOrNull(Function(x) x.GetName.Name.Equals(name)) IsNot Nothing Then Return

            Me.AddNode(name,
                Function()

                    Using reader As New StreamReader(Me.GetFileName(name))

                        Return Me.Parse(reader)
                    End Using
                End Function)
        End Sub

        Public Overridable Sub LoadModule(name As String, reader As TextReader)

            Me.AddNode(name, Function() Me.Parse(reader))
        End Sub

        Public Overridable Function Parse(reader As TextReader) As ProgramNode

            Dim parser As New MyParser With {.Loader = Me}
            Dim lex As New MyLexer(reader) With {.Parser = parser}

            Return Util.Errors.Logging(Function() CType(parser.Parse(lex), ProgramNode),
                Sub(ex As SyntaxErrorException)

                    Console.WriteLine(ex.Message)
                    Console.WriteLine(lex.ReadLine)
                    If lex.StoreToken IsNot Nothing Then

                        Dim store = CType(lex.StoreToken, Token)
                        Console.Write("".PadLeft(store.LineColumn.Value - 1))
                        Console.WriteLine("".PadLeft(If(store.Name Is Nothing, 1, store.Name.Length), "~"c))
                    End If
                End Sub)
        End Function

        Public Overridable Function AddNode(name As String, node As Func(Of ProgramNode)) As ProgramNode

            Dim ns = Me.GetNamespace(name)
            If Not Me.Root.Namespaces.ContainsKey(ns) Then

                Me.Root.Namespaces(ns) = Nothing
                Me.Root.Namespaces(ns) = node()
            End If

            Return Me.Root.Namespaces(ns)
        End Function

        Public Overridable Function LoadAssembly(path As String) As Assembly

            Dim asm = Assembly.Load(path)
            If Not Me.Assemblies.Contains(asm) Then Me.Assemblies.Add(asm)
            Return asm
        End Function
    End Class

End Namespace

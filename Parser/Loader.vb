Imports System
Imports System.IO
Imports Roku.Node

Namespace Parser

    Public Class Loader

        Public Overridable Property CurrentDirectory As String
        Public Overridable Property Root As New RootNode

        Public Overridable Function GetFileName(name As String) As String

            If name.Length = 0 Then Return name
            If File.Exists(name) Then Return Path.GetFullPath(name)
            If Path.GetExtension(name).Length = 0 AndAlso File.Exists($"{name}.rk") Then Return Path.GetFullPath($"{name}.rk")

            Throw New FileNotFoundException
        End Function

        Public Overridable Function GetNamespace(name As String) As String

            Dim fn = Me.GetFileName(name)
            If fn.Length = 0 Then Return fn
            Return Path.GetFileNameWithoutExtension(fn)
        End Function

        Public Overridable Function LoadModule(name As String) As ProgramNode

            Me.AddNode(name,
                Function()

                    Using reader As New StreamReader(Me.GetFileName(name))

                        Return Me.Parse(reader)
                    End Using
                End Function)

            Return Me.Root.Namespaces(Me.GetNamespace(name))
        End Function

        Public Overridable Function LoadModule(name As String, reader As TextReader) As ProgramNode

            Me.AddNode(name, Function() Me.Parse(reader))

            Return Me.Root.Namespaces(Me.GetNamespace(name))
        End Function

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
    End Class

End Namespace

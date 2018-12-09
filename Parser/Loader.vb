Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Node
Imports Roku.Util.Functions


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

                    Dim fname = Me.GetFileName(name)
                    Using reader As New StreamReader(fname)

                        Return Tee(Me.Parse(reader), Sub(x) x.FileName = fname)
                    End Using
                End Function)
        End Sub

        Public Overridable Sub LoadModule(ns As String, reader As TextReader)

            Me.AddNode(ns, Function() Tee(Me.Parse(reader), Sub(x) x.FileName = ns))
        End Sub

        Public Overridable Function Parse(reader As TextReader) As ProgramNode

            Dim parser As New MyParser With {.Loader = Me}
            Dim lex As New MyLexer(reader) With {.Parser = parser}

            Return CType(parser.Parse(lex), ProgramNode)
        End Function

        Public Overridable Function AddNode(ns As String, node As Func(Of ProgramNode)) As ProgramNode

            If Not Me.Root.Namespaces.ContainsKey(ns) Then

                Me.Root.Namespaces(ns) = Nothing
                Dim pgm = node()
                pgm.Uses.Add(New UseNode With {.Namespace = New VariableNode("#Sys")})
                Me.Root.Namespaces(ns) = pgm
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

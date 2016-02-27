Imports System
Imports Roku.Node

Namespace Parser

    Public Class Loader

        Public Overridable Property CurrentDirectory As String
        Public Overridable Property Root As New RootNode

        Public Overridable Function GetNamespace(name As String) As String

            Return name
        End Function

        Public Overridable Sub AddNode(name As String, node As Func(Of ProgramNode))

            Dim ns = Me.GetNamespace(name)
            If Me.Root.Namespaces.ContainsKey(ns) Then Return

            Me.Root.Namespaces(ns) = Nothing
            Me.Root.Namespaces(ns) = node()
        End Sub
    End Class

End Namespace

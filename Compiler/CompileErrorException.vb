Imports System
Imports Roku.Node


Namespace Compiler

    Public Class CompileErrorException
        Inherits Exception

        Public Overridable ReadOnly Property Node As INode

        Public Sub New(node As INode, message As String)
            Me.New(node, message, Nothing)

        End Sub

        Public Sub New(node As INode, message As String, inner As Exception)
            MyBase.New($"({node.LineNumber}, {node.LineColumn}): {message}", inner)

            Me.Node = node
        End Sub

    End Class

End Namespace

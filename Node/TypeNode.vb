Imports System.Collections.Generic


Namespace Node

    Public Class TypeNode
        Inherits TypeBaseNode
        Implements IEvaluableNode


        Public Sub New()

            Me.Name = ""
        End Sub

        Public Sub New(name As VariableNode)
            MyBase.New(name)

        End Sub

        Public Sub New(ns As TypeNode, name As VariableNode)
            Me.New(name)

            ns.IsNamespace = True
            Me.Namespace = ns
        End Sub

        Public Overridable Property [Namespace] As TypeNode = Nothing
        Public Overridable Property Arguments As New List(Of TypeBaseNode)
        Public Overridable Property IsNamespace As Boolean = False
        Public Overridable Property IsTypeClass As Boolean = False

        Public Overrides Function ToString() As String

            Return If(Me.Type Is Nothing, Me.Name, Me.Type.ToString)
        End Function
    End Class

End Namespace

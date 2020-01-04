Imports System.Collections.Generic
Imports Roku.Util.Extensions


Namespace Node

    Public Class UnionNode
        Inherits TypeBaseNode


        Public Sub New(name As VariableNode, items As ListNode(Of TypeBaseNode))

            Me.Name = name.Name
            Me.Union = items
            Me.AppendLineNumber(name)
        End Sub

        Public Sub New(items As ListNode(Of TypeBaseNode))

            Me.Union = items
            Me.AppendLineNumber(items)
        End Sub

        Public Sub New(base As TypeBaseNode)

            Me.Name = base.Name
            Me.Union = New ListNode(Of TypeBaseNode)
            Me.IsGeneric = base.IsGeneric
            Me.AppendLineNumber(base)
        End Sub

        Public Overridable ReadOnly Property Union As ListNode(Of TypeBaseNode)
        Public Overridable ReadOnly Property Generics As New List(Of TypeBaseNode)
        Public Overridable Property Dynamic As Boolean = False

        Public Overrides Function HasGeneric() As Boolean

            Return Me.IsGeneric OrElse Me.Union.List.Or(Function(x) x.HasGeneric)
        End Function

        Public Overrides Function ToString() As String

            Return $"{MyBase.ToString()}{If(Me.Dynamic, "..", "")}"
        End Function

    End Class

End Namespace

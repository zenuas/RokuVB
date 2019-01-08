Imports Roku.Util


Namespace Node

    Public Class TypeTupleNode
        Inherits TypeBaseNode


        Public Sub New(items As ListNode(Of TypeBaseNode))

            Me.Items = items
            Me.AppendLineNumber(items)
        End Sub

        Public Sub New(name As VariableNode)

            Me.Name = name.Name
            Me.Items = New ListNode(Of TypeBaseNode)
            Me.Items.AppendLineNumber(name)
            Me.AppendLineNumber(name)
        End Sub

        Public Sub New(name As VariableNode, items As ListNode(Of TypeBaseNode))

            Me.Name = name.Name
            Me.Items = items
            Me.AppendLineNumber(name)
        End Sub

        Public Overridable Property Items As ListNode(Of TypeBaseNode)

        Public Overrides Function HasGeneric() As Boolean

            Return Me.Items.List.Or(Function(x) x.HasGeneric)
        End Function

    End Class

End Namespace

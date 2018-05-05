Imports Roku.Util


Namespace Node

    Public Class TypeTupleNode
        Inherits TypeNode


        Public Sub New(items As ListNode(Of TypeNode))

            Me.Items = items
            Me.AppendLineNumber(items)
        End Sub

        Public Overridable Property Items As ListNode(Of TypeNode)

        Public Overrides Function HasGeneric() As Boolean

            Return Me.Items.List.Or(Function(x) x.HasGeneric)
        End Function

    End Class

End Namespace

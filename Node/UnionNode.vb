Imports Roku.Util.Extensions


Namespace Node

    Public Class UnionNode
        Inherits TypeNode


        Public Sub New(name As VariableNode, items As ListNode(Of TypeNode))

            Me.Name = name.Name
            Me.Union = items
            Me.AppendLineNumber(name)
        End Sub

        Public Sub New(items As ListNode(Of TypeNode))

            Me.Union = items
            Me.AppendLineNumber(items)
        End Sub

        Public Overridable ReadOnly Property Union As ListNode(Of TypeNode)

        Public Overrides Function HasGeneric() As Boolean

            Return Me.Union.List.Or(Function(x) x.HasGeneric)
        End Function

    End Class

End Namespace

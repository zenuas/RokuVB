Namespace Node

    Public Class TypeArrayNode
        Inherits TypeNode


        Public Sub New(item As TypeNode)

            Me.Item = item
            Me.AppendLineNumber(item)
        End Sub

        Public Overridable Property Item As TypeNode

        Public Overrides Function HasGeneric() As Boolean

            Return Me.Item.HasGeneric
        End Function

    End Class

End Namespace

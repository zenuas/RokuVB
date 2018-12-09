Namespace Node

    Public Class TypeArrayNode
        Inherits TypeBaseNode


        Public Sub New(item As TypeBaseNode)

            Me.Item = item
            Me.AppendLineNumber(item)
        End Sub

        Public Overridable Property Item As TypeBaseNode

        Public Overrides Function HasGeneric() As Boolean

            Return Me.Item.HasGeneric
        End Function

        Public Overrides Function ToString() As String

            Return $"[{Me.Item}]"
        End Function
    End Class

End Namespace

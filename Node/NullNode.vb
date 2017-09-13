Imports Roku.Parser
Imports Roku.Manager


Namespace Node

    Public Class NullNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(x As Token)

            Me.AppendLineNumber(x)
        End Sub

        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overrides Function ToString() As String

            Return "null"
        End Function
    End Class

End Namespace

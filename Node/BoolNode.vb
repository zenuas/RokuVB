Imports Roku.Parser
Imports Roku.Manager


Namespace Node

    Public Class BoolNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(x As Token, v As Boolean)

            Me.AppendLineNumber(x)
            Me.Value = v
        End Sub

        Public Overridable Property Value As Boolean
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overrides Function ToString() As String

            Return If(Me.Value, "true", "false")
        End Function
    End Class

End Namespace

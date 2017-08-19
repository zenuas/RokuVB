Imports Roku.Manager


Namespace Node

    Public Class ExpressionNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property [Operator] As String = ""
        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As IEvaluableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property [Function] As IFunction

        Public Overrides Function ToString() As String

            If Me.Right Is Nothing Then Return $"{Me.Operator} {Me.Left}"
            Return $"{Me.Left} {Me.Operator} {Me.Right}"
        End Function
    End Class

End Namespace

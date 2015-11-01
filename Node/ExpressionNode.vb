Imports Roku.Manager


Namespace Node

    Public Class ExpressionNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property [Operator] As String = ""
        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As IEvaluableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
    End Class

End Namespace

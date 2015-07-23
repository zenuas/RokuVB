Imports Roku.Manager


Namespace Node

    Public Class FunctionCallNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(expr As IEvaluableNode, ParamArray args() As IEvaluableNode)

            Me.Expression = expr
            Me.Arguments = args
            Me.AppendLineNumber(expr)
        End Sub

        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Arguments As IEvaluableNode()
        Public Overridable Property Type As InType Implements IEvaluableNode.Type
    End Class

End Namespace

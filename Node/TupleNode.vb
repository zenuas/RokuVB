Imports Roku.Manager
Imports Roku.Util.Extensions


Namespace Node

    Public Class TupleNode
        Inherits BaseNode
        Implements IEvaluableNode, IStatementNode


        Public Overridable Property Items As IEvaluableNode()
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overrides Function ToString() As String

            Return $"tuple ({String.Join(", ", Me.Items.Map(Function(x) x.ToString))})"
        End Function
    End Class

End Namespace

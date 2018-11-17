Imports Roku.Manager


Namespace Node

    Public Class LetNode
        Inherits BaseNode
        Implements IEvaluableNode, IStatementNode


        Public Overridable Property Receiver As IEvaluableNode
        Public Overridable Property Var As VariableNode
        Public Overridable Property [Declare] As TypeBaseNode
        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overrides Function ToString() As String

            Return $"var {Me.Var}{If(Me.Declare Is Nothing, "", $": {Me.Declare}")} = {Me.Expression}"
        End Function
    End Class

End Namespace

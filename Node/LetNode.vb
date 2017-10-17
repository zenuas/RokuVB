Imports Roku.Manager


Namespace Node

    Public Class LetNode
        Inherits BaseNode
        Implements IEvaluableNode, IStatementNode


        Public Overridable Property Receiver As IEvaluableNode
        Public Overridable Property Var As VariableNode
        Public Overridable Property [Declare] As TypeNode
        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance
    End Class

End Namespace

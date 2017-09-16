Imports Roku.Manager


Namespace Node

    Public MustInherit Class CaseNode
        Inherits BaseNode
        Implements IEvaluableNode

        Public Overridable Property [Then] As BlockNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = False Implements IEvaluableNode.IsInstance
    End Class

End Namespace

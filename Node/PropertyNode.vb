Imports Roku.Manager


Namespace Node

    Public Class PropertyNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As VariableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
    End Class

End Namespace

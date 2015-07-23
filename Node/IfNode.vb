Imports Roku.Manager


Namespace Node

    Public Class IfNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property [Condition] As IEvaluableNode = Nothing
        Public Overridable Property [Then] As IEvaluableNode = Nothing
        Public Overridable Property [Else] As IEvaluableNode = Nothing
        Public Overridable Property Type As InType Implements IEvaluableNode.Type
    End Class

End Namespace

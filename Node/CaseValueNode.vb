Namespace Node

    Public Class CaseValueNode
        Inherits BaseNode
        Implements ICaseNode

        Public Overridable Property Expression As IEvaluableNode = Nothing
        Public Overridable Property [Then] As BlockNode Implements ICaseNode.Then

    End Class

End Namespace

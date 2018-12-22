Namespace Node

    Public Class CaseValueNode
        Inherits BaseNode
        Implements ICaseNode

        Public Overridable Property Value As BlockNode = Nothing
        Public Overridable Property [Then] As BlockNode Implements ICaseNode.Then

    End Class

End Namespace

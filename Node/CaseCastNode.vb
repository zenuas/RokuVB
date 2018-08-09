Namespace Node

    Public Class CaseCastNode
        Inherits BaseNode
        Implements ICaseNode

        Public Overridable Property [Declare] As TypeBaseNode
        Public Overridable Property Var As VariableNode
        Public Overridable Property [Then] As BlockNode Implements ICaseNode.Then

    End Class

End Namespace

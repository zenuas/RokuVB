Namespace Node

    Public Interface IRunableNode
        Inherits INode

        Property Expression As IEvaluableNode
        Property [Next] As IRunableNode

    End Interface

End Namespace

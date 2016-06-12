Imports System


Namespace Node

    Public Interface INode

        Property Parent As IScopeNode
        Property LineNumber As Integer?
        Property LineColumn As Integer?

        Function Clone() As INode

    End Interface

End Namespace

Imports Roku.Parser


Namespace Node

    Public Interface INode

        Property Parent As IScopeNode
        Property LineNumber As Integer?
        Property LineColumn As Integer?

        Sub AppendLineNumber(node As INode)
        Sub AppendLineNumber(token As Token)

        Function Clone() As INode

    End Interface

End Namespace

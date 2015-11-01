Imports Roku.Node
Imports Roku.Manager


Namespace Manager

    Public Class RkValue

        Public Overridable Property Scope As IType
        Public Overridable Property Name As String
        Public Overridable Property Type As IType

        Public Shared Widening Operator CType(node As VariableNode) As RkValue

            Return New RkValue With {.Name = node.Name, .Type = node.Type}
        End Operator
    End Class

End Namespace

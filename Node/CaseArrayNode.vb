Imports System.Collections.Generic


Namespace Node

    Public Class CaseArrayNode
        Inherits BaseNode
        Implements ICaseNode

        Public Overridable Property Pattern As New List(Of VariableNode)
        Public Overridable ReadOnly Property Statements As New List(Of IStatementNode)
        Public Overridable Property [Then] As BlockNode Implements ICaseNode.Then

    End Class

End Namespace

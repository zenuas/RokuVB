Imports System.Collections.Generic


Namespace Node

    Public Class CaseArrayNode
        Inherits CaseNode

        Public Overridable Property Pattern As New List(Of VariableNode)
        Public Overridable ReadOnly Property Statements As New List(Of IStatementNode)
    End Class

End Namespace

Imports System.Collections.Generic


Namespace Node

    Public Class SwitchNode
        Inherits BaseNode
        Implements IStatementNode


        Public Overridable Property Expression As IEvaluableNode = Nothing
        Public Overridable Property [Case] As New List(Of CaseNode)
    End Class

End Namespace

Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class SwitchNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property Expression As IEvaluableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = False Implements IEvaluableNode.IsInstance
        Public Overridable Property [Case] As New List(Of CaseNode)
    End Class

End Namespace

Imports System.Text
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class ListNode(Of T As INode)
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable ReadOnly Property List As New List(Of T)
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
    End Class

End Namespace

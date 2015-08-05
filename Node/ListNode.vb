Imports System.Text
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class ListNode(Of T)
        Inherits BaseNode
        Implements IEvaluableNode


        Private list_ As New List(Of T)
        Public Overridable ReadOnly Property List As List(Of T)
            Get
                Return Me.list_
            End Get
        End Property

        Public Overridable Property Type As InType Implements IEvaluableNode.Type
    End Class

End Namespace

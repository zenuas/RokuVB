Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class ListNode(Of T As INode)
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


        Public Overridable ReadOnly Property List As New List(Of T)
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            Dim apply = CType(t, IApply).Apply(0)
            Dim fix = False
            For Each x In Me.List

                If TypeOf x Is IFeedback Then fix = CType(x, IFeedback).Feedback(apply) OrElse fix
            Next

            If CType(Me.Type, RkStruct).Apply(0) IsNot apply Then

                CType(Me.Type, RkStruct).Apply(0) = apply
                fix = True
            End If
            Return fix
        End Function

    End Class

End Namespace

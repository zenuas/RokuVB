Imports System.Collections.Generic
Imports Roku.Manager
Imports Roku.Util.Extensions


Namespace Node

    Public Class ListNode(Of T As INode)
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


        Public Overridable ReadOnly Property List As New List(Of T)
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If TypeOf t Is RkByName Then Return Me.Feedback(CType(t, RkByName).Type)

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

        Public Overrides Function ToString() As String

            Return $"[{String.Join(", ", Me.List.Take(3))}{If(Me.List.Count >= 4, ", ...", "")}]"
        End Function
    End Class

End Namespace

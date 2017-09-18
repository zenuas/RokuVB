Imports Roku.Manager


Namespace Node

    Public Class LambdaExpressionNode
        Inherits BaseNode
        Implements IEvaluableNode, IStatementNode, IFeedback


        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
            Get
                Return Me.Expression.Type
            End Get
            Set(value As IType)

                Me.Expression.Type = value
            End Set
        End Property
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If TypeOf Me.Expression Is IFeedback Then Return CType(Me.Expression, IFeedback).Feedback(t)
            Return False
        End Function

        Public Overrides Function ToString() As String

            Return $"lambda ({Me.Expression})"
        End Function
    End Class

End Namespace

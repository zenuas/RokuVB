Imports Roku.Manager
Imports Roku.Manager.SystemLibrary


Namespace Node

    Public Class PropertyNode
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As VariableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If t Is Me.Type Then Return False

            If Me.Type Is Nothing Then

                Me.Type = t
                Return True
            End If

            Return TypeFeedback(Me.Type, t)
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Left}.{Me.Right}"
        End Function
    End Class

End Namespace

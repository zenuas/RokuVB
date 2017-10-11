Imports Roku.Manager


Namespace Node

    Public Class PropertyNode
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As VariableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If TypeOf Me.Type Is RkUnionType Then

                Dim union = CType(Me.Type, RkUnionType)
                Return union.Merge(t)
            Else

                If t Is Me.Type Then Return False

                Me.Type = t
                Return True
            End If
        End Function
    End Class

End Namespace

Imports Roku.Manager


Namespace Node

    Public Class PropertyNode
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As VariableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            Dim apply = CType(t, IApply).Apply(0)
            If CType(Me.Type, IApply).Apply(0) IsNot apply Then

                CType(Me.Type, IApply).Apply(0) = apply
                Return True
            End If
            Return False
        End Function
    End Class

End Namespace

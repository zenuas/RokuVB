Imports Roku.Manager


Namespace Node

    Public Class IfExpressionNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property Condition As IEvaluableNode = Nothing
        Public Overridable Property [Then] As IEvaluableNode = Nothing
        Public Overridable Property [Else] As IEvaluableNode = Nothing
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overrides Function ToString() As String

            Return $"{Me.Condition} ? {Me.Then} : {Me.Else}"
        End Function
    End Class

End Namespace

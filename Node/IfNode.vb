Namespace Node

    Public Class IfNode
        Inherits BaseNode
        Implements IStatementNode


        Public Overridable Property [Condition] As IEvaluableNode = Nothing
        Public Overridable Property [Then] As BlockNode = Nothing
        Public Overridable Property [Else] As BlockNode = Nothing

        Public Overrides Function ToString() As String

            Return $"if {Me.Condition}"
        End Function
    End Class

End Namespace

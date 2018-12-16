Namespace IntermediateCode

    Public Class InCode0

        Public Overridable Property [Operator] As InOperator = InOperator.Nop

        Public Overrides Function ToString() As String

            Return $"{Me.Operator}"
        End Function
    End Class

End Namespace

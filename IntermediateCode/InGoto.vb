Namespace IntermediateCode

    Public Class InGoto
        Inherits InCode0

        Public Sub New()

            Me.Operator = InOperator.Goto
        End Sub

        Public Overridable Property Label As InLabel

        Public Overrides Function ToString() As String

            Return $"goto {Me.Label}"
        End Function
    End Class

End Namespace

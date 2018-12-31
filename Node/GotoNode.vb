Namespace Node

    Public Class GotoNode
        Inherits BaseNode
        Implements IStatementNode


        Public Sub New()

            Me.LineNumber = 0
            Me.LineColumn = 0
        End Sub

        Public Overridable Property Label As Integer

        Public Overrides Function ToString() As String

            Return $"goto label{Me.Label}"
        End Function
    End Class

End Namespace

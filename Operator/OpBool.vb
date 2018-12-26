Namespace [Operator]

    Public Class OpBool
        Inherits OpValue

        Public Overridable Property Value As Boolean

        Public Overrides Function ToString() As String

            Return If(Me.Value, "true", "false")
        End Function

    End Class

End Namespace

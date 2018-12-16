Namespace [Operator]

    Public Class OpProperty
        Inherits OpValue

        Public Overridable Property Receiver As OpValue

        Public Overrides Function ToString() As String

            Return $"{Me.Receiver}.{Me.Name}"
        End Function
    End Class

End Namespace

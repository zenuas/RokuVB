Namespace [Operator]

    Public Class OpString
        Inherits OpValue

        Public Overridable Property [String] As String

        Public Overrides Function ToString() As String

            Return $"""{Me.String}"""
        End Function

    End Class

End Namespace

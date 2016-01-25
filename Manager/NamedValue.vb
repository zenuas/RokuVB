
Namespace Manager

    Public Class NamedValue

        Public Overridable Property Name As String
        Public Overridable Property Value As IType

        Public Overrides Function ToString() As String

            Return $"{Me.Name}: {Me.Value}"
        End Function

    End Class

End Namespace

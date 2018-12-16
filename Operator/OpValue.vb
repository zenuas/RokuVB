Imports Roku.Manager


Namespace [Operator]

    Public Class OpValue

        Public Overridable Property Scope As IType
        Public Overridable Property Name As String
        Public Overridable Property Type As IType

        Public Overrides Function ToString() As String

            Return $"{Me.Name}"
        End Function
    End Class

End Namespace

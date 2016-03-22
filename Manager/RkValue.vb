Imports Roku.Node
Imports Roku.Manager


Namespace Manager

    Public Class RkValue

        Public Overridable Property Scope As IType
        Public Overridable Property Name As String
        Public Overridable Property Type As IType

        Public Overrides Function ToString() As String

            Return $"{Me.Name}: {Me.Type}"
        End Function
    End Class

End Namespace

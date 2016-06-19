Imports Roku.Node
Imports Roku.Manager


Namespace [Operator]

    Public Class OpNull
        Inherits OpValue

        Public Overrides Function ToString() As String

            Return "null"
        End Function

    End Class

End Namespace

Imports System.Collections.Generic


Namespace [Operator]

    Public Class OpArray
        Inherits OpValue

        Public Overridable ReadOnly Property List As New List(Of OpValue)

        Public Overrides Function ToString() As String

            Return $"{Me.Type} [...]"
        End Function
    End Class

End Namespace

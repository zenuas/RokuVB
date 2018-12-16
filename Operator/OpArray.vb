Imports System.Collections.Generic
Imports Roku.Util.Extensions


Namespace [Operator]

    Public Class OpArray
        Inherits OpValue

        Public Overridable ReadOnly Property List As New List(Of OpValue)

        Public Overrides Function ToString() As String

            Return $"[{String.Join(", ", Me.List.Take(3))}{If(Me.List.Count >= 4, ", ...", "")}]"
        End Function
    End Class

End Namespace

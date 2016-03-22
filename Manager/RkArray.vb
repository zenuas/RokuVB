Imports System.Collections.Generic
Imports Roku.Manager


Namespace Manager

    Public Class RkArray
        Inherits RkValue

        Public Overridable ReadOnly Property List As New List(Of RkValue)

        Public Overrides Function ToString() As String

            Return $"{Me.Type} [...]"
        End Function
    End Class

End Namespace

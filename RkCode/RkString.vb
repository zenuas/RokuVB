Imports Roku.Node
Imports Roku.Manager


Namespace RkCode

    Public Class RkString
        Inherits RkValue

        Public Sub New(value As String, type As InType)
            MyBase.New(value, type)

            Me.String = value
        End Sub

        Public Overridable Property [String] As String

    End Class

End Namespace

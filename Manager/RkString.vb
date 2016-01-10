Imports Roku.Node
Imports Roku.Manager


Namespace Manager

    Public Class RkString
        Inherits RkValue

        Public Overridable Property [String] As String

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} ""{Me.String}"""
        End Function

    End Class

End Namespace

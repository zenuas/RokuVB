Imports System

Namespace Parser

    Public Class SyntaxErrorException
        Inherits Exception

        Public Sub New(lineno As Integer, column As Integer, message As String, ParamArray args As Object())
            MyBase.New($"parser read({lineno}, {column}): {String.Format(message, args)}")

        End Sub

    End Class

End Namespace

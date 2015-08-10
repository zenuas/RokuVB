Imports System

Namespace Parser

    Public Class SyntaxErrorException
        Inherits Exception

        Public Sub New(ByVal lineno As Integer, ByVal column As Integer, ByVal message As String, ByVal ParamArray args As Object())
            MyBase.New(String.Format("parser read({0}, {1}): {2}", lineno, column, String.Format(message, args)))

        End Sub

    End Class

End Namespace

Imports System.IO


Namespace Architecture

    Public Class SourceWriter
        Inherits StreamWriter

        Public Sub New(path As String)
            MyBase.New(path, False, System.Text.Encoding.UTF8)

            Me.CoreNewLine = Char.ConvertFromUtf32(&HA).ToCharArray
        End Sub

        Public Overridable Property DefaultIndent As Integer = 0
        Public Overridable Property IsLineHead As Boolean = True
        Public Overridable Property HeadString As String = ""

        Public Overrides Sub Write(value As String)

            If Me.IsLineHead Then

                If Me.DefaultIndent > 0 Then Me.WriteIndent(Me.DefaultIndent)
                MyBase.Write(Me.HeadString)
            End If
            MyBase.Write(value)

            Me.IsLineHead = False
        End Sub

        Public Overrides Sub WriteLine()

            If Me.IsLineHead Then

                If Me.DefaultIndent > 0 Then Me.WriteIndent(Me.DefaultIndent)
                MyBase.Write(Me.HeadString)
            End If
            MyBase.WriteLine()

            Me.IsLineHead = True
        End Sub

        Public Overrides Sub WriteLine(value As String)

            If Me.IsLineHead Then

                If Me.DefaultIndent > 0 Then Me.WriteIndent(Me.DefaultIndent)
                MyBase.Write(Me.HeadString)
            End If
            MyBase.WriteLine(value)

            Me.IsLineHead = True
        End Sub

        Public Overridable Sub WriteIndent(indent As Integer)

            For i As Integer = 0 To indent - 1

                MyBase.Write(Char.ConvertFromUtf32(&H9))
            Next
        End Sub

    End Class

End Namespace

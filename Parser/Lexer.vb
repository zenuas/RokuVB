Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace Parser

    Public MustInherit Class Lexer(Of T)

        Public Overridable Property BaseReader As TextReader

        Public Sub New(reader As TextReader)

            Me.BaseReader = reader
            Me.Initialize()
        End Sub

        Public Overridable Sub Initialize()
        End Sub

#Region "read token"

        Public Overridable Property StoreToken As IToken(Of T) = Nothing
        Public Overridable Property EndOfToken As Boolean = False

        Public Overridable Function ReadText(Optional eofmark As String = "") As String

            If Me.StoreToken IsNot Nothing Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "token buffer is not null")

            Dim text As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim line = Me.ReadLine
                If line.Equals(eofmark) Then Return text.ToString

                text.AppendLine(line)
            Loop

            If Not eofmark.Equals("") Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "missing eof mark")
            Return text.ToString
        End Function

        Public Overridable Function PeekToken() As IToken(Of T)

            If Me.StoreToken IsNot Nothing Then Return Me.StoreToken

            Dim token = Me.Reader()
            If token Is Nothing Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "Reader return no token")

            Me.StoreToken = token
            Return Me.StoreToken

        End Function

        Public Overridable Function NextToken() As IToken(Of T)

            If Me.EndOfToken Then Throw New InvalidOperationException("NextToken called end-of-token")
            Return Me.PeekToken()
        End Function

        Public Overridable Function ReadToken() As IToken(Of T)

            If Me.EndOfToken Then Throw New InvalidOperationException("ReadToken called end-of-token")

            Dim t = Me.StoreToken
            Me.StoreToken = Nothing
            If t.EndOfToken Then Me.EndOfToken = True
            Return t
        End Function

#End Region

#Region "read char"

        Public Overridable Property PeekBuffer As Integer = -1

        Public Overridable Function EndOfStream() As Boolean

            If Me.PeekBuffer < 0 Then

                Me.PeekBuffer = Me.BaseReader.Read
                If Me.PeekBuffer < 0 Then Return True
            End If
            Return False
        End Function

        Public Overridable Function PeekStream() As Integer

            If Me.EndOfStream Then Return -1
            Return Me.PeekBuffer
        End Function

        Public Overridable Function ReadStream() As Integer

            If Me.EndOfStream Then Return -1
            Dim c = Me.PeekBuffer
            Me.PeekBuffer = -1
            Return c
        End Function

        Public Overridable Function NextChar() As Char

            If Me.EndOfStream() Then Throw New InvalidOperationException("NextChar called end-of-stream")
            Return Char.ConvertFromUtf32(Me.PeekStream())(0)
        End Function

        Public Overridable Function ReadChar() As Char

            If Me.EndOfStream() Then Throw New InvalidOperationException("ReadChar called end-of-stream")
            Dim n = Me.ReadStream
            Dim c = Char.ConvertFromUtf32(n)(0)
            If n = &HA OrElse (n = &HD AndAlso Me.PeekStream() <> &HA) Then

                Me.LineColumn = 1
                Me.LineNumber += 1
            Else
                Me.LineColumn += 1
            End If
            Return c
        End Function

        Public Overridable Function ReadLine() As String

            If Not Me.EndOfStream Then

                Me.LineColumn = 1
                Me.LineNumber += 1
            End If
            Me.PeekBuffer = -1
            Return Me.BaseReader.ReadLine
        End Function

#End Region

#Region "reader"

        Public MustOverride Function CreateEndOfToken() As IToken(Of T)
        Public MustOverride Function CreateCharToken(x As SymbolTypes) As IToken(Of T)
        Public MustOverride Function CreateWordToken(x As SymbolTypes) As IToken(Of T)

        Public Overridable Function Reader() As IToken(Of T)

            If Me.EndOfStream() Then Return Me.CreateEndOfToken

            ' lex char
            Dim c = Me.ReadChar
            If Me.ReservedChar.ContainsKey(c) Then Return Me.CreateCharToken(Me.ReservedChar(c))

            Do While Char.IsWhiteSpace(c)

                If Me.EndOfStream() Then Return Me.CreateEndOfToken
                c = Me.ReadChar
            Loop

            ' lex word
            Dim s As New System.Text.StringBuilder(c.ToString())
            If Not Me.EndOfStream Then

                c = Me.NextChar
                Do While Not Char.IsWhiteSpace(c) AndAlso Not Me.ReservedChar.ContainsKey(c)

                    s.Append(c)
                    Me.ReadChar()
                    If Me.EndOfStream() Then Exit Do
                    c = Me.NextChar
                Loop
            End If

            Dim word = s.ToString
            If Me.ReservedWord.ContainsKey(word) Then Return Me.CreateWordToken(Me.ReservedWord(word))

            Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
        End Function

        Public Overridable ReadOnly Property ReservedChar As New Dictionary(Of Char, SymbolTypes)
        Public Overridable ReadOnly Property ReservedWord As New Dictionary(Of String, SymbolTypes)

#End Region

#Region "line number/column"

        Public Overridable Property LineColumn As Integer = 1
        Public Overridable Property LineNumber As Integer = 1

#End Region

    End Class

End Namespace

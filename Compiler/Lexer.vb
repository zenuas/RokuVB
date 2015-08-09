Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace Compiler

    Public MustInherit Class Lexer(Of T)

        Private reader_ As TextReader

        Public Sub New(ByVal reader As TextReader)

            Me.reader_ = reader
            Me.SetRegisterWord()
        End Sub

        Public Overridable Sub SetRegisterWord()

            Me.SetCustomRegisterWord()
        End Sub

        Protected Overridable Sub SetCustomRegisterWord()
        End Sub

#Region "read token"

        Private store_token_ As IToken(Of T) = Nothing
        Private end_of_token_ As Boolean = False

        Public Overridable Function ReadText(Optional ByVal eofmark As String = "") As String

            If Me.store_token_ IsNot Nothing Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "token buffer is not null")

            Dim text As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim line As String = Me.ReadLine
                If line.Equals(eofmark) Then Return text.ToString

                text.AppendLine(line)
            Loop

            If Not eofmark.Equals("") Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "missing eof mark")
            Return text.ToString
        End Function

        Public Overridable Function EndOfToken() As Boolean

            Return Me.end_of_token_
        End Function

        Public Overridable Function PeekToken() As IToken(Of T)

            If Me.store_token_ IsNot Nothing Then Return Me.store_token_

            Dim token As IToken(Of T) = Me.Reader()
            If token Is Nothing Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "Reader return no token")

            Me.store_token_ = token
            Return Me.store_token_

        End Function

        Public Overridable Function NextToken() As IToken(Of T)

            If Me.EndOfToken Then Throw New InvalidOperationException("NextToken called end-of-token")
            Return Me.PeekToken()
        End Function

        Public Overridable Function ReadToken() As IToken(Of T)

            If Me.EndOfToken Then Throw New InvalidOperationException("ReadToken called end-of-token")

            Dim t As IToken(Of T)
            t = Me.store_token_
            Me.store_token_ = Nothing
            If t.EndOfToken Then Me.end_of_token_ = True
            Return t
        End Function

#End Region

#Region "read char"

        Private peek_buffer_ As Integer = -1

        Public Overridable Function EndOfStream() As Boolean

            If Me.peek_buffer_ < 0 Then

                Me.peek_buffer_ = Me.reader_.Read
                If Me.peek_buffer_ < 0 Then Return True
            End If
            Return False
        End Function

        Public Overridable Function PeekStream() As Integer

            If Me.EndOfStream Then Return -1
            Return Me.peek_buffer_
        End Function

        Public Overridable Function ReadStream() As Integer

            If Me.EndOfStream Then Return -1
            Dim c As Integer = Me.peek_buffer_
            Me.peek_buffer_ = -1
            Return c
        End Function

        Public Overridable Function NextChar() As Char

            If Me.EndOfStream() Then Throw New InvalidOperationException("NextChar called end-of-stream")
            Return Char.ConvertFromUtf32(Me.PeekStream())(0)
        End Function

        Public Overridable Function ReadChar() As Char

            If Me.EndOfStream() Then Throw New InvalidOperationException("ReadChar called end-of-stream")
            Dim n As Integer = Me.ReadStream
            Dim c As Char = Char.ConvertFromUtf32(n)(0)
            If n = &HA OrElse (n = &HD AndAlso Me.PeekStream() <> &HA) Then

                Me.reader_x_ = 1
                Me.reader_y_ += 1
            Else
                Me.reader_x_ += 1
            End If
            Return c
        End Function

        Public Overridable Function ReadLine() As String

            If Not Me.EndOfStream Then

                Me.reader_x_ = 1
                Me.reader_y_ += 1
            End If
            Me.peek_buffer_ = -1
            Return Me.reader_.ReadLine
        End Function

#End Region

#Region "reader"

        Protected MustOverride Function CreateEndOfToken() As IToken(Of T)
        Protected MustOverride Function CreateCharToken(ByVal x As Integer) As IToken(Of T)
        Protected MustOverride Function CreateWordToken(ByVal x As Integer) As IToken(Of T)

        Protected Overridable Function Reader() As IToken(Of T)

            If Me.EndOfStream() Then Return Me.CreateEndOfToken

            ' lex char
            Dim c As Char = Me.ReadChar
            If Me.ReservedChar.ContainsKey(c) Then Return Me.CreateCharToken(Me.ReservedChar(c))

            Do While Char.IsWhiteSpace(c)

                If Me.EndOfStream() Then Return Me.CreateEndOfToken
                c = Me.ReadChar
            Loop

            ' lex word
            Dim s As New System.Text.StringBuilder(c.ToString())
            c = Me.ReadChar
            Do While Not Char.IsWhiteSpace(c) AndAlso Not Me.ReservedChar.ContainsKey(c)

                s.Append(c)
                If Me.EndOfStream() Then Exit Do
                c = Me.ReadChar
            Loop

            Dim word As String = s.ToString
            If Me.ReservedWord.ContainsKey(word) Then Return Me.CreateWordToken(Me.ReservedWord(word))

            Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
        End Function

        Private reserved_char_ As New Dictionary(Of Char, Integer)
        Private reserved_word_ As New Dictionary(Of String, Integer)

        Public Overridable ReadOnly Property ReservedChar() As Dictionary(Of Char, Integer)
            Get
                Return Me.reserved_char_
            End Get
        End Property

        Public Overridable ReadOnly Property ReservedWord() As Dictionary(Of String, Integer)
            Get
                Return Me.reserved_word_
            End Get
        End Property

#End Region

#Region "line number/column"

        Private reader_x_ As Integer = 1
        Private reader_y_ As Integer = 1

        Public Overridable ReadOnly Property LineNumber() As Integer
            Get
                Return Me.reader_y_
            End Get
        End Property

        Public Overridable ReadOnly Property LineColumn() As Integer
            Get
                Return Me.reader_x_
            End Get
        End Property

#End Region

    End Class

End Namespace

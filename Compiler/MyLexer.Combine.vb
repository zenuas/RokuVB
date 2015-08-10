Imports System
Imports System.Collections.Generic
Imports Roku.Node

Namespace Compiler

    Partial Public Class MyLexer

        Public Overridable Property Parser As MyParser

        Protected Overrides Sub SetCustomRegisterWord()

            Me.ReservedWord.Clear()
            Me.ReservedWord("sub") = SymbolTypes.SUB
            Me.ReservedWord("var") = SymbolTypes.LET
        End Sub

#Region "reader"

        Private eol_ As New Token(SymbolTypes.EOL)
        Private prev_ As IToken(Of INode) = Nothing
        Private next_ As IToken(Of INode) = Nothing
        Private indent_stack_ As New List(Of Integer)


        Protected Overrides Function Reader() As IToken(Of INode)

            If Me.next_ Is Nothing Then Me.next_ = Me.ReaderNext

            If Me.next_.InputToken = SymbolTypes.EOL Then

                Me.next_.Indent = 0
                If Me.indent_stack_.Count > 0 Then Me.next_.Indent = Me.indent_stack_(Me.indent_stack_.Count - 1)
                Me.prev_ = Me.next_
                Me.next_ = Nothing
                Return Me.prev_
            End If

            If Me.prev_ IsNot Nothing AndAlso
                Me.prev_.InputToken <> SymbolTypes.EOL AndAlso
                Me.prev_.InputToken <> SymbolTypes.END AndAlso
                Me.indent_stack_.Count > 0 Then

                Me.next_.Indent = Me.indent_stack_(Me.indent_stack_.Count - 1)
            End If

            If Me.indent_stack_.Count > 0 AndAlso
                (Me.next_.InputToken = SymbolTypes._END OrElse
                 Me.next_.Indent < Me.indent_stack_(Me.indent_stack_.Count - 1)) Then

                Dim block_end = Me.CreateBlockEnd(Me.indent_stack_(Me.indent_stack_.Count - 1))
                Me.indent_stack_.RemoveAt(Me.indent_stack_.Count - 1)
                Me.prev_ = block_end
                Return block_end
            End If

            If Me.next_.InputToken = SymbolTypes._END Then

                Me.next_.Indent = 0
                Me.prev_ = Me.next_
                Me.next_ = Nothing
                Return Me.prev_
            End If

            If Me.indent_stack_.Count = 0 Then

                Me.indent_stack_.Add(Me.next_.Indent)
                Return Me.CreateBlockBegin(Me.next_.Indent)
            End If

            If Me.prev_ IsNot Nothing AndAlso
                (Me.prev_.InputToken = SymbolTypes.EOL OrElse
                 Me.prev_.InputToken = SymbolTypes.BEGIN OrElse
                 Me.prev_.InputToken = SymbolTypes.END) Then

                Dim prev_indent = Me.indent_stack_(Me.indent_stack_.Count - 1)
                Dim next_indent = Me.next_.Indent

                If prev_indent = next_indent Then

                    Me.prev_ = Me.next_
                    Me.next_ = Nothing

                ElseIf prev_indent < next_indent Then

                    Me.indent_stack_.Add(next_indent)
                    Me.prev_ = Me.CreateBlockBegin(next_indent)
                Else
                    Me.indent_stack_.RemoveAt(Me.indent_stack_.Count - 1)
                    Me.prev_ = Me.CreateBlockEnd(prev_indent)
                End If

            Else
                Me.next_.Indent = Me.indent_stack_(Me.indent_stack_.Count - 1)
                Me.prev_ = Me.next_
                Me.next_ = Nothing
            End If

            Return Me.prev_
        End Function

        Protected Overridable Function ReaderNext() As IToken(Of INode)

RESTART_:
            If Me.EndOfStream() Then Return Me.CreateEndOfToken_

            ' lex char
            Dim indent = 0
            Dim c = Me.NextChar
            Do While Char.IsWhiteSpace(c)

                indent += 1
                Me.ReadChar()
                If c = Convert.ToChar(10) OrElse c = Convert.ToChar(13) Then

                    If Me.Parser.IsAccept(Me.eol_) Then Return Me.CreateEndOfLine
                    indent = 0
                End If
                If Me.EndOfStream() Then Return Me.CreateEndOfToken_
                c = Me.NextChar
            Loop
            If c = "#"c Then

                Me.ReadLineComment()
                GoTo RESTART_
            End If
            Dim lineno = Me.LineNumber
            Dim column = Me.LineColumn

            Dim x = CType(Me.ReaderToken(), Token)
            x.Indent = indent
            x.LineNumber = lineno
            x.LineColumn = column
            Return x
        End Function

        Protected Overridable Overloads Function ReaderToken() As IToken(Of INode)

            If Me.EndOfStream() Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
            Dim c = Me.ReadChar
            If Char.IsWhiteSpace(c) OrElse c = "#"c Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")

            If Me.ReservedChar.ContainsKey(c) Then Return Me.CreateCharToken(Me.ReservedChar(c))

            Dim buf As New System.Text.StringBuilder(c.ToString)

            If c = "_"c Then

                Do While Not Me.EndOfStream AndAlso Me.NextChar = "_"c

                    buf.Append(Me.ReadChar)
                Loop
                Return Me.ReadVariableFirst(buf)

            ElseIf Me.IsAlphabet(c) Then

                Return Me.ReadVariable(buf)

            ElseIf c = """"c Then

                Return Me.ReadString(c)

            ElseIf c = "="c Then

                ' =    -> Equal
                ' ===? -> Operator
                If Not Me.EndOfStream AndAlso Me.NextChar = "="c Then

                    buf.Append(Me.ReadChar())
                    If Not Me.EndOfStream AndAlso Me.NextChar = "="c Then buf.Append(Me.ReadChar())
                    Return New Token(SymbolTypes.OPE, buf.ToString)
                Else
                    Return New Token(SymbolTypes.EQ, buf.ToString)
                End If

                'ElseIf c = "."c Then

                '    If Not Me.EndOfStream AndAlso Me.NextChar = "."c Then Return New Token(SymbolTypes.DOT2)
                '    Return New Token(SymbolTypes.__x2E)

            ElseIf Me.IsOperator(c) Then

                Return Me.ReadOperator(buf)

            ElseIf Me.IsNumber(c) Then

                ' 0      -> Numeric(zero)
                ' 0[0-7] -> Numeric(oct)
                ' 0b     -> Numeric(bin)
                ' 0o     -> Numeric(oct)
                ' 0x     -> Numeric(hex)
                ' other  -> Numeric(dec)
                If c = "0"c Then

                    If Not Me.EndOfStream Then

                        If Me.NextChar = "x"c Then

                            Me.ReadChar()
                            Return Me.ReadHexadecimal()

                        ElseIf Me.NextChar = "o"c Then

                            Me.ReadChar()
                            Return Me.ReadOctal()

                        ElseIf Me.IsOctal(Me.NextChar) Then

                            Return Me.ReadOctal()

                        ElseIf Me.NextChar = "b"c Then

                            Me.ReadChar()
                            Return Me.ReadBinary()
                        End If
                    End If
                End If

                Return Me.ReadDecimal(buf)
            End If

            Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
        End Function

#End Region

#Region "type check"

        Protected Overridable Function IsNumber(c As Char) As Boolean

            Return (c >= "0"c AndAlso c <= "9"c)
        End Function

        Protected Overridable Function IsBinary(c As Char) As Boolean

            Return (c = "0"c OrElse c = "1"c)
        End Function

        Protected Overridable Function IsOctal(c As Char) As Boolean

            Return (c >= "0"c AndAlso c <= "7"c)
        End Function

        Protected Overridable Function IsHexadecimal(c As Char) As Boolean

            Return _
                ((c >= "0"c AndAlso c <= "9"c) OrElse
                (c >= "a"c AndAlso c <= "f"c) OrElse
                (c >= "A"c AndAlso c <= "F"c))
        End Function

        Protected Overridable Function IsLowerAlphabet(c As Char) As Boolean

            Return (c >= "a"c AndAlso c <= "z"c)
        End Function

        Protected Overridable Function IsUpperAlphabet(c As Char) As Boolean

            Return (c >= "A"c AndAlso c <= "Z"c)
        End Function

        Protected Overridable Function IsAlphabet(c As Char) As Boolean

            Return (Me.IsLowerAlphabet(c) OrElse Me.IsUpperAlphabet(c))
        End Function

        Protected Overridable Function IsWord(c As Char) As Boolean

            Return (c = "_"c OrElse Me.IsLowerAlphabet(c) OrElse Me.IsUpperAlphabet(c) OrElse Me.IsNumber(c))
        End Function

        Protected Overridable Function IsOperator(c As Char) As Boolean

            Return (
                c = "-"c OrElse
                c = "+"c OrElse
                c = "*"c OrElse
                c = "/"c OrElse
                c = "<"c OrElse
                c = ">"c OrElse
                c = "!"c OrElse
                c = "%"c OrElse
                c = "^"c OrElse
                c = "&"c OrElse
                c = "\"c OrElse
                c = "|"c OrElse
                c = "?"c OrElse
                c = "~"c OrElse
                c = "$"c)
        End Function

#End Region

#Region "read token"

        Protected Overridable Sub ReadLineComment()

            Me.ReadLine()
        End Sub

        Protected Overridable Function CreateEndOfLine() As IToken(Of INode)

            Return New Token(SymbolTypes.EOL)
        End Function

        Protected Overridable Function CreateEndOfToken_() As IToken(Of INode)

            Return Me.CreateEndOfToken
        End Function

        Protected Overridable Function CreateBlockBegin(indent As Integer) As IToken(Of INode)

            Return New Token(SymbolTypes.BEGIN) With {.Indent = indent}
        End Function

        Protected Overridable Function CreateBlockEnd(indent As Integer) As IToken(Of INode)

            Return New Token(SymbolTypes.END) With {.Indent = indent}
        End Function

        Protected Overridable Function ReadVariableFirst(buf As System.Text.StringBuilder) As Token

            If Me.EndOfStream Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "not variable")

            Dim c = Me.ReadChar
            If Not Me.IsAlphabet(c) Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "variable not begin with alphabetic")
            buf.Append(c)
            Return Me.ReadVariable(buf)
        End Function

        Protected Overridable Function ReadVariable(buf As System.Text.StringBuilder) As Token

            Do While Not Me.EndOfStream AndAlso Me.IsWord(Me.NextChar)

                buf.Append(Me.ReadChar)
            Loop

            Dim s = buf.ToString
            If Me.ReservedWord.ContainsKey(s) Then Return New Token(CType(Me.ReservedWord(s), SymbolTypes), s)
            Return New Token(SymbolTypes.VAR, s)
        End Function

        Protected Overridable Function ReadString(start_char As Char) As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.ReadChar
                If c = start_char Then Exit Do
                buf.Append(c)
            Loop

            Return New Token(SymbolTypes.STR, buf.ToString)
        End Function

        Protected Overridable Function ReadOperator(ByVal buf As System.Text.StringBuilder) As Token

            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If Me.IsOperator(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else

                    If Not Me.EndOfStream AndAlso Me.NextChar = "="c Then buf.Append(Me.ReadChar())
                    Exit Do
                End If
            Loop

            Return New Token(SymbolTypes.OPE, buf.ToString)
        End Function

        Protected Overridable Function ReadDecimal(buf As System.Text.StringBuilder) As Token

            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    ' nothing

                ElseIf Me.IsNumber(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If
            Loop

            Return Me.CreateNumericToken(Convert.ToUInt32(buf.ToString))
        End Function

        Protected Overridable Function ReadBinary() As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    ' nothing

                ElseIf Me.IsBinary(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If
            Loop

            Return Me.CreateNumericToken(Convert.ToUInt32(buf.ToString, 2))
        End Function

        Protected Overridable Function ReadOctal() As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    ' nothing

                ElseIf Me.IsOctal(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If
            Loop

            Return Me.CreateNumericToken(Convert.ToUInt32(buf.ToString, 8))
        End Function

        Protected Overridable Function ReadHexadecimal() As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    ' nothing

                ElseIf Me.IsHexadecimal(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If
            Loop

            Return Me.CreateNumericToken(Convert.ToUInt32(buf.ToString, 16))
        End Function

        Protected Overridable Function CreateNumericToken(n As UInt32) As Token

            Return New Token(SymbolTypes.NUM, n.ToString) With {.Value = New NumericNode(n)}
        End Function

#End Region

    End Class

End Namespace

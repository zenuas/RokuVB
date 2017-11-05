Imports System
Imports System.Collections.Generic
Imports Roku.Node

Namespace Parser

    Partial Public Class MyLexer

        Public Overridable Property Parser As MyParser

        Public Overrides Sub Initialize()

            Me.ReservedWord.Clear()
            Me.ReservedWord("sub") = SymbolTypes.SUB
            Me.ReservedWord("var") = SymbolTypes.LET
            Me.ReservedWord("struct") = SymbolTypes.STRUCT
            Me.ReservedWord("union") = SymbolTypes.UNION
            Me.ReservedWord("if") = SymbolTypes.IF
            Me.ReservedWord("else") = SymbolTypes.ELSE
            Me.ReservedWord("switch") = SymbolTypes.SWITCH
            Me.ReservedWord("use") = SymbolTypes.USE
            Me.ReservedWord("null") = SymbolTypes.NULL
        End Sub

#Region "reader"

        Public Overridable ReadOnly Property TokenStack As New List(Of IToken(Of INode))
        Public Overridable ReadOnly Property IndentStack As New List(Of Integer)


        Public Overrides Function Reader() As IToken(Of INode)

            Dim indent = 0
            If TokenStack.Count = 0 Then

READ_LINE_:
                Dim begin = Me.ReaderNext
                If begin.InputToken = SymbolTypes.EOL Then GoTo READ_LINE_

                Dim count = Me.IndentStack.Count
                indent = begin.Indent
                If count = 0 OrElse Me.IndentStack(count - 1) < indent Then

                    Me.TokenStack.Add(Me.CreateBlockBegin(indent, begin.LineNumber))
                    Me.IndentStack.Add(indent)
                Else

                    Do While count > 0 AndAlso Me.IndentStack(count - 1) > indent

                        Me.TokenStack.Insert(0, Me.CreateBlockEnd(Me.IndentStack(count - 1), begin.LineNumber))
                        Me.IndentStack.RemoveAt(count - 1)
                        count -= 1
                    Loop
                End If

                Me.TokenStack.Add(begin)
                If Not begin.EndOfToken Then

READ_CONTINUE_:
                    Do While True

                        Dim t = Me.ReaderNext
                        t.Indent = indent
                        Me.TokenStack.Add(t)
                        If t.InputToken = SymbolTypes.EOL Then Exit Do
                        If t.EndOfToken Then

                            Me.TokenStack.Insert(Me.TokenStack.Count - 1, Me.CreateEndOfLine(t.LineNumber, t.LineColumn))
                            Exit Do
                        End If
                    Loop
                End If

                If Me.TokenStack(Me.TokenStack.Count - 1).EndOfToken Then

                    count = Me.IndentStack.Count
                    Do While count > 0

                        count -= 1
                        Me.TokenStack.Insert(Me.TokenStack.Count - 1, Me.CreateBlockEnd(Me.IndentStack(count), Me.LineNumber))
                        Me.IndentStack.RemoveAt(count)
                    Loop
                End If
            End If

            Dim first = Me.TokenStack(0)
            Me.TokenStack.RemoveAt(0)
            If first.InputToken = SymbolTypes.EOL AndAlso Not Me.Parser.IsAccept(first) Then

                indent = first.Indent
                GoTo READ_CONTINUE_
            End If
            Return first
        End Function

        Public Overridable Function ReaderNext() As IToken(Of INode)

            If Me.EndOfStream() Then Return Me.CreateEndOfToken

            ' lex char
            Dim indent = 0
            Dim c = Me.NextChar
            Do While Char.IsWhiteSpace(c)

                indent += 1
                Me.ReadChar()
                If c = Convert.ToChar(13) Then

                    If Me.NextChar = Convert.ToChar(10) Then

                        Me.ReadChar()
                    End If
                End If
                If c = Convert.ToChar(10) OrElse c = Convert.ToChar(13) Then

                    Return Me.CreateEndOfLine(Me.LineNumber, Me.LineColumn)
                End If
                If Me.EndOfStream() Then Return Me.CreateEndOfToken(Me.LineNumber, Me.LineColumn)
                c = Me.NextChar
            Loop
            If c = "#"c Then

                Dim eol = Me.CreateEndOfLine(Me.LineNumber, Me.LineColumn)
                Me.ReadLineComment()
                Return eol
            End If
            Dim lineno = Me.LineNumber
            Dim column = Me.LineColumn

            Dim x = CType(Me.ReaderToken(), Token)
            x.Indent = indent
            x.LineNumber = lineno
            x.LineColumn = column
            Return x
        End Function

        Public Overridable Overloads Function ReaderToken() As IToken(Of INode)

            If Me.EndOfStream() Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
            Dim c = Me.ReadChar
            If Char.IsWhiteSpace(c) OrElse c = "#"c Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")

            If Me.ReservedChar.ContainsKey(c) Then Return Me.CreateCharToken(Me.ReservedChar(c), c)

            Dim buf As New System.Text.StringBuilder(c.ToString)

            If c = "_"c Then

                Do While Not Me.EndOfStream AndAlso Me.NextChar = "_"c

                    buf.Append(Me.ReadChar)
                Loop
                Return Me.ReadVariableFirst(buf)

            ElseIf Me.IsAlphabet(c) Then

                Return Me.ReadVariable(buf)

            ElseIf c = "@"c Then

                Dim at = Me.ReadVariableFirst(buf)
                at.Type = SymbolTypes.ATVAR
                Return at

            ElseIf c = """"c Then

                Return Me.ReadString(c)

            ElseIf c = "="c Then

                ' =    -> Equal
                ' =>   -> Arrow
                ' ===? -> Operator
                If Not Me.EndOfStream AndAlso Me.NextChar = "="c Then

                    buf.Append(Me.ReadChar())
                    If Not Me.EndOfStream AndAlso Me.NextChar = "="c Then buf.Append(Me.ReadChar())
                    Return New Token(SymbolTypes.OPE, buf.ToString)

                ElseIf Not Me.EndOfStream AndAlso Me.NextChar = ">"c Then

                    buf.Append(Me.ReadChar())
                    Return New Token(SymbolTypes.ARROW, buf.ToString)
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

        Public Overridable Function IsNumber(c As Char) As Boolean

            Return (c >= "0"c AndAlso c <= "9"c)
        End Function

        Public Overridable Function IsBinary(c As Char) As Boolean

            Return (c = "0"c OrElse c = "1"c)
        End Function

        Public Overridable Function IsOctal(c As Char) As Boolean

            Return (c >= "0"c AndAlso c <= "7"c)
        End Function

        Public Overridable Function IsHexadecimal(c As Char) As Boolean

            Return _
                ((c >= "0"c AndAlso c <= "9"c) OrElse
                (c >= "a"c AndAlso c <= "f"c) OrElse
                (c >= "A"c AndAlso c <= "F"c))
        End Function

        Public Overridable Function IsLowerAlphabet(c As Char) As Boolean

            Return (c >= "a"c AndAlso c <= "z"c)
        End Function

        Public Overridable Function IsUpperAlphabet(c As Char) As Boolean

            Return (c >= "A"c AndAlso c <= "Z"c)
        End Function

        Public Overridable Function IsAlphabet(c As Char) As Boolean

            Return (Me.IsLowerAlphabet(c) OrElse Me.IsUpperAlphabet(c))
        End Function

        Public Overridable Function IsWord(c As Char) As Boolean

            Return (c = "_"c OrElse Me.IsLowerAlphabet(c) OrElse Me.IsUpperAlphabet(c) OrElse Me.IsNumber(c))
        End Function

        Public Overridable Function IsOperator(c As Char) As Boolean

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

        Public Overridable Sub ReadLineComment()

            Me.ReadLine()
        End Sub

        Public Overridable Function CreateEndOfLine(linenum As Integer, linecolumn As Integer) As IToken(Of INode)

            Return New Token(SymbolTypes.EOL) With {.LineNumber = linenum, .LineColumn = linecolumn}
        End Function

        Public Overridable Overloads Function CreateEndOfToken(linenum As Integer, linecolumn As Integer) As IToken(Of INode)

            Dim t = Me.CreateEndOfToken
            t.LineNumber = linenum
            t.LineColumn = linecolumn
            Return t
        End Function

        Public Overridable Overloads Function CreateCharToken(x As SymbolTypes, c As Char) As IToken(Of INode)

            Return New Token(x) With {.Name = c.ToString}
        End Function

        Public Overridable Function CreateBlockBegin(indent As Integer, linenum As Integer) As IToken(Of INode)

            Return New Token(SymbolTypes.BEGIN) With {.Indent = indent, .LineNumber = linenum, .LineColumn = 0}
        End Function

        Public Overridable Function CreateBlockEnd(indent As Integer, linenum As Integer) As IToken(Of INode)

            Return New Token(SymbolTypes.END) With {.Indent = indent, .LineNumber = linenum, .LineColumn = 0}
        End Function

        Public Overridable Function ReadVariableFirst(buf As System.Text.StringBuilder) As Token

            If Me.EndOfStream Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "not variable")

            Dim c = Me.ReadChar
            If Not Me.IsAlphabet(c) Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "variable not begin with alphabetic")
            buf.Append(c)
            Return Me.ReadVariable(buf)
        End Function

        Public Overridable Function ReadVariable(buf As System.Text.StringBuilder) As Token

            Do While Not Me.EndOfStream AndAlso Me.IsWord(Me.NextChar)

                buf.Append(Me.ReadChar)
            Loop

            Dim s = buf.ToString
            If Me.ReservedWord.ContainsKey(s) Then Return New Token(CType(Me.ReservedWord(s), SymbolTypes), s)
            Return New Token(SymbolTypes.VAR, s)
        End Function

        Public Overridable Function ReadString(start_char As Char) As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.ReadChar
                If c = start_char Then Exit Do
                buf.Append(c)
            Loop

            Return New Token(SymbolTypes.STR, buf.ToString)
        End Function

        Public Overridable Function ReadOperator(buf As System.Text.StringBuilder) As Token

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

            If buf.Length = 1 AndAlso buf(0) = "|"c Then Return New Token(SymbolTypes.OR, "|")
            Return New Token(SymbolTypes.OPE, buf.ToString)
        End Function

        Public Overridable Function ReadDecimal(buf As System.Text.StringBuilder) As Token

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

        Public Overridable Function ReadBinary() As Token

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

        Public Overridable Function ReadOctal() As Token

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

        Public Overridable Function ReadHexadecimal() As Token

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

        Public Overridable Function CreateNumericToken(n As UInt32) As Token

            Return New Token(SymbolTypes.NUM, n.ToString) With {.Value = New NumericNode(n)}
        End Function

#End Region

#Region "read line"

        Public Overridable Property CurrentWord As New System.Text.StringBuilder
        Public Overridable Property CurrentLine As New System.Text.StringBuilder

        Public Overrides Function ReadChar() As Char

            If Me.EndOfStream() Then Throw New InvalidOperationException("ReadChar called end-of-stream")
            Dim n = Me.ReadStream
            Dim c = Char.ConvertFromUtf32(n)(0)
            If n = &HA OrElse (n = &HD AndAlso Me.PeekStream() <> &HA) Then

                Me.LineColumn = 1
                Me.LineNumber += 1
                Me.CurrentWord.Clear()
                Me.CurrentLine.Clear()
            Else
                Me.LineColumn += 1
                Me.CurrentWord.Append(c)
                Me.CurrentLine.Append(c)
            End If
            Return c
        End Function

        Public Overrides Function ReadLine() As String

            If Not Me.EndOfStream Then

                Me.LineColumn = 1
                Me.LineNumber += 1
            End If
            If Me.PeekBuffer >= 0 Then Me.CurrentLine.Append(Char.ConvertFromUtf32(Me.PeekBuffer)(0))
            Me.PeekBuffer = -1
            Me.CurrentLine.Append(Me.BaseReader.ReadLine)
            Dim s = Me.CurrentLine.ToString
            Me.CurrentWord.Clear()
            Me.CurrentLine.Clear()
            Return s
        End Function

#End Region

    End Class

End Namespace

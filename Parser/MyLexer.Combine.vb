Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.Extensions

Namespace Parser

    Partial Public Class MyLexer

        Public Overridable Property Parser As MyParser
        Public Overridable Property PhysicalLineColumn As Integer = 1

        Public Overrides Sub Initialize()

            Me.ReservedWord.Clear()
            Me.ReservedWord("sub") = SymbolTypes.SUB
            Me.ReservedWord("var") = SymbolTypes.LET
            Me.ReservedWord("struct") = SymbolTypes.STRUCT
            Me.ReservedWord("union") = SymbolTypes.UNION
            Me.ReservedWord("class") = SymbolTypes.CLASS
            Me.ReservedWord("if") = SymbolTypes.IF
            Me.ReservedWord("else") = SymbolTypes.ELSE
            Me.ReservedWord("then") = SymbolTypes.THEN
            Me.ReservedWord("switch") = SymbolTypes.SWITCH
            Me.ReservedWord("use") = SymbolTypes.USE
            Me.ReservedWord("null") = SymbolTypes.NULL
            Me.ReservedWord("true") = SymbolTypes.TRUE
            Me.ReservedWord("false") = SymbolTypes.FALSE
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
                    Me.ReaderTokens(indent)
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

        Public Overridable Sub ReaderTokens(indent As Integer)

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
        End Sub

        Public Overrides Function PeekToken() As IToken(Of INode)

            Dim t = MyBase.PeekToken
            If t.InputToken = SymbolTypes.OPE AndAlso Not Me.Parser.IsAccept(t) AndAlso CType(t, Token).Name.StartsWith(">") Then

                Dim ope = CType(t, Token)
                Dim gt As New Token(SymbolTypes.GT, ">") With {
                        .Indent = ope.Indent,
                        .LineNumber = ope.LineNumber,
                        .LineColumn = ope.LineColumn
                    }
                Me.StoreToken = gt

                ope.LineColumn += 1
                ope.Name = ope.Name.Substring(1)
                If ope.Name.Equals(">") Then ope.Type = SymbolTypes.GT
                Me.TokenStack.Insert(0, ope)

                t = gt

            ElseIf t.InputToken = SymbolTypes.EOL AndAlso Not Me.Parser.IsAccept(t) Then

                Me.StoreToken = Nothing
                Me.ReaderTokens(t.Indent)
                Return Me.PeekToken
            End If
            Return t

        End Function

        Public Overridable Function ReaderNext() As IToken(Of INode)

            If Me.EndOfStream() Then Return Me.CreateEndOfToken

            ' lex char
            Dim ishead = Me.PhysicalLineColumn = 1
            Dim lineno = 0
            Dim column = 0
            Dim indent = 0
            Dim c = Me.NextChar
            Do While Char.IsWhiteSpace(c)

                indent += 1
                lineno = Me.LineNumber
                column = Me.LineColumn
                Me.ReadChar()
                If c = Convert.ToChar(13) Then

                    If Me.NextChar = Convert.ToChar(10) Then

                        Me.ReadChar()
                    End If
                End If
                If c = Convert.ToChar(10) OrElse c = Convert.ToChar(13) Then

                    Return Me.CreateEndOfLine(lineno, column)
                End If
                If Me.EndOfStream() Then Return Me.CreateEndOfToken(lineno, column)
                c = Me.NextChar
            Loop
            If c = "#"c Then

                Dim eol = Me.CreateEndOfLine(Me.LineNumber, Me.LineColumn)
                Dim comment = Me.ReadLineComment()
                If ishead AndAlso comment.StartsWith("###") Then

                    Dim start_count = comment.MatchLength(Function(x) x = "#"c)

                    Do While True

                        If Me.EndOfStream() Then Return Me.CreateEndOfToken
                        If indent = Me.ReaderIndent Then

                            If start_count = Me.ReadLineComment.MatchLength(Function(x) x = "#"c) Then Exit Do
                        Else

                            Me.ReadLine()
                        End If
                    Loop
                End If
                Return eol
            End If
            lineno = Me.LineNumber
            column = Me.LineColumn

            Dim t = CType(Me.ReaderToken(), Token)
            t.Indent = indent
            t.LineNumber = lineno
            t.LineColumn = column
            Return t
        End Function

        Public Overridable Function ReaderIndent() As Integer

            If Me.EndOfStream() Then Return 0

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
                If Me.EndOfStream() Then Return indent
                If c = Convert.ToChar(10) OrElse c = Convert.ToChar(13) Then indent = 0
                c = Me.NextChar
            Loop
            Return indent
        End Function

        Public Overridable Overloads Function ReaderToken() As IToken(Of INode)

            If Me.EndOfStream() Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
            Dim c = Me.ReadChar
            If Char.IsWhiteSpace(c) OrElse c = "#"c Then Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")

            If c = "."c Then

                If Not Me.EndOfStream AndAlso Me.NextChar = "."c Then

                    Me.ReadChar()
                    Return New Token(SymbolTypes.DOT2)
                End If
            End If
            If Me.ReservedChar.ContainsKey(c) Then Return Me.CreateCharToken(Me.ReservedChar(c), c)

            Dim buf As New System.Text.StringBuilder(c.ToString)

            If c = "_"c Then

                If Me.EndOfStream OrElse (Me.NextChar <> "_"c AndAlso Not Me.IsAlphabet(Me.NextChar)) Then

                    Return New Token(SymbolTypes.IGNORE, c)
                End If

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

            ElseIf c = "$"c Then

                buf.Clear()
                If Not Me.EndOfStream Then

                    c = Me.NextChar
                    If Me.IsNoneZeroNumber(c) Then

                        Dim imp = Me.ReadDecimal(buf, Me.LineNumber, Me.LineColumn - 1)
                        Dim num = CType(imp.Value, NumericNode)
                        imp.Type = SymbolTypes.VAR
                        imp.Name = $"${num.Format}"
                        imp.Value = New ImplicitParameterNode(imp.Name, num.Numeric)
                        imp.Value.AppendLineNumber(imp)
                        Return imp
                    End If
                End If

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

            ElseIf Me.IsOperator(c) Then

                Return Me.ReadOperator(buf)

            ElseIf Me.IsNumber(c) Then

                Dim linenum = Me.LineNumber
                Dim linecol = Me.LineColumn - 1

                ' 0      -> Numeric(zero)
                ' 0[0-7] -> Numeric(oct)
                ' 0b     -> Numeric(bin)
                ' 0o     -> Numeric(oct)
                ' 0x     -> Numeric(hex)
                ' other  -> Numeric(dec)
                If c = "0"c Then

                    If Not Me.EndOfStream Then

                        If Me.NextChar = "x"c Then

                            buf.Append(Me.ReadChar())
                            Return Me.ReadHexadecimal(buf, linenum, linecol)

                        ElseIf Me.NextChar = "o"c Then

                            buf.Append(Me.ReadChar())
                            Return Me.ReadOctal(buf, linenum, linecol)

                        ElseIf Me.IsOctal(Me.NextChar) Then

                            Return Me.ReadOctal(buf, linenum, linecol)

                        ElseIf Me.NextChar = "b"c Then

                            buf.Append(Me.ReadChar())
                            Return Me.ReadBinary(buf, linenum, linecol)
                        End If
                    End If
                End If

                Return Me.ReadDecimal(buf.ToString, buf, linenum, linecol)
            End If

            Throw New SyntaxErrorException(Me.LineNumber, Me.LineColumn, "syntax error")
        End Function

#End Region

#Region "type check"

        Public Overridable Function IsNumber(c As Char) As Boolean

            Return (c >= "0"c AndAlso c <= "9"c)
        End Function

        Public Overridable Function IsNoneZeroNumber(c As Char) As Boolean

            Return (c >= "1"c AndAlso c <= "9"c)
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
                c = "~"c)
        End Function

#End Region

#Region "read token"

        Public Overridable Function ReadLineComment() As String

            Dim column = Me.PhysicalLineColumn
            Dim s = Me.ReadLine()
            Return s.Substring(column - 1)
        End Function

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
                If c = "\" Then

                    c = Me.ReadChar
                Else

                    If c = start_char Then Exit Do
                End If
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

            If buf.Length = 1 Then

                Select Case buf(0)
                    Case "<"c : Return New Token(SymbolTypes.LT, "<")
                    Case ">"c : Return New Token(SymbolTypes.GT, ">")
                    Case "|"c : Return New Token(SymbolTypes.OR, "|")
                End Select

            ElseIf buf.Length = 2 AndAlso buf(0) = buf(1) Then

                Select Case buf(0)
                    Case "&"c : Return New Token(SymbolTypes.AND2, "&&")
                    Case "|"c : Return New Token(SymbolTypes.OR2, "||")
                End Select
            End If
            Return New Token(SymbolTypes.OPE, buf.ToString)
        End Function

        Public Overridable Function ReadDecimal(org As System.Text.StringBuilder, linenum As Integer, linecol As Integer) As Token

            Return Me.ReadDecimal("", org, linenum, linecol)
        End Function

        Public Overridable Function ReadDecimal(readed As String, org As System.Text.StringBuilder, linenum As Integer, linecol As Integer) As Token

            Dim buf As New System.Text.StringBuilder(readed)
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    Me.ReadChar()

                ElseIf Me.IsNumber(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If

                org.Append(c)
            Loop

            Return Me.CreateNumericToken(org.ToString, Convert.ToUInt32(buf.ToString), linenum, linecol)
        End Function

        Public Overridable Function ReadBinary(org As System.Text.StringBuilder, linenum As Integer, linecol As Integer) As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    Me.ReadChar()

                ElseIf Me.IsBinary(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If

                org.Append(c)
            Loop

            Return Me.CreateNumericToken(org.ToString, Convert.ToUInt32(buf.ToString, 2), linenum, linecol)
        End Function

        Public Overridable Function ReadOctal(org As System.Text.StringBuilder, linenum As Integer, linecol As Integer) As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    Me.ReadChar()

                ElseIf Me.IsOctal(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If

                org.Append(c)
            Loop

            Return Me.CreateNumericToken(org.ToString, Convert.ToUInt32(buf.ToString, 8), linenum, linecol)
        End Function

        Public Overridable Function ReadHexadecimal(org As System.Text.StringBuilder, linenum As Integer, linecol As Integer) As Token

            Dim buf As New System.Text.StringBuilder
            Do While Not Me.EndOfStream

                Dim c = Me.NextChar
                If c = "_"c Then

                    Me.ReadChar()

                ElseIf Me.IsHexadecimal(c) Then

                    Me.ReadChar()
                    buf.Append(c)
                Else
                    Exit Do
                End If

                org.Append(c)
            Loop

            Return Me.CreateNumericToken(org.ToString, Convert.ToUInt32(buf.ToString, 16), linenum, linecol)
        End Function

        Public Overridable Function CreateNumericToken(format As String, n As UInt32, linenum As Integer, linecol As Integer) As Token

            Return New Token(SymbolTypes.NUM, n.ToString) With {.LineNumber = linenum, .LineColumn = linecol, .Value = New NumericNode(format, n) With {.LineNumber = linenum, .LineColumn = linecol}}
        End Function

#End Region

#Region "read line"

        Public Overridable Property CurrentWord As New System.Text.StringBuilder
        Public Overridable Property CurrentLine As New System.Text.StringBuilder
        Public Overridable Property TabSize As Integer = 4

        Public Overrides Function ReadChar() As Char

            If Me.EndOfStream() Then Throw New InvalidOperationException("ReadChar called end-of-stream")
            Dim n = Me.ReadStream
            Dim c = Char.ConvertFromUtf32(n)(0)
            If n = &HA OrElse (n = &HD AndAlso Me.PeekStream() <> &HA) Then

                Me.LineColumn = 1
                Me.PhysicalLineColumn = 1
                Me.LineNumber += 1
                Me.CurrentWord.Clear()
                Me.CurrentLine.Clear()

            ElseIf n = &H9 Then

                Me.LineColumn += Me.TabSize - (Me.LineColumn - 1) Mod Me.TabSize
                Me.PhysicalLineColumn += 1
                Me.CurrentWord.Clear()
                Me.CurrentLine.Append(c)
            Else

                Me.LineColumn += 1
                Me.PhysicalLineColumn += 1
                Me.CurrentWord.Append(c)
                Me.CurrentLine.Append(c)
            End If
            Return c
        End Function

        Public Overrides Function ReadLine() As String

            If Not Me.EndOfStream Then

                Me.LineColumn = 1
                Me.PhysicalLineColumn = 1
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

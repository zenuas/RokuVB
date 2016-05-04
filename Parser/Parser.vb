Imports System.Collections.Generic


Namespace Parser

    Public MustInherit Class Parser(Of T)

        Protected MustOverride Sub OnError(lex As Lexer(Of T))
        Protected MustOverride Function CreateTable() As Integer(,)
        Protected MustOverride Function RunAction(yy_no As Integer) As IToken(Of T)

        Private ReadOnly tables_(,) As Integer = Me.CreateTable()
        Private ReadOnly token_stack_ As New List(Of IToken(Of T))

        Public Overridable Function Parse(lex As Lexer(Of T)) As T

            Dim current = 0

            Do While Not lex.EndOfToken

                Dim token = lex.PeekToken()
                Dim x = Me.tables_(current, token.InputToken)

                If x < 0 Then

                    token = Me.RunAction(x)
                    If token.IsAccept Then Return token.Value
                    If Me.token_stack_.Count = 0 Then

                        current = 0
                    Else
                        current = Me.token_stack_(Me.token_stack_.Count - 1).TableIndex
                    End If
                    x = Me.tables_(current, token.InputToken)
                    GoTo SHIFT_TOKEN_

                ElseIf x = 0 Then

                    Me.OnError(lex)
                Else

                    lex.ReadToken()
SHIFT_TOKEN_:
                    token.TableIndex = x
                    Me.token_stack_.Add(token)
                    current = x
                End If
            Loop

            If Me.token_stack_(Me.token_stack_.Count - 1).IsAccept Then Return Me.token_stack_(0).Value
            Throw New SyntaxErrorException(-1, -1, "syntax error")
        End Function

        Protected Overridable Function GetToken(from_last_index As Integer) As IToken(Of T)

            Return Me.token_stack_(Me.token_stack_.Count + from_last_index)
        End Function

        Protected Overridable Function GetValue(from_last_index As Integer) As T

            Return Me.GetToken(from_last_index).Value
        End Function

        Protected Overridable Function DefaultAction(length As Integer) As T

            If length > 0 Then Return Me.GetValue(-length)
            Return Nothing
        End Function

        Public Overridable Function IsAccept(token As IToken(Of T)) As Boolean

            Dim current = 0
            If Me.token_stack_.Count = 0 Then

                current = 0
            Else
                current = Me.token_stack_(Me.token_stack_.Count - 1).TableIndex
            End If
            Return (Me.tables_(current, token.InputToken) <> 0)
        End Function

        Protected Overridable Function DoAction(
                token As IToken(Of T),
                length As Integer,
                value As T
            ) As IToken(Of T)

            token.Value = value
            Me.token_stack_.RemoveRange(Me.token_stack_.Count - length, length)
            Return token
        End Function

    End Class

End Namespace

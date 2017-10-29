Imports System.Collections.Generic


Namespace Parser

    Public MustInherit Class Parser(Of T)

        Public MustOverride Sub OnError(lex As Lexer(Of T))
        Public MustOverride Function CreateTable() As Integer(,)
        Public MustOverride Function RunAction(yy_no As Integer) As IToken(Of T)

        Public ReadOnly Tables(,) As Integer = Me.CreateTable()
        Public Overridable ReadOnly Property TokenStack As New List(Of IToken(Of T))

        Public Overridable Function Parse(lex As Lexer(Of T)) As T

            Dim current = 0

            Do While Not lex.EndOfToken

                Dim token = lex.PeekToken()
                Dim x = Me.Tables(current, token.InputToken)

                If x < 0 Then

                    token = Me.RunAction(x)
                    If token.IsAccept Then Return token.Value
                    If Me.TokenStack.Count = 0 Then

                        current = 0
                    Else
                        current = Me.TokenStack(Me.TokenStack.Count - 1).TableIndex
                    End If
                    x = Me.Tables(current, token.InputToken)
                    GoTo SHIFT_TOKEN_

                ElseIf x = 0 Then

                    Me.OnError(lex)
                Else

                    lex.ReadToken()
SHIFT_TOKEN_:
                    token.TableIndex = x
                    Me.TokenStack.Add(token)
                    current = x
                End If
            Loop

            If Me.TokenStack(Me.TokenStack.Count - 1).IsAccept Then Return Me.TokenStack(0).Value
            Throw New SyntaxErrorException(-1, -1, "syntax error")
        End Function

        Public Overridable Function GetToken(from_last_index As Integer) As IToken(Of T)

            Return Me.TokenStack(Me.TokenStack.Count + from_last_index)
        End Function

        Public Overridable Function GetValue(from_last_index As Integer) As T

            Return Me.GetToken(from_last_index).Value
        End Function

        Public Overridable Function DefaultAction(length As Integer) As T

            If length > 0 Then Return Me.GetValue(-length)
            Return Nothing
        End Function

        Public Overridable Function IsAccept(token As IToken(Of T)) As Boolean

            Dim current = 0
            If Me.TokenStack.Count = 0 Then

                current = 0
            Else
                current = Me.TokenStack(Me.TokenStack.Count - 1).TableIndex
            End If
            Return (Me.Tables(current, token.InputToken) <> 0)
        End Function

        Public Overridable Function DoAction(
                token As IToken(Of T),
                length As Integer,
                value As T
            ) As IToken(Of T)

            token.Value = value
            Me.TokenStack.RemoveRange(Me.TokenStack.Count - length, length)
            Return token
        End Function

    End Class

End Namespace

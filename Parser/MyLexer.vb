

Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports LetListNode = Roku.Node.ListNode(Of Roku.Node.LetNode)
Imports TypeListNode = Roku.Node.ListNode(Of Roku.Node.TypeBaseNode)
Imports VariableListNode = Roku.Node.ListNode(Of Roku.Node.VariableNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)
Imports FunctionListNode = Roku.Node.ListNode(Of Roku.Node.FunctionNode)


Namespace Parser

    Public Class MyLexer
        Inherits Lexer(Of INode)

        Public Sub New(reader As System.IO.TextReader)
            MyBase.New(reader)

            Me.ReservedChar("("c) = SymbolTypes.__x28
            Me.ReservedChar(")"c) = SymbolTypes.__x29
            Me.ReservedChar(","c) = SymbolTypes.__x2C
            Me.ReservedChar("."c) = SymbolTypes.__x2E
            Me.ReservedChar(":"c) = SymbolTypes.__x3A
            Me.ReservedChar("?"c) = SymbolTypes.__x3F
            Me.ReservedChar("["c) = SymbolTypes.__x5B
            Me.ReservedChar("]"c) = SymbolTypes.__x5D
            Me.ReservedChar("{"c) = SymbolTypes.__x7B
            Me.ReservedChar("}"c) = SymbolTypes.__x7D
            Me.ReservedWord("AND2") = SymbolTypes.AND2
            Me.ReservedWord("ARROW") = SymbolTypes.ARROW
            Me.ReservedWord("ATVAR") = SymbolTypes.ATVAR
            Me.ReservedWord("BEGIN") = SymbolTypes.BEGIN
            Me.ReservedWord("CLASS") = SymbolTypes.[CLASS]
            Me.ReservedWord("ELSE") = SymbolTypes.[ELSE]
            Me.ReservedWord("END") = SymbolTypes.[END]
            Me.ReservedWord("EOL") = SymbolTypes.EOL
            Me.ReservedWord("EQ") = SymbolTypes.EQ
            Me.ReservedWord("FALSE") = SymbolTypes.[FALSE]
            Me.ReservedWord("GT") = SymbolTypes.GT
            Me.ReservedWord("IF") = SymbolTypes.[IF]
            Me.ReservedWord("IGNORE") = SymbolTypes.IGNORE
            Me.ReservedWord("LET") = SymbolTypes.[LET]
            Me.ReservedWord("LT") = SymbolTypes.LT
            Me.ReservedWord("NULL") = SymbolTypes.NULL
            Me.ReservedWord("NUM") = SymbolTypes.NUM
            Me.ReservedWord("OPE") = SymbolTypes.OPE
            Me.ReservedWord("OR") = SymbolTypes.[OR]
            Me.ReservedWord("OR2") = SymbolTypes.OR2
            Me.ReservedWord("STR") = SymbolTypes.STR
            Me.ReservedWord("STRUCT") = SymbolTypes.STRUCT
            Me.ReservedWord("SUB") = SymbolTypes.[SUB]
            Me.ReservedWord("SWITCH") = SymbolTypes.SWITCH
            Me.ReservedWord("THEN") = SymbolTypes.[THEN]
            Me.ReservedWord("TRUE") = SymbolTypes.[TRUE]
            Me.ReservedWord("TYPE_PARAM") = SymbolTypes.TYPE_PARAM
            Me.ReservedWord("UNARY") = SymbolTypes.UNARY
            Me.ReservedWord("UNION") = SymbolTypes.UNION
            Me.ReservedWord("USE") = SymbolTypes.USE
            Me.ReservedWord("VAR") = SymbolTypes.VAR
        End Sub

        Public Overrides Function CreateEndOfToken() As IToken(Of INode)

            Return New Token(SymbolTypes._END)
        End Function

        Public Overrides Function CreateCharToken(x As SymbolTypes) As IToken(Of INode)

            Return New Token(x)
        End Function

        Public Overrides Function CreateWordToken(x As SymbolTypes) As IToken(Of INode)

            Return New Token(x)
        End Function
    End Class

End Namespace

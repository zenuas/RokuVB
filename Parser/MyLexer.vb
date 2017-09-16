

Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports TypeListNode = Roku.Node.ListNode(Of Roku.Node.TypeNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)


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
            Me.ReservedWord("ALLOW") = SymbolTypes.ALLOW
            Me.ReservedWord("ATVAR") = SymbolTypes.ATVAR
            Me.ReservedWord("BEGIN") = SymbolTypes.BEGIN
            Me.ReservedWord("ELSE") = SymbolTypes.[ELSE]
            Me.ReservedWord("END") = SymbolTypes.[END]
            Me.ReservedWord("EOL") = SymbolTypes.EOL
            Me.ReservedWord("EQ") = SymbolTypes.EQ
            Me.ReservedWord("IF") = SymbolTypes.[IF]
            Me.ReservedWord("LET") = SymbolTypes.[LET]
            Me.ReservedWord("NULL") = SymbolTypes.NULL
            Me.ReservedWord("NUM") = SymbolTypes.NUM
            Me.ReservedWord("OPE") = SymbolTypes.OPE
            Me.ReservedWord("STR") = SymbolTypes.STR
            Me.ReservedWord("STRUCT") = SymbolTypes.STRUCT
            Me.ReservedWord("SUB") = SymbolTypes.[SUB]
            Me.ReservedWord("SWITCH") = SymbolTypes.SWITCH
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

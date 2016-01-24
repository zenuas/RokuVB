
Imports Roku.Node
Imports VariableListNode = Roku.Node.ListNode(Of Roku.Node.VariableNode)
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)


Imports Roku.Parser

Namespace Parser

    Public Class MyLexer
        Inherits Lexer(Of INode)

        Public Sub New(ByVal reader As System.IO.TextReader)
            MyBase.New(reader)

            Me.ReservedChar("("c) = SymbolTypes.__x28
            Me.ReservedChar(")"c) = SymbolTypes.__x29
            Me.ReservedChar("."c) = SymbolTypes.__x2E
            Me.ReservedChar("["c) = SymbolTypes.__x5B
            Me.ReservedChar("]"c) = SymbolTypes.__x5D
            Me.ReservedChar("?"c) = SymbolTypes.__x3F
            Me.ReservedChar(":"c) = SymbolTypes.__x3A
            Me.ReservedChar(","c) = SymbolTypes.__x2C
            Me.ReservedWord("EOL") = SymbolTypes.EOL
            Me.ReservedWord("END") = SymbolTypes.[END]
            Me.ReservedWord("BEGIN") = SymbolTypes.BEGIN
            Me.ReservedWord("OPE") = SymbolTypes.OPE
            Me.ReservedWord("LET") = SymbolTypes.[LET]
            Me.ReservedWord("EQ") = SymbolTypes.EQ
            Me.ReservedWord("SUB") = SymbolTypes.[SUB]
            Me.ReservedWord("IF") = SymbolTypes.[IF]
            Me.ReservedWord("VAR") = SymbolTypes.VAR
            Me.ReservedWord("ELSE") = SymbolTypes.[ELSE]
            Me.ReservedWord("ATVAR") = SymbolTypes.ATVAR
            Me.ReservedWord("NUM") = SymbolTypes.NUM
            Me.ReservedWord("STR") = SymbolTypes.STR
            Me.ReservedWord("STRUCT") = SymbolTypes.STRUCT
        End Sub

        Public Overrides Function CreateEndOfToken() As IToken(Of INode)

            Return New Token(SymbolTypes._END)
        End Function

        Public Overrides Function CreateCharToken(x As Integer) As IToken(Of INode)

            Return New Token(CType(x, SymbolTypes))
        End Function

        Public Overrides Function CreateWordToken(x As Integer) As IToken(Of INode)

            Return New Token(CType(x, SymbolTypes))
        End Function
    End Class

End Namespace


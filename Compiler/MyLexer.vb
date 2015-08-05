
Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)


Imports Roku.Compiler

Namespace Compiler

    Public Class MyLexer
        Inherits Lexer(Of INode)

        Public Sub New(ByVal reader As System.IO.TextReader)
            MyBase.New(reader)

        End Sub

        Public Overrides Sub SetRegisterWord()

            Me.ReservedChar("("c) = SymbolTypes.__x28
            Me.ReservedChar(")"c) = SymbolTypes.__x29
            Me.ReservedChar("["c) = SymbolTypes.__x5B
            Me.ReservedChar("]"c) = SymbolTypes.__x5D
            Me.ReservedChar("?"c) = SymbolTypes.__x3F
            Me.ReservedChar(":"c) = SymbolTypes.__x3A
            Me.ReservedWord("EOL") = SymbolTypes.EOL
            Me.ReservedWord("BEGIN") = SymbolTypes.BEGIN
            Me.ReservedWord("END") = SymbolTypes.[END]
            Me.ReservedWord("OPE") = SymbolTypes.OPE
            Me.ReservedWord("LET") = SymbolTypes.[LET]
            Me.ReservedWord("EQ") = SymbolTypes.EQ
            Me.ReservedWord("SUB") = SymbolTypes.[SUB]
            Me.ReservedWord("ELSE") = SymbolTypes.[ELSE]
            Me.ReservedWord("IF") = SymbolTypes.[IF]
            Me.ReservedWord("VAR") = SymbolTypes.VAR
            Me.ReservedWord("NUM") = SymbolTypes.NUM
            Me.ReservedWord("STR") = SymbolTypes.STR

            MyBase.SetRegisterWord()
        End Sub

        Protected Overrides Function CreateEndOfToken() As IToken(Of INode)

            Return New Token(SymbolTypes._END)
        End Function

        Protected Overrides Function CreateCharToken(ByVal x As Integer) As IToken(Of INode)

            Return New Token(CType(x, SymbolTypes))
        End Function

        Protected Overrides Function CreateWordToken(ByVal x As Integer) As IToken(Of INode)

            Return New Token(CType(x, SymbolTypes))
        End Function
    End Class

End Namespace


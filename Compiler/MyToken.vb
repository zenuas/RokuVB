
Imports Roku.Node


Imports System
Imports Roku.Compiler

Namespace Compiler

    <Serializable()> _
    Public Class Token
        Implements IToken(Of INode)

        Public Sub New(ByVal type As SymbolTypes)

            Me.Type = type
        End Sub

        Public Sub New(ByVal type As SymbolTypes, ByVal name As String)
            Me.New(type)

            If Me.Type = SymbolTypes._END AndAlso Not name.Equals("") Then Throw New ArgumentException("eof with blank", "name")

            Me.Name = name
        End Sub

        Protected Overridable ReadOnly Property InputToken() As Integer Implements IToken(Of INode).InputToken
            Get
                Return Me.Type
            End Get
        End Property

        Public Overridable Property Name As String
        Public Overridable Property Type As SymbolTypes
        Protected Overridable Property TableIndex As Integer Implements IToken(Of INode).TableIndex
        Public Overridable Property Value As INode Implements IToken(Of INode).Value
        Public Overridable Property LineNumber As Integer?
        Public Overridable Property LineColumn As Integer?
        Public Overridable Property Indent As Integer Implements IToken(Of INode).Indent

        Public Overridable ReadOnly Property EndOfToken() As Boolean Implements IToken(Of INode).EndOfToken
            Get
                Return (Me.Type = SymbolTypes._END)
            End Get
        End Property

        Public Overridable ReadOnly Property IsAccept() As Boolean Implements IToken(Of INode).IsAccept
            Get
                Return Me.EndOfToken
            End Get
        End Property
    End Class

End Namespace

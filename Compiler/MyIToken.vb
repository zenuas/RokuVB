Namespace Compiler

    Public Interface IToken(Of T)

        ReadOnly Property EndOfToken() As Boolean
        ReadOnly Property IsAccept() As Boolean
        ReadOnly Property InputToken() As Integer
        Property TableIndex() As Integer
        Property Value() As T
        Property Indent As Integer

    End Interface

End Namespace

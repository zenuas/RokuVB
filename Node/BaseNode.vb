Imports Roku.Parser


Namespace Node

    Public MustInherit Class BaseNode
        Implements INode


        Public Overridable Property LineNumber As Integer? Implements INode.LineNumber
        Public Overridable Property LineColumn As Integer? Implements INode.LineColumn

        Public Overridable Sub AppendLineNumber(node As INode) Implements INode.AppendLineNumber

            Me.LineNumber = node.LineNumber
            Me.LineColumn = node.LineColumn
        End Sub

        Public Overridable Sub AppendLineNumber(token As Token) Implements INode.AppendLineNumber

            Me.LineNumber = token.LineNumber
            Me.LineColumn = token.LineColumn
        End Sub

        Public Overridable Function Clone() As INode Implements INode.Clone

            Return CType(Me.MemberwiseClone, INode)
        End Function
    End Class

End Namespace

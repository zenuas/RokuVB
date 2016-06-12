Imports System.Reflection
Imports Roku.Parser


Namespace Node

    Public MustInherit Class BaseNode
        Implements INode


        Public Overridable Property Parent As IScopeNode Implements INode.Parent
        Public Overridable Property LineNumber As Integer? Implements INode.LineNumber
        Public Overridable Property LineColumn As Integer? Implements INode.LineColumn

        Public Overridable Sub AppendLineNumber(node As INode)

            Me.LineNumber = node.LineNumber
            Me.LineColumn = node.LineColumn
        End Sub

        Public Overridable Sub AppendLineNumber(token As Token)

            Me.LineNumber = token.LineNumber
            Me.LineColumn = token.LineColumn
        End Sub

        Public Overridable Function Clone() As INode Implements INode.Clone

            Return CType(Me.MemberwiseClone, INode)
        End Function
    End Class

End Namespace

Namespace Node

    Public Class TypeNode
        Inherits BaseNode


        Public Sub New(name As VariableNode)

            Me.Name = name.Name
            Me.AppendLineNumber(name)
        End Sub

        Public Sub New(parent_ As TypeNode, name As VariableNode)
            Me.New(name)

            Me.Namespace = parent_
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property [Namespace] As TypeNode
        Public Overridable Property IsArray As Boolean = False

    End Class

End Namespace

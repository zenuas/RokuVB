Imports Roku.Node
Imports Roku.Manager


Namespace RkCode

    Public Class RkValue

        Public Sub New(scope As InType, name As String)

            Me.Scope = scope
            Me.Name = name
        End Sub

        Public Sub New(name As String)
            Me.New(Nothing, name)

        End Sub

        Public Sub New(name As String, type As InType)
            Me.New(Nothing, name, type)

        End Sub

        Public Sub New(scope As InType, name As String, type As InType)
            Me.New(scope, name)

            Me.Type = type
        End Sub

        Public Overridable ReadOnly Property Scope As InType
        Public Overridable ReadOnly Property Name As String
        Public Overridable Property Type As InType

        Public Shared Widening Operator CType(node As VariableNode) As RkValue

            Return New RkValue(node.Name, node.Type)
        End Operator
    End Class

End Namespace

Imports Roku.Manager


Namespace Node

    Public Class FunctionNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(name As String)

            Me.Name = name
        End Sub

        Public Overridable Property Name As String = ""
        Public Overridable Property Arguments As DeclareNode()
        Public Overridable Property [Return] As TypeNode
        Public Overridable Property Body As BlockNode
        Public Overridable Property Type As InType Implements IEvaluableNode.Type
    End Class

End Namespace

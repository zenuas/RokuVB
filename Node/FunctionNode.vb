Imports System.Collections.Generic
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
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable ReadOnly Property Bind As New Dictionary(Of IScopeNode, Boolean)

        Public Overridable ReadOnly Property [Function] As RkFunction
            Get
                Return CType(Me.Type, RkFunction)
            End Get
        End Property
    End Class

End Namespace

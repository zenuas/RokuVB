Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class FunctionNode
        Inherits BaseNode
        Implements IEvaluableNode, IBlock


        Public Sub New(name As String)

            Me.Name = name
        End Sub

        Public Overridable Property Name As String = "" Implements IBlock.Name
        Public Overridable Property Arguments As DeclareNode()
        Public Overridable Property [Return] As TypeNode
        Public Overridable Property Body As BlockNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable ReadOnly Property Bind As New Dictionary(Of IScopeNode, Boolean)

        Public Overridable Property [Function] As RkFunction Implements IBlock.Function
            Get
                Return CType(Me.Type, RkFunction)
            End Get
            Set(value As RkFunction)

                Me.Type = value
            End Set
        End Property
    End Class

End Namespace

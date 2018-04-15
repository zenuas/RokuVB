Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class FunctionNode
        Inherits BlockNode
        Implements IHaveScopeType


        Public Sub New(linenum As Integer)
            MyBase.New(linenum)

        End Sub

        Public Overridable Property Arguments As DeclareNode()
        Public Overridable Property [Return] As TypeNode
        Public Overridable Property Type As IType Implements IHaveScopeType.Type
        Public Overridable Property Bind As New Dictionary(Of IScopeNode, Boolean)

        Public Overridable Property [Function] As RkFunction
            Get
                Return CType(Me.Type, RkFunction)
            End Get
            Set(value As RkFunction)

                Me.Type = value
            End Set
        End Property

        Public Overrides Property Scope As RkScope
            Get
                Return Me.Function
            End Get
            Set(value As RkScope)

                Me.Function = CType(value, RkFunction)
            End Set
        End Property
    End Class

End Namespace

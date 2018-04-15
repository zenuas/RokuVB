Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class StructNode
        Inherits BaseNode
        Implements IScopeNode, IStatementNode, IHaveScopeType


        Public Sub New(linenum As Integer)

            Me.LineNumber = linenum
            Me.LineColumn = 0
        End Sub

        Public Overridable Sub AddLet(let_ As LetNode) Implements IScopeNode.AddLet

            Me.Lets.Add(let_.Var.Name, let_)
            Me.Statements.Add(let_)
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property Parent As IScopeNode Implements IScopeNode.Parent
        Public Overridable ReadOnly Property Statements As New List(Of IStatementNode)
        Public Overridable Property Owner As INamedFunction Implements IScopeNode.Owner
        Public Overridable Property InnerScope As Boolean = False Implements IScopeNode.InnerScope
        Public Overridable ReadOnly Property Lets As New Dictionary(Of String, INode) Implements IScopeNode.Lets
        Public Overridable Property Type As IType Implements IHaveScopeType.Type
        Public Overridable ReadOnly Property Generics As New List(Of TypeNode)

        Public Overridable ReadOnly Property Struct As RkStruct
            Get
                Return CType(Me.Type, RkStruct)
            End Get
        End Property
    End Class

End Namespace

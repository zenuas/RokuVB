Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class StructNode
        Inherits BaseNode
        Implements IScopeNode, IEvaluableNode


        Public Sub New(linenum As Integer)

            Me.LineNumber = linenum
            Me.LineColumn = 0
        End Sub

        Public Overridable Sub AddFunction(func As FunctionNode) Implements IScopeNode.AddFunction

            Me.Scope.Add(func.Name, func)
        End Sub

        Public Overridable Sub AddLet(let_ As LetNode) Implements IScopeNode.AddLet

            Me.Scope.Add(let_.Var.Name, let_)
            Me.Statements.Add(let_)
        End Sub

        Public Overridable Property Name As String
        Public Overridable ReadOnly Property Statements As New List(Of IEvaluableNode)
        Public Overridable Property Owner As IEvaluableNode Implements IScopeNode.Owner
        Public Overridable ReadOnly Property Scope As New Dictionary(Of String, INode) Implements IScopeNode.Scope
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable ReadOnly Property Generics As New List(Of VariableNode)

        Public Overridable ReadOnly Property Struct As RkStruct
            Get
                Return CType(Me.Type, RkStruct)
            End Get
        End Property
    End Class

End Namespace

﻿Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class BlockNode
        Inherits BaseNode
        Implements IScopeNode, IAddFunction


        Public Sub New(linenum As Integer)

            Me.LineNumber = linenum
            Me.LineColumn = 0
        End Sub

        Public Overridable Sub AddStatement(stmt As IStatementNode)

            If stmt IsNot Nothing Then Me.Statements.Add(stmt)
        End Sub

        Public Overridable Sub AddFunction(func As FunctionNode) Implements IAddFunction.AddFunction

            Me.Functions.Add(func)
        End Sub

        Public Overridable Sub AddLet(let_ As LetNode) Implements IScopeNode.AddLet

            Me.Lets.Add(let_.Var.Name, let_)
        End Sub

        Public Overridable Property Parent As IScopeNode Implements IScopeNode.Parent
        Public Overridable ReadOnly Property Statements As New List(Of IStatementNode)
        Public Overridable Property InnerScope As Boolean = True Implements IScopeNode.InnerScope
        Public Overridable ReadOnly Property Lets As New Dictionary(Of String, INode) Implements IScopeNode.Lets
        Public Overridable ReadOnly Property Functions As New List(Of FunctionNode) Implements IAddFunction.Functions
        Public Overridable Property Scope As RkScope

        Public Overridable ReadOnly Property Owner As INamedFunction Implements IScopeNode.Owner
            Get
                Dim p = CType(Me, IScopeNode)

                Do While TypeOf p IsNot INamedFunction

                    If p Is Nothing Then Return Nothing
                    p = p.Parent
                Loop

                Return CType(p, INamedFunction)
            End Get
        End Property

        Public Overrides Function ToString() As String

            Return $"line: {Me.LineNumber}"
        End Function

    End Class

End Namespace

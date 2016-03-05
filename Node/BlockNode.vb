Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class BlockNode
        Inherits BaseNode
        Implements IScopeNode, IEvaluableNode


        Public Sub New(linenum As Integer)

            Me.LineNumber = linenum
            Me.LineColumn = 0
        End Sub

        Public Overridable Sub AddStatement(stmt As IEvaluableNode)

            If stmt IsNot Nothing Then Me.Statements.Add(stmt)
        End Sub

        Public Overridable Sub AddFunction(func As FunctionNode) Implements IScopeNode.AddFunction

            Me.Scope.Add(func.Name, func)
        End Sub

        Public Overridable Sub AddLet(let_ As LetNode) Implements IScopeNode.AddLet

            Me.Scope.Add(let_.Var.Name, let_)
        End Sub

        Public Overridable ReadOnly Property Statements As New List(Of IEvaluableNode)
        Public Overridable Property Owner As IEvaluableNode Implements IScopeNode.Owner
        Public Overridable Property InnerScope As Boolean = True Implements IScopeNode.InnerScope
        Public Overridable ReadOnly Property Scope As New Dictionary(Of String, INode) Implements IScopeNode.Scope
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
    End Class

End Namespace

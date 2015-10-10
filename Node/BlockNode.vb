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

        Public Overridable Property Statements As New List(Of IEvaluableNode)

        Public Overridable Sub AddStatement(stmt As IEvaluableNode)

            If stmt IsNot Nothing Then Me.Statements.Add(stmt)
        End Sub

        Public Overridable Sub AddFunction(func As FunctionNode) Implements IScopeNode.AddFunction

            Me.Scope.Add(func.Name, func)
        End Sub

        Public Overridable Sub AddVar(var_ As VariableNode) Implements IScopeNode.AddVar

            Me.Scope.Add(var_.Name, var_)
        End Sub

        Public Overridable Property Owner As IEvaluableNode Implements IScopeNode.Owner

        Private scope_ As New Dictionary(Of String, INode)
        Public Overridable ReadOnly Property Scope As Dictionary(Of String, INode) Implements IScopeNode.Scope
            Get
                Return Me.scope_
            End Get
        End Property

        Public Overridable Property Type As InType Implements IEvaluableNode.Type
    End Class

End Namespace

Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class BlockNode
        Inherits BaseNode
        Implements IScopeNode


        Public Overridable Property Statement As IRunableNode

        Public Overridable Sub AddStatement(stmt As IRunableNode)

            If stmt Is Nothing Then Return

            stmt.Parent = Me
            If Me.Statement Is Nothing Then

                Me.Statement = stmt
            Else

                Dim current = Me.Statement
                Do While current.Next IsNot Nothing

                    current = current.Next
                Loop
                current.Next = stmt
            End If
        End Sub

        Public Overridable Sub AddFunction(func As FunctionNode) Implements IScopeNode.AddFunction

        End Sub

        Public Overridable Sub AddVar(var_ As VariableNode) Implements IScopeNode.AddVar

        End Sub

        Public Overridable Property Owner As IEvaluableNode Implements IScopeNode.Owner

        Private scope_ As New Dictionary(Of String, INode)
        Public Overridable ReadOnly Property Scope As Dictionary(Of String, INode) Implements IScopeNode.Scope
            Get
                Return Me.scope_
            End Get
        End Property
    End Class

End Namespace

Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class TypeNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New()

            Me.Name = ""
        End Sub

        Public Sub New(name As VariableNode)

            Me.Name = name.Name
            Me.AppendLineNumber(name)
        End Sub

        Public Sub New(parent_ As TypeNode, name As VariableNode)
            Me.New(name)

            Me.Namespace = parent_
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property [Namespace] As TypeNode
        Public Overridable Property IsArray As Boolean = False
        Public Overridable Property IsGeneric As Boolean = False
        Public Overridable Property Type As IType Implements IEvaluableNode.Type

    End Class

End Namespace

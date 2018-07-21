﻿Namespace Node

    Public Class DeclareNode
        Inherits BaseNode


        Public Sub New()

        End Sub

        Public Sub New(name As VariableNode, type As TypeBaseNode)

            Me.Name = name
            Me.Type = type
            Me.AppendLineNumber(name)
        End Sub

        Public Overridable Property Name As VariableNode
        Public Overridable Property Type As TypeBaseNode

    End Class

End Namespace

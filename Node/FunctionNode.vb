﻿Imports Roku.Manager


Namespace Node

    Public Class FunctionNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(name As String)

            Me.Name = name
        End Sub

        Public Overridable Property Name As String = ""
        Public Overridable Property Type As InType Implements IEvaluableNode.Type

        Public Overridable ReadOnly Property Receiver As InType Implements IEvaluableNode.Receiver
            Get
                Return Me.Type
            End Get
        End Property

    End Class

End Namespace

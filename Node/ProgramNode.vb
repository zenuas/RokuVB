Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class ProgramNode
        Inherits BlockNode
        Implements IBlock

        Public Sub New()
            MyBase.New(1)

            Me.InnerScope = False
        End Sub

        Public Overridable Property Name As String = ".ctor" Implements IBlock.Name
        Public Overridable ReadOnly Property Uses As New List(Of UseNode)
        Public Overridable Property [Function] As RkFunction Implements IBlock.Function
    End Class

End Namespace

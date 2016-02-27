Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class ProgramNode
        Inherits BlockNode

        Public Sub New()
            MyBase.New(1)

        End Sub

        Public Overridable ReadOnly Property Uses As New List(Of UseNode)
    End Class

End Namespace

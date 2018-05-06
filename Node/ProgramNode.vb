Imports System.Collections.Generic


Namespace Node

    Public Class ProgramNode
        Inherits FunctionNode

        Public Sub New()
            MyBase.New(1)

            Me.Name = ".ctor"
            Me.InnerScope = False
        End Sub

        Public Overridable ReadOnly Property Uses As New List(Of UseNode)
        Public Overridable ReadOnly Property FixedGenericFunction As New List(Of FunctionNode)

    End Class

End Namespace

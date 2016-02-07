Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class TypeFunctionNode
        Inherits TypeNode


        Public Sub New()

            Me.Name = ""
        End Sub

        Public Overridable Property Arguments As TypeNode()
        Public Overridable Property [Return] As TypeNode

    End Class

End Namespace

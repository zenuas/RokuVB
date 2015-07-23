Imports System
Imports Roku.Manager


Namespace Node

    Public Class NumericNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(ByVal n As UInt32)

            Me.Numeric = n
        End Sub

        Public Overridable Property Numeric As UInt32
        Public Overridable Property Type As InType Implements IEvaluableNode.Type
    End Class

End Namespace

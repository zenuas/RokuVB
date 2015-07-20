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

        Public Overridable ReadOnly Property Receiver() As InType Implements IEvaluableNode.Receiver
            Get
                Return Me.Type
            End Get
        End Property
    End Class

End Namespace

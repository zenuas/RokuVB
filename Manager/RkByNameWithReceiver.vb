Imports System.Collections.Generic
Imports Roku.Node


Namespace Manager

    Public Class RkByNameWithReceiver
        Inherits RkByName
        Implements IApply


        Public Overridable Property Receiver As IEvaluableNode

        Public Overridable ReadOnly Property Apply As List(Of IType) Implements IApply.Apply
            Get
                Return CType(Me.Receiver.Type, IApply).Apply
            End Get
        End Property

        Public Overrides Function HasIndefinite() As Boolean

            Return Me.Receiver.Type.HasIndefinite
        End Function

        Public Overrides Function ToString() As String

            Return If(Me.Receiver Is Nothing, $"{Me.Name}", $"{Me.Receiver}.{Me.Name}")
        End Function
    End Class

End Namespace

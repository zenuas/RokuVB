Imports System.Collections.Generic
Imports Roku.Manager


Namespace RkCode

    Public Class RkCall
        Inherits RkCode3

        Public Sub New(expr As RkValue, slot As RkValue)
            Me.New(expr, slot, Nothing)

        End Sub

        Public Sub New(expr As RkValue, slot As RkValue, return_ As RkValue)
            MyBase.New(RkOperand.Call, expr, slot, return_)

        End Sub

        Private args_ As New List(Of RkValue)
        Public Overridable ReadOnly Property Arguments As List(Of RkValue)
            Get
                Return Me.args_
            End Get
        End Property

    End Class

End Namespace

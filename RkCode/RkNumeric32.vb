Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace RkCode

    Public Class RkNumeric32
        Inherits RkValue

        Public Sub New(value As UInt32, type As InType)
            MyBase.New(value.ToString, type)

            Me.Numeric = value
        End Sub

        Public Overridable Property Numeric As UInt32

    End Class

End Namespace

Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace Manager

    Public Class RkNumeric32
        Inherits RkValue

        Public Overridable Property Numeric As UInt32

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} {Me.Numeric}"
        End Function

    End Class

End Namespace

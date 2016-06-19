Imports System


Namespace [Operator]

    Public Class OpNumeric32
        Inherits OpValue

        Public Overridable Property Numeric As UInt32

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} {Me.Numeric}"
        End Function

    End Class

End Namespace

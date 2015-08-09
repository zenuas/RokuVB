Namespace RkCode

    Public Class RkCode0

        Public Sub New()

            Me.Operand = RkOperand.Nop
        End Sub

        Public Sub New(ope As RkOperand)

            Me.Operand = ope
        End Sub

        Public Overridable Property Operand As RkOperand

    End Class

End Namespace

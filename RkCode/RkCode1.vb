Namespace RkCode

    Public Class RkCode1
        Inherits RkCode0

        Public Sub New(ope As RkOperand, left As RkValue)
            MyBase.New(ope)

            Me.Left = left
        End Sub

        Public Overridable Property Left As RkValue

    End Class

End Namespace

Namespace RkCode

    Public Class RkCode2
        Inherits RkCode1

        Public Sub New(ope As RkOperand, left As RkValue, return_ As RkValue)
            MyBase.New(ope, left)

            Me.Return = return_
        End Sub

        Public Overridable Property [Return] As RkValue

    End Class

End Namespace

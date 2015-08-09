Namespace RkCode

    Public Class RkCode3
        Inherits RkCode2

        Public Sub New(ope As RkOperand, left As RkValue, right As RkValue, ByVal return_ As RkValue)
            MyBase.New(ope, left, return_)

            Me.Right = right
        End Sub

        Public Overridable Property Right As RkValue

    End Class

End Namespace

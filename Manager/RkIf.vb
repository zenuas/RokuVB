Namespace Manager

    Public Class RkIf
        Inherits RkCode0

        Public Sub New()

            Me.Operator = RkOperator.If
        End Sub

        Public Overridable Property Condition As RkValue
        Public Overridable Property [Then] As RkLabel
        Public Overridable Property [Else] As RkLabel

    End Class

End Namespace

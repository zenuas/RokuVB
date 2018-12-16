Imports Roku.Operator


Namespace IntermediateCode

    Public Class InCode
        Inherits InCode0
        Implements IReturnBind

        Public Overridable Property [Return] As OpValue Implements IReturnBind.Return
        Public Overridable Property Left As OpValue
        Public Overridable Property Right As OpValue

        Public Overrides Function ToString() As String

            If Me.Return Is Nothing Then

                If Me.Right Is Nothing Then

                    Return $"{Me.Operator} {Me.Left}"
                Else
                    Return $"{Me.Left} {Me.Operator} {Me.Right}"
                End If
            Else

                If Me.Right Is Nothing Then

                    Return $"{Me.Return} = {Me.Operator} {Me.Left}"
                Else
                    Return $"{Me.Return} = {Me.Left} {Me.Operator} {Me.Right}"
                End If
            End If
        End Function
    End Class

End Namespace

Namespace Manager

    Public Class RkCILByName
        Inherits RkByName

        Public Overrides Function [Is](t As IType) As Boolean

            If t.Name.Equals(Me.Name) Then Return True
            Return False
        End Function

    End Class

End Namespace


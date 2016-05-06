Imports System.Reflection


Namespace Manager

    Public Class RkCILStruct
        Inherits RkStruct

        Public Overridable Property TypeInfo As TypeInfo

        Public Overrides Function [Is](t As IType) As Boolean

            If t.Name.Equals(Me.Name) Then Return True
            Return False
        End Function

    End Class

End Namespace


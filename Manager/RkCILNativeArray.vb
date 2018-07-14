Namespace Manager

    Public Class RkCILNativeArray
        Inherits RkStruct

        Public Overrides Function [Is](t As IType) As Boolean

            Return (TypeOf t Is RkCILStruct AndAlso CType(t, RkCILStruct).TypeInfo.IsArray)
        End Function

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkCILNativeArray With {.Name = Me.Name, .Scope = Me.Scope, .GenericBase = Me}
            x.Scope.AddStruct(x)
            Return x
        End Function

    End Class

End Namespace

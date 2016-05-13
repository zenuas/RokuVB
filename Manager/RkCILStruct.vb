Imports System.Reflection


Namespace Manager

    Public Class RkCILStruct
        Inherits RkStruct

        Public Overridable Property TypeInfo As TypeInfo

        Public Overrides Function [Is](t As IType) As Boolean

            Return t Is Me
        End Function

        Public Overridable Function LoadConstructor(ParamArray args() As IType) As ConstructorInfo

            Return Me.TypeInfo.GetConstructor({GetType(Integer), GetType(Integer), GetType(Integer)})
        End Function

    End Class

End Namespace


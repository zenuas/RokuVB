Imports System.Reflection
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkCILStruct
        Inherits RkStruct

        Public Overridable Property TypeInfo As TypeInfo

        Public Overrides Function [Is](t As IType) As Boolean

            Return t Is Me
        End Function

        Public Overridable Function LoadConstructor(root As SystemLirary, ParamArray args() As IType) As ConstructorInfo

            Return Me.TypeInfo.GetConstructors.FindFirst(Function(ctor) ctor.GetParameters.And(Function(arg, i) root.LoadType(arg.ParameterType.GetTypeInfo).Is(args(i))))
        End Function

    End Class

End Namespace


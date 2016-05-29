Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkCILStruct
        Inherits RkStruct

        Public Overridable Property TypeInfo As TypeInfo
        Public Overridable Property FunctionNamespace As RkCILNamespace
        Public Overridable ReadOnly Property ConstructorCache As New Dictionary(Of ConstructorInfo, RkCILConstructor)

        Public Overridable Function LoadConstructor(root As SystemLirary, ParamArray args() As IType) As RkCILConstructor

            Dim ci = Me.TypeInfo.GetConstructors.FindFirst(Function(ctor) ctor.GetParameters.And(Function(arg, i) root.LoadType(arg.ParameterType.GetTypeInfo).Is(args(i))))
            If Me.ConstructorCache.ContainsKey(ci) Then Return Me.ConstructorCache(ci)

            Dim rk_ctor As New RkCILConstructor With {.Name = ci.Name, .TypeInfo = Me.TypeInfo, .ConstructorInfo = ci, .Return = Me}
            Me.ConstructorCache(ci) = rk_ctor
            rk_ctor.Arguments.AddRange(ci.GetParameters.Map(Function(arg) New NamedValue With {.Name = arg.Name, .Value = root.LoadType(arg.ParameterType.GetTypeInfo)}))
            Return rk_ctor
        End Function

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkCILStruct With {.Name = Me.Name, .Namespace = Me.Namespace, .TypeInfo = Me.TypeInfo}
            x.Namespace.AddStruct(x)
            Return x
        End Function

    End Class

End Namespace


Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkCILStruct
        Inherits RkStruct

        Public Overridable Property TypeInfo As TypeInfo
        Public Overridable Property FunctionNamespace As RkCILNamespace
        Public Overridable ReadOnly Property ConstructorCache As New Dictionary(Of ConstructorInfo, RkCILConstructor)

        Public Overridable Function LoadConstructor(root As SystemLibrary, ParamArray args() As IType) As RkCILConstructor

            Dim ci = Me.TypeInfo.GetConstructors.FindFirst(Function(x) x.GetParameters.Length = args.Length AndAlso x.GetParameters.And(Function(arg, i) root.LoadType(arg.ParameterType.GetTypeInfo).Is(args(i))))
            If Me.ConstructorCache.ContainsKey(ci) Then Return Me.ConstructorCache(ci)

            Dim ctor As New RkCILConstructor With {.Name = ci.Name, .TypeInfo = Me.TypeInfo, .ConstructorInfo = ci, .Return = Me}
            Me.ConstructorCache(ci) = ctor
            ctor.Arguments.AddRange(ci.GetParameters.Map(Function(arg) New NamedValue With {.Name = arg.Name, .Value = root.LoadType(arg.ParameterType.GetTypeInfo)}))
            Return ctor
        End Function

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkCILStruct With {.Name = Me.Name, .Scope = Me.Scope, .TypeInfo = Me.TypeInfo, .GenericBase = Me, .FunctionNamespace = Me.FunctionNamespace}
            x.Scope.AddStruct(x)
            Return x
        End Function

        Public Overrides Function [Is](t As IType) As Boolean

            If MyBase.Is(t) Then Return True
            If Me.TypeInfo = GetType(System.Void) AndAlso t Is Nothing Then Return True

            If TypeOf t Is RkCILStruct Then

                Dim cil_t = CType(t, RkCILStruct)
                If Me.TypeInfo.IsInterface AndAlso Util.TypeHelper.IsInterface(cil_t.TypeInfo, Me.TypeInfo) Then Return True
                If cil_t.TypeInfo.IsInterface AndAlso Util.TypeHelper.IsInterface(Me.TypeInfo, cil_t.TypeInfo) Then Return True
            End If

            Return False
        End Function

    End Class

End Namespace


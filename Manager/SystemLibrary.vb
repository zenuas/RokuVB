Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.IntermediateCode
Imports Roku.Util.Extensions


Namespace Manager

    Public Class SystemLibrary
        Inherits RkNamespace


        Public Overridable ReadOnly Property TypeCache As New Dictionary(Of TypeInfo, RkCILStruct)

        Public Sub New()

            Me.Initialize()
        End Sub

        Public Overridable Sub Initialize()

            ' struct Bool : Boolean
            Dim bool = Me.LoadType(GetType(Boolean).GetTypeInfo)
            Me.AddStruct(bool, "Bool")

            Dim add_native_operator_function =
                Function(t As IType, name As String, op As InOperator) As RkNativeFunction

                    Dim f As New RkNativeFunction With {.Name = name, .Operator = op, .Scope = Me, .Parent = Me}
                    f.Return = t
                    f.Arguments.Add(New NamedValue With {.Name = "left", .Value = t})
                    f.Arguments.Add(New NamedValue With {.Name = "right", .Value = t})
                    Me.AddFunction(f)
                    Return f
                End Function
            Dim add_native_comparison_function =
                Function(t As IType, name As String, op As InOperator) As RkNativeFunction

                    Dim f As New RkNativeFunction With {.Name = name, .Operator = op, .Scope = Me, .Parent = Me}
                    f.Return = bool
                    f.Arguments.Add(New NamedValue With {.Name = "left", .Value = t})
                    f.Arguments.Add(New NamedValue With {.Name = "right", .Value = t})
                    Me.AddFunction(f)
                    Return f
                End Function

            ' struct Int32 : Numeric
            Dim define_num =
                Function(t As Type)

                    Dim x = Me.LoadType(t.GetTypeInfo)
                    Me.AddStruct(x)

                    Dim num_plus = add_native_operator_function(x, "+", InOperator.Plus)
                    Dim num_minus = add_native_operator_function(x, "-", InOperator.Minus)
                    Dim num_mul = add_native_operator_function(x, "*", InOperator.Mul)
                    Dim num_div = add_native_operator_function(x, "/", InOperator.Div)
                    Dim num_eq = add_native_comparison_function(x, "==", InOperator.Equal)
                    Dim num_gt = add_native_comparison_function(x, ">", InOperator.Gt)
                    Dim num_gte = add_native_comparison_function(x, ">=", InOperator.Gte)
                    Dim num_lt = add_native_comparison_function(x, "<", InOperator.Lt)
                    Dim num_lte = add_native_comparison_function(x, "<=", InOperator.Lte)
                    Return x
                End Function
            Dim int64 = define_num(GetType(Int64))
            Dim int32 = define_num(GetType(Int32))
            Dim int16 = define_num(GetType(Int16))
            Dim int8 = define_num(GetType(Byte))
            Me.AddStruct(int32, "Int")

            ' struct Array(@T) : List(@T)
            Dim arr = Me.LoadType(GetType(List(Of )).GetTypeInfo)
            Me.AddStruct(arr, "Array")

            ' struct Char : Char
            Dim chr = Me.LoadType(GetType(Char).GetTypeInfo)
            Me.AddStruct(chr)

            ' struct String : String
            Dim str = Me.LoadType(GetType(String).GetTypeInfo)
            Me.AddStruct(str)
            Dim str_plus = add_native_operator_function(str, "+", InOperator.Plus)

            ' sub [](self: Array(@T), index: Int32) @T
            Dim array_index = LoadFunction(arr.FunctionNamespace, "Item", arr, int32)
            Me.AddFunction(array_index, "[]")

            ' sub print(s: @T)
            Me.CreateFunctionAlias(Me.LoadType(GetType(System.Console).GetTypeInfo).FunctionNamespace, "WriteLine", "print")

            ' sub cast(x: @T, t: @R) @R
            Dim cast As New RkNativeFunction With {.Name = "cast", .Operator = InOperator.Cast, .Scope = Me, .Parent = Me}
            Dim cast_t = cast.DefineGeneric("@T")
            Dim cast_r = cast.DefineGeneric("@R")
            cast.Return = cast_r
            cast.Arguments.Add(New NamedValue With {.Name = "x", .Value = cast_t})
            cast.Arguments.Add(New NamedValue With {.Name = "t", .Value = cast_r})
            Me.AddFunction(cast)

            ' sub #Alloc(x: @T) @T
            Dim alloc As New RkNativeFunction With {.Name = "#Alloc", .Operator = InOperator.Alloc, .Scope = Me, .Parent = Me}
            Dim alloc_t = alloc.DefineGeneric("@T")
            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = alloc_t})
            alloc.Return = alloc_t
            Me.AddFunction(alloc)

            ' sub #Type() @T
            Dim type As New RkNativeFunction With {.Name = "#Type", .Operator = InOperator.Nop, .Scope = Me, .Parent = Me}
            Dim type_t = type.DefineGeneric("@T")
            type.Return = type_t
            Me.AddFunction(type)

            ' struct Null
            Dim null As New RkStruct With {.Name = "Null", .Scope = Me, .Parent = Me}
            Me.AddStruct(null, "Null")

        End Sub

        Public Overridable Sub CreateFunctionAlias(scope As IScope, name As String, [alias] As String)

            For Each f In FindLoadFunction(scope, name, Function(x) True)

                Me.AddFunction(f, [alias])
            Next
        End Sub

        Public Overridable Function CreateNamespace(name As String) As RkNamespace

            Return Me.CreateNamespace(name, Me)
        End Function

        Public Overridable Function CreateNamespace(name As String, current As RkNamespace) As RkNamespace

            If String.IsNullOrEmpty(name) Then Return current

            Dim make_ns =
                Function(s As String, parent As RkNamespace)

                    Dim ns = parent.TryGetNamespace(s)
                    If ns IsNot Nothing Then Return ns
                    ns = New RkNamespace With {.Name = s, .Parent = parent}
                    parent.AddNamespace(ns)
                    Return ns
                End Function

            Dim index = name.IndexOf("."c)
            If index < 0 Then

                Return make_ns(name, current)
            Else

                Return Me.CreateNamespace(name.Substring(index + 1), make_ns(name.Substring(0, index), current))
            End If
        End Function

        Public Overridable Function GetNamespace(name As String) As RkNamespace

            If String.IsNullOrEmpty(name) Then Return Me
            Return Me.Namespaces(name)
        End Function

        Public Overridable Iterator Function AllNamespace() As IEnumerable(Of RkNamespace)

            Dim nswalk As Func(Of RkNamespace, IEnumerable(Of RkNamespace)) =
                Iterator Function(ns As RkNamespace) As IEnumerable(Of RkNamespace)

                    Yield ns
                    For Each child In ns.Namespaces.Values

                        For Each x In nswalk(child)

                            Yield x
                        Next
                    Next
                End Function

            For Each ns In nswalk(Me)

                Yield ns
            Next
        End Function

        Public Overridable Sub LoadAssembly(asm As Assembly)

            For Each t In asm.ExportedTypes

                Me.LoadType(CType(t, TypeInfo))
            Next
        End Sub

        Public Overridable Function LoadType(ti As TypeInfo) As RkCILStruct

            If Me.TypeCache.ContainsKey(ti) Then Return Me.TypeCache(ti)

            Dim ns = Me.CreateNamespace(ti.Namespace)
            Dim s = New RkCILStruct With {.Scope = ns, .Name = ti.Name, .TypeInfo = ti}
            Me.TypeCache(ti) = s

            If ti.IsGenericType Then

                Dim type_name = ti.Name
                Dim gens = ti.GenericTypeParameters
                Dim suffix = $"`{gens.Length}"
                If type_name.EndsWith(suffix) Then type_name = type_name.Substring(0, type_name.Length - suffix.Length)
                gens.Do(Sub(x) s.DefineGeneric(x.Name))

                ns.AddStruct(s, type_name)
            Else

                ns.AddStruct(s)
            End If

            If ns.Namespaces.ContainsKey(ti.Name) Then

                'ToDo: generics suport
            Else
                s.FunctionNamespace = New RkCILNamespace With {.Name = ti.Name, .Parent = ns, .Root = Me, .BaseType = s}
                ns.AddNamespace(s.FunctionNamespace)
            End If

            Return s
        End Function

        Public Shared Function CurrentNamespace(scope As IScope) As RkNamespace

            If TypeOf scope Is RkNamespace Then Return CType(scope, RkNamespace)
            Return CurrentNamespace(scope.Parent)
        End Function

        Public Shared Function LoadNamespace(scope As IScope, name As String) As RkNamespace

            Dim x = TryLoadNamespace(scope, name)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Shared Function TryLoadNamespace(scope As IScope, name As String) As RkNamespace

            If scope Is Nothing Then Return Nothing

            If TypeOf scope Is RkNamespace Then Return TryLoadNamespace(CType(scope, RkNamespace), name)
            Return TryLoadNamespace(scope.Parent, name)
        End Function

        Public Shared Function TryLoadNamespace(ns As RkNamespace, name As String) As RkNamespace

            If ns.Namespaces.ContainsKey(name) Then

                Dim x = ns.TryGetNamespace(name)
                If x IsNot Nothing Then Return x
            End If

            For Each path In ns.LoadPaths

                If TypeOf path Is RkNamespace Then

                    Dim ns2 = CType(path, RkNamespace)
                    If ns2.Name.Equals(name) Then Return ns2
                    Dim ns3 = TryLoadNamespace(ns2, name)
                    If ns3 IsNot Nothing Then Return ns3
                End If
            Next

            Return Nothing
        End Function

        Public Shared Function LoadStruct(scope As IScope, name As String, ParamArray args() As IType) As IType

            Dim x = TryLoadStruct(scope, name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Shared Function TryCurrentLoadStruct(scope As IScope, name As String, ParamArray args() As IType) As IType

            For Each f In scope.FindCurrentStruct(name).By(Of RkStruct).Where(Function(x) x.Apply.Count = args.Length AndAlso Not x.HasGeneric)

                If f.Apply.And(Function(x, i) x Is args(i)) Then Return f
            Next

            For Each f In scope.FindCurrentStruct(name).By(Of RkStruct).Where(Function(x) x.Generics.Count = args.Length AndAlso x.HasGeneric)

                Return CType(f.FixedGeneric(args), RkStruct)
            Next

            If args.Length = 0 Then

                For Each f In scope.FindCurrentStruct(name).By(Of RkUnionType)

                    Return f
                Next
            End If

            Return Nothing
        End Function

        Public Shared Function TryLoadStruct(scope As IScope, name As String, ParamArray args() As IType) As IType

            If scope Is Nothing Then Return Nothing

            Dim x = TryCurrentLoadStruct(scope, name, args)
            If x IsNot Nothing Then Return x

            If TypeOf scope Is RkNamespace Then

                Dim ns = CType(scope, RkNamespace)
                For Each path In ns.LoadPaths

                    If TypeOf path Is RkStruct Then

                        Dim struct = CType(path, RkStruct)
                        If struct.Name.Equals(name) Then Return struct
                    End If

                    If TypeOf path Is IScope Then

                        x = TryLoadStruct(CType(path, IScope), name, args)
                        If x IsNot Nothing Then Return x
                    End If
                Next

                Return Nothing
            Else

                Return TryLoadStruct(scope.Parent, name, args)
            End If

        End Function

        Public Shared Function LoadFunction(scope As IScope, name As String, ParamArray args() As IType) As IFunction

            Dim x = TryLoadFunction(scope, name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Shared Function TryLoadFunction(scope As IScope, name As String, ParamArray args() As IType) As IFunction

            Dim fs = FindLoadFunction(scope, name, args).ToList

            If fs.Count = 0 Then

                If args.Length > 0 AndAlso TypeOf args(0) Is RkCILStruct Then

                    Dim ci = CType(args(0), RkCILStruct)
                    If ci.TypeInfo.IsArray Then

                        Select Case name
                            Case "[]" : Return TryLoadFunction(ci.FunctionNamespace, "GetValue", args)
                        End Select
                    End If
                End If
                Return Nothing

            ElseIf fs.Count = 1 Then

                Return fs(0).ApplyFunction(args)
            Else

                fs.Done(Function(x) x.ApplyFunction(args))
                fs = fs.ToHash_ValueDerivation(Function(x) True).Keys.ToList
                If fs.Count <= 1 Then Return fs(0)

                Return New RkUnionType(fs)
            End If
        End Function

        Public Shared Iterator Function FindLoadFunction(scope As IScope, name As String, ParamArray args() As IType) As IEnumerable(Of IFunction)

            For Each f In FindLoadFunction(scope, name, Function(x) Not x.HasIndefinite AndAlso x.Arguments.Count = args.Length AndAlso x.Arguments.And(Function(arg, i) args(i) Is Nothing OrElse TypeOf args(i) Is RkGenericEntry OrElse arg.Value.Is(args(i))))

                Yield f
            Next

        End Function

        Public Shared Iterator Function FindLoadFunction(scope As IScope, name As String, match As Func(Of IFunction, Boolean)) As IEnumerable(Of IFunction)

            If scope Is Nothing Then Return

            For Each f In scope.FindCurrentFunction(name)

                If match(f) Then Yield f
            Next

            If TypeOf scope Is RkNamespace Then

                Dim ns = CType(scope, RkNamespace)
                For Each path In ns.LoadPaths

                    If TypeOf path Is RkFunction Then

                        Dim func = CType(path, RkFunction)
                        If func.Name.Equals(name) AndAlso match(func) Then Yield func
                    End If

                    If TypeOf path Is IScope Then

                        For Each f In FindLoadFunction(CType(path, IScope), name, match)

                            Yield f
                        Next
                    End If
                Next
            Else

                For Each f In FindLoadFunction(scope.Parent, name, match)

                    Yield f
                Next
            End If

        End Function

        Public Overridable Function IsArray(t As IType) As Boolean

            If TypeOf t Is RkByName Then Return Me.IsArray(CType(t, RkByName).Type)
            If TypeOf t IsNot RkCILStruct Then Return False
            Return Me.Structs("Array")(0) Is CType(t, RkCILStruct).GenericBase
        End Function

        Public Overridable Function GetArrayType(t As IType) As IType

            If TypeOf t Is RkByName Then Return Me.GetArrayType(CType(t, RkByName).Type)
            If Not Me.IsArray(t) Then Throw New Exception("not array")
            Return CType(t, RkCILStruct).Apply(0)
        End Function

    End Class

End Namespace

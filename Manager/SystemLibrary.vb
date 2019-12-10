Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.IntermediateCode
Imports Roku.Util.Extensions


Namespace Manager

    Public Class SystemLibrary
        Inherits RkNamespace


        Public Overridable ReadOnly Property TypeCache As New Dictionary(Of TypeInfo, RkCILStruct)
        Public Overridable ReadOnly Property TupleCache As New Dictionary(Of IStruct, RkStruct)

        Public Sub New()

            Me.Initialize()
        End Sub

        Public Overridable Sub Initialize()

            ' struct Void : Void
            Me.VoidType = Me.LoadType(GetType(Void).GetTypeInfo)
            Me.AddStruct(Me.VoidType, "Void")

            ' struct Object : Object
            Me.ObjectType = Me.LoadType(GetType(Object).GetTypeInfo)
            Me.AddStruct(Me.ObjectType, "Object")

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
                    f.Return = Me.BoolType
                    f.Arguments.Add(New NamedValue With {.Name = "left", .Value = t})
                    f.Arguments.Add(New NamedValue With {.Name = "right", .Value = t})
                    Me.AddFunction(f)
                    Return f
                End Function
            Dim add_native_unary_function =
                Function(t As IType, name As String, op As InOperator) As RkNativeFunction

                    Dim f As New RkNativeFunction With {.Name = name, .Operator = op, .Scope = Me, .Parent = Me}
                    f.Return = t
                    f.Arguments.Add(New NamedValue With {.Name = "left", .Value = t})
                    Me.AddFunction(f)
                    Return f
                End Function

            ' struct Bool : Boolean
            Me.BoolType = Me.LoadType(GetType(Boolean).GetTypeInfo)
            Me.AddStruct(Me.BoolType, "Bool")
            add_native_unary_function(Me.BoolType, "!", InOperator.Not)

            ' struct Int32 : Numeric
            Dim define_num =
                Function(t As Type)

                    Dim x = Me.LoadType(t.GetTypeInfo)
                    Me.AddStruct(x)

                    Dim num_plus = add_native_operator_function(x, "+", InOperator.Plus)
                    Dim num_minus = add_native_operator_function(x, "-", InOperator.Minus)
                    Dim num_mul = add_native_operator_function(x, "*", InOperator.Mul)
                    Dim num_div = add_native_operator_function(x, "/", InOperator.Div)
                    Dim num_mod = add_native_operator_function(x, "%", InOperator.Mod)
                    Dim num_eq = add_native_comparison_function(x, "==", InOperator.Equal)
                    Dim num_gt = add_native_comparison_function(x, ">", InOperator.Gt)
                    Dim num_gte = add_native_comparison_function(x, ">=", InOperator.Gte)
                    Dim num_lt = add_native_comparison_function(x, "<", InOperator.Lt)
                    Dim num_lte = add_native_comparison_function(x, "<=", InOperator.Lte)
                    Dim num_uminus = add_native_unary_function(x, "-", InOperator.UMinus)
                    Return x
                End Function
            Dim int64 = define_num(GetType(Int64))
            Dim int32 = define_num(GetType(Int32))
            Dim int16 = define_num(GetType(Int16))
            Dim int8 = define_num(GetType(Byte))
            Me.AddStruct(int32, "Int")

            ' struct NativeArray(@T)
            Dim native_array As New RkCILNativeArray With {.Name = "NativeArray", .Scope = Me, .Parent = Me}
            native_array.DefineGeneric("@T")
            Me.AddStruct(native_array)

            ' sub [](self: NativeArray(@T), index: Int32) @T
            Dim native_array_index As New RkCILReplacedFunction With {.Name = "[]", .Scope = Me, .Parent = Me}
            Dim native_array_index_t = native_array_index.DefineGeneric("@T")
            native_array_index.Return = native_array_index_t
            native_array_index.Arguments.Add(New NamedValue With {.Name = "xs", .Value = native_array.FixedGeneric(native_array_index_t)})
            native_array_index.Arguments.Add(New NamedValue With {.Name = "index", .Value = int32})
            native_array_index.ReplacedFunction = Function(xs) TryLoadFunction(CType(FixedByName(xs(0)), RkCILStruct).FunctionNamespace, "Get", xs)
            Me.AddFunction(native_array_index)

            ' struct Array(@T) : List(@T)
            Dim arr = Me.LoadArrayType(GetType(List(Of )).GetTypeInfo)
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

            ' sub typeof(x: @T) System.Type
            Dim typeof_ As New RkNativeFunction With {.Name = "typeof", .Operator = InOperator.Typeof, .Scope = Me, .Parent = Me}
            Dim typeof_t = typeof_.DefineGeneric("@T")
            typeof_.Arguments.Add(New NamedValue With {.Name = "x", .Value = typeof_t})
            typeof_.Return = Me.LoadType(GetType(System.Type).GetTypeInfo)
            Me.AddFunction(typeof_)

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

            ' sub #Bind(x: @T) @T
            Dim bind As New RkNativeFunction With {.Name = "#Bind", .Operator = InOperator.Bind, .Scope = Me, .Parent = Me}
            Dim bind_t = bind.DefineGeneric("@T")
            bind.Arguments.Add(New NamedValue With {.Name = "x", .Value = bind_t})
            bind.Return = bind_t
            Me.AddFunction(bind)

            ' sub #Type() @T
            Dim type As New RkNativeFunction With {.Name = "#Type", .Operator = InOperator.Nop, .Scope = Me, .Parent = Me}
            Dim type_t = type.DefineGeneric("@T")
            type.Return = type_t
            Me.AddFunction(type)

            ' struct Null
            Me.NullType = New RkStruct With {.Name = "Null", .Scope = Me, .Parent = Me}
            Me.AddStruct(Me.NullType, "Null")
        End Sub

        Public Overridable Property VoidType() As RkStruct
        Public Overridable Property ObjectType() As RkStruct
        Public Overridable Property NullType() As RkStruct
        Public Overridable Property BoolType() As RkStruct

        Private Property NumericTypes_ As IType() = Nothing
        Public Overridable Function NumericTypes() As IType()

            ' [Int32 | Int64 | Int16 | Byte]
            If NumericTypes_ Is Nothing Then

                NumericTypes_ = {
                    LoadStruct(Me, "Int32"),
                    LoadStruct(Me, "Int64"),
                    LoadStruct(Me, "Int16"),
                    LoadStruct(Me, "Byte")}
            End If
            Return NumericTypes_
        End Function

        Public Overridable Function ChoosePriorityType(types As List(Of IType)) As IType

            If types Is Nothing Then Return Me.VoidType
            Dim not_void = types.Where(Function(x) x IsNot Me.VoidType).ToList
            If not_void.Count = 0 Then Return Me.VoidType

            Dim not_num = not_void.FindFirstOrNull(Function(x) Me.NumericTypes.FindFirstOrNull(Function(a) a.Is(x)) Is Nothing)
            If not_num IsNot Nothing Then Return not_num

            Dim t As IType
            t = not_void.FindFirstOrNull(Function(x) LoadStruct(Me, "Int32").Is(x))
            If t IsNot Nothing Then Return t

            t = not_void.FindFirstOrNull(Function(x) LoadStruct(Me, "Int64").Is(x))
            If t IsNot Nothing Then Return t

            t = not_void.FindFirstOrNull(Function(x) LoadStruct(Me, "Int16").Is(x))
            If t IsNot Nothing Then Return t

            t = not_void.FindFirstOrNull(Function(x) LoadStruct(Me, "Byte").Is(x))
            If t IsNot Nothing Then Return t

            Return not_void(0)
        End Function

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

        Public Overridable Function LoadArrayType(ti As TypeInfo) As RkCILStruct

            If Me.TypeCache.ContainsKey(ti) Then Return Me.TypeCache(ti)

            Dim ns = Me.CreateNamespace(ti.Namespace)
            Dim s = New RkCILStruct With {.Scope = ns, .Name = ti.Name, .TypeInfo = ti}
            Me.AddTypeCache(ti, ns, s)

            If ns.Namespaces.ContainsKey(ti.Name) Then

                'ToDo: generics suport
            Else
                s.FunctionNamespace = New RkCILArrayNamespace With {.Name = ti.Name, .Parent = ns, .Root = Me, .BaseType = s}
                ns.AddNamespace(s.FunctionNamespace)
            End If

            Return s
        End Function

        Public Overridable Function LoadType(ti As TypeInfo) As RkCILStruct

            If Me.TypeCache.ContainsKey(ti) Then Return Me.TypeCache(ti)

            Dim ns = Me.CreateNamespace(ti.Namespace)
            Dim s = New RkCILStruct With {.Scope = ns, .Name = ti.Name, .TypeInfo = ti}
            Me.AddTypeCache(ti, ns, s)

            If ti.IsEnum Then

                ti.GetEnumNames.Each(Sub(name) s.AddLet(name, s))
            Else

                If ns.Namespaces.ContainsKey(ti.Name) Then

                    'ToDo: generics suport
                Else

                    s.FunctionNamespace = New RkCILNamespace With {.Name = ti.Name, .Parent = ns, .Root = Me, .BaseType = s}
                    ns.AddNamespace(s.FunctionNamespace)
                End If
            End If

            Return s
        End Function

        Public Overridable Sub AddTypeCache(ti As TypeInfo, ns As RkNamespace, s As RkCILStruct)

            Me.TypeCache(ti) = s

            If ti.IsGenericType Then

                Dim type_name = ti.Name
                Dim gens = ti.GenericTypeParameters
                Dim suffix = $"`{gens.Length}"
                If type_name.EndsWith(suffix) Then type_name = type_name.Substring(0, type_name.Length - suffix.Length)
                gens.Each(Sub(x) s.DefineGeneric(x.Name))

                ns.AddStruct(s, type_name)
            Else

                ns.AddStruct(s)
            End If
        End Sub

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

            For Each t In scope.FindCurrentStruct(name).By(Of RkStruct).Where(Function(x) x.Apply.Count = args.Length AndAlso Not x.HasGeneric)

                If t.Apply.And(Function(x, i) x Is args(i)) Then Return t
            Next

            For Each t In scope.FindCurrentStruct(name).By(Of RkStruct).Where(Function(x) x.Generics.Count = args.Length AndAlso x.HasGeneric)

                Return CType(t.FixedGeneric(args), RkStruct)
            Next

            For Each t In scope.FindCurrentStruct(name).By(Of RkClass).Where(Function(x) x.Generics.Count = args.Length)

                Return CType(t.FixedGeneric(args), RkClass)
            Next

            If args.Length = 0 Then

                For Each t In scope.FindCurrentStruct(name).By(Of RkClass)

                    Return t
                Next

                For Each t In scope.FindCurrentStruct(name).By(Of RkUnionType)

                    Return t
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

        Public Shared Function LoadClass(scope As IScope, name As String, ParamArray args() As IType) As RkClass

            Dim x = TryLoadClass(scope, name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Shared Function TryCurrentLoadClass(scope As IScope, name As String, ParamArray args() As IType) As RkClass

            For Each t In scope.FindCurrentStruct(name).By(Of RkClass).Where(Function(x) x.Generics.Count = args.Length)

                Return t
            Next

            Return Nothing
        End Function

        Public Shared Function TryLoadClass(scope As IScope, name As String, ParamArray args() As IType) As RkClass

            If scope Is Nothing Then Return Nothing

            Dim x = TryCurrentLoadClass(scope, name, args)
            If x IsNot Nothing Then Return x

            If TypeOf scope Is RkNamespace Then

                Dim ns = CType(scope, RkNamespace)
                For Each path In ns.LoadPaths

                    If TypeOf path Is RkClass Then

                        Dim class_ = CType(path, RkClass)
                        If class_.Name.Equals(name) Then Return class_
                    End If

                    If TypeOf path Is IScope Then

                        x = TryLoadClass(CType(path, IScope), name, args)
                        If x IsNot Nothing Then Return x
                    End If
                Next

                Return Nothing
            Else

                Return TryLoadClass(scope.Parent, name, args)
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

                Return Nothing

            ElseIf fs.Count = 1 Then

                Return fs(0).ApplyFunction(scope, args)
            Else

                fs.Done(Function(x) x.ApplyFunction(scope, args))
                Dim unique As New List(Of IFunction)
                For Each f In fs

                    Dim same = False
                    For Each u In unique

                        If f.GenericBase Is u.GenericBase Then

                            same = f.Apply.And(Function(x, i) x IsNot Nothing AndAlso x.Is(u.Apply(i)))
                            If same Then Exit For
                        End If
                    Next
                    If Not same Then unique.Add(f)
                Next
                If unique.Count = 1 Then Return unique(0)
                Dim union As New RkUnionType(unique) With {.Dynamic = False}
                If union.Types.Count = 1 Then Return CType(union.Types(0), IFunction)
                Return union
            End If
        End Function

        Public Shared Function FindLoadFunction(scope As IScope, name As String) As IEnumerable(Of IFunction)

            Return FindLoadFunction(scope, name, Function(x) True)
        End Function

        Public Shared Function FindLoadFunction(scope As IScope, name As String, ParamArray args() As IType) As IEnumerable(Of IFunction)

            Dim arg_any_indefinite = args.Or(Function(x) x IsNot Nothing AndAlso x.HasIndefinite)

            Dim fs = FindLoadFunction(scope, name, Function(x) Not x.HasIndefinite AndAlso (Not arg_any_indefinite OrElse x.GenericBase Is Nothing) AndAlso x.Arguments.Count = args.Length AndAlso x.Arguments.And(Function(arg, i) FixedByName(args(i)).If(Function(fix) fix.Is(arg.Value), Function() True)))
            If fs.Or(Function(x) x.HasGeneric) AndAlso fs.Or(Function(x) Not x.HasGeneric) Then

                Return fs.Where(Function(x) Not x.HasGeneric)
            End If
            Return fs
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

            t = FixedByName(t)
            If TypeOf t IsNot RkCILStruct Then Return False
            Dim get_base_root As Func(Of RkStruct, RkStruct) = Function(x) If(x.GenericBase Is Nothing, x, get_base_root(x.GenericBase))
            Return Me.Structs("Array")(0) Is get_base_root(CType(t, RkCILStruct))
        End Function

        Public Overridable Function GetArrayType(t As IType) As IType

            t = FixedByName(t)
            If Not Me.IsArray(t) Then Throw New Exception("not array")
            Return CType(t, RkCILStruct).Apply(0)
        End Function

        Public Shared Function TypeIs(a As IType, b As IType) As Boolean

            a = FixedByName(a)
            b = FixedByName(b)

            If a Is b Then Return True
            Return False
        End Function

        Public Shared Function FixedByName(t As IType) As IType

            If TypeOf t Is RkByNameWithReceiver Then

                Return FixedByName(CType(t, RkByNameWithReceiver).Type)

            ElseIf TypeOf t Is RkByName Then

                Return FixedByName(CType(t, RkByName).Type)

            ElseIf TypeOf t Is RkGenericEntry Then

                Return FixedByName(CType(t, RkGenericEntry).ToType)

            ElseIf TypeOf t Is RkUnionType Then

                Dim union = CType(t, RkUnionType)
                If union.Types?.Count = 1 Then Return FixedByName(union.Types(0))
                Return union
            Else

                Return t
            End If
        End Function

        Public Shared Function TypeToString(t As IType) As String

            t = FixedByName(t)
            If t Is Nothing Then Return "_"
            Return t.ToString
        End Function

        Public Shared Function CopyGenericEntry(clone As IApply, t As RkGenericEntry) As RkGenericEntry

            Return New RkGenericEntry With {.Name = t.Name, .Scope = t.Scope, .ApplyIndex = t.ApplyIndex, .Reference = clone}
        End Function

        Public Shared Function RelatedGeneric(t As IType) As Boolean

            If t Is Nothing Then Return False
            If t.HasGeneric Then Return True

            If TypeOf t Is RkFunction Then

                Dim func = CType(t, RkFunction)
                Return RelatedGeneric(func.Return) OrElse func.Arguments.Or(Function(x) RelatedGeneric(x.Value))
            End If

            Return False
        End Function

        Public Shared Function CopyType(base As IApply, dest As IApply, t As IType) As IType

            If t Is Nothing OrElse Not RelatedGeneric(t) Then

                Return t

            ElseIf TypeOf t Is RkGenericEntry Then

                Return CopyGenericEntry(dest, CType(t, RkGenericEntry))
            Else

                Dim clones As New Dictionary(Of IApply, IApply)
                clones.Add(base, dest)

                Dim deep_copy As Func(Of IType, IType) =
                    Function(value)

                        If value Is Nothing Then Return Nothing
                        If TypeOf value Is RkGenericEntry Then

                            Dim g = CType(value, RkGenericEntry)
                            'If g.Reference Is base Then Return CopyGenericEntry(dest, g)
                            If clones.ContainsKey(g.Reference) Then Return CopyGenericEntry(clones(g.Reference), g)
                            Return value
                        End If

                        Dim clone = value.CloneGeneric()
                        If TypeOf value Is IApply Then clones.Add(CType(value, IApply), CType(clone, IApply))

                        If TypeOf clone Is RkStruct Then

                            Dim struct = CType(clone, RkStruct)
                            Dim s = CType(value, RkStruct)
                            s.CopyGeneric(struct, True)
                            struct.Apply.Done(Function(x) deep_copy(x))
                            struct.Local.Clear()
                            s.Local.Each(Sub(kv) struct.Local.Add(kv.Key, deep_copy(kv.Value)))
                            Return struct

                        ElseIf TypeOf clone Is RkFunction Then

                            Dim func = CType(clone, RkFunction)
                            CType(value, RkFunction).CopyGeneric(func, True)
                            func.Arguments.Each(Sub(x) x.Value = deep_copy(x.Value))
                            If func.Return IsNot Nothing Then func.Return = deep_copy(func.Return)
                            Return func

                        ElseIf TypeOf clone Is RkTuple Then

                            Dim tuple = CType(clone, RkTuple)
                            Throw New Exception("no test")
                            Return tuple
                        End If

                        Return clone
                    End Function

                Return deep_copy(t)
            End If
        End Function

        Public Overridable Function CreateTuple(name As String, tuples() As IType) As RkStruct

            Dim same_type = Me.TupleCache.FindFirstOrNull(Function(x) x.Value.Name.Equals(name)).Value
            If same_type IsNot Nothing Then

                Return same_type
            Else

                Dim t As New RkStruct With {.Name = name, .Scope = Me, .Parent = Me, .IsTuple = True}
                'Dim alloc = LoadFunction(Me, "#Alloc", t)
                tuples.Each(Sub(x, i) t.AddLet((i + 1).ToString, x))
                Me.TupleCache(t) = t
                Me.AddStruct(t)
                Return t
            End If
        End Function

        Public Overridable Function CreateTuple(tuples() As IType) As RkStruct

            Return Me.CreateTuple($"({String.Join(", ", tuples.Map(Function(x) x?.ToString))})", tuples)
        End Function

        Public Overridable Function CreateTuple(tuple As RkTuple) As RkStruct

            If Me.TupleCache.ContainsKey(tuple) Then Return Me.TupleCache(tuple)

            Dim name = $"({String.Join(", ", tuple.Local.SortToList(Function(a, b) Convert.ToInt32(a.Key) - Convert.ToInt32(b.Key)).Map(Function(x) x.Value.ToString))})"
            Dim same_type = Me.TupleCache.FindFirstOrNull(Function(x) x.Value.Name.Equals(name)).Value
            If same_type IsNot Nothing Then

                Me.TupleCache(tuple) = same_type
                Return same_type
            Else

                Dim t As New RkStruct With {.Name = name, .Scope = Me, .Parent = Me, .IsTuple = True}
                'Dim alloc = LoadFunction(Me, "#Alloc", t)
                tuple.Local.Each(Sub(x) t.AddLet(x.Key, x.Value))
                Me.TupleCache(tuple) = t
                Me.AddStruct(t)
                Return t
            End If

        End Function

        Public Shared Function TypeFeedback(base As IType, t As IType) As Boolean

            If TypeOf base Is RkUnionType Then

                Return CType(base, RkUnionType).Merge(t)
            End If

            Return False
        End Function

    End Class

End Namespace

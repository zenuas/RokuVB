Imports System
Imports System.Collections.Generic
Imports System.Reflection


Namespace Manager

    Public Class SystemLirary
        Inherits RkNamespace


        Public Overridable ReadOnly Property TypeCache As New Dictionary(Of TypeInfo, RkCILStruct)

        Public Sub New()

            Me.Initialize()
        End Sub

        Public Overridable Sub Initialize()

            ' struct Numeric
            ' sub +(@T: Numeric)(@T, @T) @T
            Dim num As New RkStruct With {.Name = "Numeric", .Namespace = Me}
            Dim add_native_function =
                Function(name As String, op As RkOperator) As RkNativeFunction

                    Dim f As New RkNativeFunction With {.Name = name, .Operator = op, .Namespace = Me}
                    Dim f_t = f.DefineGeneric("@T")
                    f.Return = f_t
                    f.Arguments.Add(New NamedValue With {.Name = "left", .Value = f_t})
                    f.Arguments.Add(New NamedValue With {.Name = "right", .Value = f_t})
                    Me.AddFunction(f)
                    Return f
                End Function
            Dim num_plus = add_native_function("+", RkOperator.Plus)
            Dim num_minus = add_native_function("-", RkOperator.Minus)
            Dim num_mul = add_native_function("*", RkOperator.Mul)
            Dim num_div = add_native_function("/", RkOperator.Div)
            Dim num_eq = add_native_function("==", RkOperator.Equal)
            Dim num_gt = add_native_function(">", RkOperator.Gt)
            Dim num_gte = add_native_function(">=", RkOperator.Gte)
            Dim num_lt = add_native_function("<", RkOperator.Lt)
            Dim num_lte = add_native_function("<=", RkOperator.Lte)
            Me.AddStruct(num)

            ' struct Int32 : Numeric
            Dim define_num =
                Function(t As Type)

                    Dim x = Me.LoadType(t.GetTypeInfo)
                    x.Super = num
                    Me.AddStruct(x)
                    Return x
                End Function
            Dim int64 = define_num(GetType(Int64))
            Dim int32 = define_num(GetType(Int32))
            Dim int16 = define_num(GetType(Int16))
            Dim int8 = define_num(GetType(Byte))
            Me.AddStruct(int32, "Int")

            ' struct Array(@T)
            ' struct Char : Int16
            ' struct String : Array(Char)
            Dim arr As New RkStruct With {.Name = "Array", .Namespace = Me}
            Dim arr_t = arr.DefineGeneric("@T")
            Me.AddStruct(arr)
            Dim chr = Me.LoadType(GetType(Char).GetTypeInfo)
            Me.AddStruct(chr)
            Dim str = Me.LoadType(GetType(String).GetTypeInfo) 'As New RkStruct With {.Name = "String", .Super = arr.FixedGeneric(chr), .Namespace = Me}
            Me.AddStruct(str)

            ' sub [](self: Array(@T), index: Int32) @T
            Dim array_index As New RkFunction With {.Name = "[]", .Namespace = Me}
            Dim array_index_t = array_index.DefineGeneric("@T")
            array_index.Arguments.Add(New NamedValue With {.Name = "self", .Value = arr})
            array_index.Arguments.Add(New NamedValue With {.Name = "index", .Value = int32})
            array_index.Return = array_index_t
            Me.AddFunction(array_index)

            ' sub print(s: @T)
            Dim print As New RkFunction With {.Name = "print", .Namespace = Me}
            Dim print_t = print.DefineGeneric("@T")
            print.Arguments.Add(New NamedValue With {.Name = "s", .Value = print_t})
            Me.AddFunction(print)

            ' sub return(s: @T)
            Dim return_ As New RkFunction With {.Name = "return", .Namespace = Me}
            Dim return_t = return_.DefineGeneric("@T")
            return_.Arguments.Add(New NamedValue With {.Name = "x", .Value = return_t})
            Me.AddFunction(return_)

            Dim alloc As New RkNativeFunction With {.Name = "#Alloc", .Operator = RkOperator.Alloc, .Namespace = Me}
            Dim alloc_t = alloc.DefineGeneric("@T")
            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = alloc_t})
            alloc.Return = alloc_t
            Me.AddFunction(alloc)

        End Sub

        Public Overridable Function CreateNamespace(name As String) As RkNamespace

            Return Me.CreateNamespace(name, Me)
        End Function

        Public Overridable Function CreateNamespace(name As String, current As RkNamespace) As RkNamespace

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
            'Dim type_name = ti.Name
            'If ti.GenericTypeParameters.Length > 0 Then

            '    Dim suffix = $"`{ti.GenericTypeParameters.Length}"
            '    If type_name.EndsWith(suffix) Then type_name = type_name.Substring(0, type_name.Length - suffix.Length)
            'End If

            Dim ns = Me.CreateNamespace(ti.Namespace)
            Dim s = New RkCILStruct With {.Namespace = ns, .Name = ti.Name, .TypeInfo = ti}
            Me.TypeCache(ti) = s
            ns.AddStruct(s)

            For Each method In ti.GetMethods

                Dim f As New RkCILFunction With {.Namespace = ns, .Name = method.Name, .MethodInfo = method}
                If Not method.IsStatic Then f.Arguments.Add(New NamedValue With {.Name = "self", .Value = s})
                For Each arg In method.GetParameters

                    f.Arguments.Add(New NamedValue With {.Name = arg.Name, .Value = Me.LoadType(CType(arg.ParameterType, TypeInfo))})
                Next
                If method.ReturnType IsNot Nothing AndAlso Not method.ReturnType.Equals(GetType(System.Void)) Then f.Return = Me.LoadType(CType(method.ReturnType, TypeInfo))

                Me.CreateNamespace(ti.Name, ns).AddFunction(f)
            Next

            Return s
        End Function

    End Class

End Namespace

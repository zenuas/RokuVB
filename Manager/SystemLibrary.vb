Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Util.ArrayExtension


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

            ' struct Array(@T) : List(@T)
            ' struct Char : Char
            ' struct String : String
            Dim arr = Me.LoadType(GetType(List(Of )).GetTypeInfo)
            Me.AddStruct(arr, "Array")
            Dim chr = Me.LoadType(GetType(Char).GetTypeInfo)
            Me.AddStruct(chr)
            Dim str = Me.LoadType(GetType(String).GetTypeInfo)
            Me.AddStruct(str)

            ' sub [](self: Array(@T), index: Int32) @T
            Dim array_index = arr.FunctionNamespace.LoadFunction("get_Item", arr, int32)
            Me.AddFunction(array_index, "[]")

            ' sub print(s: @T)
            Dim print_str = Me.LoadType(GetType(System.Console).GetTypeInfo).FunctionNamespace.LoadFunction("WriteLine", str)
            Dim print_int = Me.LoadType(GetType(System.Console).GetTypeInfo).FunctionNamespace.LoadFunction("WriteLine", int32)
            Me.AddFunction(print_str, "print")
            Me.AddFunction(print_int, "print")

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
            Dim s = New RkCILStruct With {.Namespace = ns, .Name = ti.Name, .TypeInfo = ti}
            Me.TypeCache(ti) = s
            ns.AddStruct(s)

            If ti.IsGenericType Then

                Dim type_name = ti.Name
                Dim gens = ti.GenericTypeParameters
                Dim suffix = $"`{gens.Length}"
                If type_name.EndsWith(suffix) Then s.Name = type_name.Substring(0, type_name.Length - suffix.Length)
                gens.Do(Sub(x) s.DefineGeneric(x.Name))
            End If

            If ns.Namespaces.ContainsKey(ti.Name) Then

                'ToDo: generics suport
            Else
                s.FunctionNamespace = New RkCILNamespace With {.Name = ti.Name, .Parent = ns, .Root = Me, .BaseType = s}
                ns.AddNamespace(s.FunctionNamespace)
            End If

            Return s
        End Function

    End Class

End Namespace

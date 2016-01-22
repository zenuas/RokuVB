
Namespace Manager

    Public Class SystemLirary
        Inherits RkNamespace

        Public Sub New()

            Me.Initialize()
        End Sub

        Public Overridable Sub Initialize()

            ' struct Numeric
            ' sub +(@T: Numeric)(@T, @T) @T
            Dim num As New RkStruct With {.Name = "Numeric"}
            Dim add_native_function =
                Function(name As String, op As RkOperator) As RkNativeFunction

                    Dim f As New RkNativeFunction With {.Name = name, .Operator = op}
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
                Function(name As String, byte_size As Integer)

                    Dim x As New RkStruct With {.Name = name}
                    x.Super = num
                    Me.AddStruct(x)
                    Return x
                End Function
            Dim int64 = define_num("Int64", 8)
            Dim int32 = define_num("Int32", 4)
            Dim int16 = define_num("Int16", 2)
            Dim int8 = define_num("Int8", 1)
            Me.Structs.Add("Int", int32)

            ' struct Array(@T)
            ' struct Char : Int16
            ' struct String : Array(Char)
            Dim arr As New RkStruct With {.Name = "Array"}
            Dim arr_t = arr.DefineGeneric("@T")
            Me.AddStruct(arr)
            Dim chr As New RkStruct With {.Name = "Char", .Super = int16}
            Me.AddStruct(chr)
            Dim str As New RkStruct With {.Name = "String", .Super = arr.FixedGeneric(chr)}
            Me.AddStruct(str)

            ' sub print(s: @T)
            Dim print As New RkFunction With {.Name = "print"}
            Dim print_t = print.DefineGeneric("@T")
            print.Arguments.Add(New NamedValue With {.Name = "s", .Value = print_t})
            Me.AddFunction(print)

            ' sub return(s: @T)
            Dim return_ As New RkFunction With {.Name = "return"}
            Dim return_t = return_.DefineGeneric("@T")
            return_.Arguments.Add(New NamedValue With {.Name = "x", .Value = return_t})
            Me.AddFunction(return_)

            Dim alloc As New RkNativeFunction With {.Name = "#Alloc", .Operator = RkOperator.Alloc}
            Dim alloc_t = alloc.DefineGeneric("@T")
            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = alloc_t})
            alloc.Return = alloc_t
            Me.AddFunction(alloc)

        End Sub

    End Class

End Namespace

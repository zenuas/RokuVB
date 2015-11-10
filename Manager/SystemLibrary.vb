
Namespace Manager

    Public Class SystemLirary
        Inherits RkStruct

        Public Sub New()

            Me.Initialize()
        End Sub

        Public Sub Initialize()

            ' struct Numeric(@T)
            '    sub +(@T, @T) @T
            Dim num As New RkStruct With {.Name = "Numeric"}
            Dim num_t = num.DefineGeneric("@T")
            Dim add_native_function =
                Sub(name As String, op As RkOperator)

                    Dim f As New RkNativeFunction With {.Name = name, .Operator = op}
                    Dim f_t = f.DefineGeneric(num_t.Name)
                    f_t.Reference = num_t
                    f.Return = f_t
                    f.Arguments.Add(New NamedValue With {.Name = "left", .Value = f_t})
                    f.Arguments.Add(New NamedValue With {.Name = "right", .Value = f_t})
                    num.Local.Add(name, f)
                End Sub
            add_native_function("+", RkOperator.Plus)
            add_native_function("-", RkOperator.Minus)
            add_native_function("*", RkOperator.Mul)
            add_native_function("/", RkOperator.Div)
            add_native_function("==", RkOperator.Equal)
            add_native_function(">", RkOperator.Gt)
            add_native_function(">=", RkOperator.Gte)
            add_native_function("<", RkOperator.Lt)
            add_native_function("<=", RkOperator.Lte)
            Me.Local.Add(num.Name, num)

            ' struct Int32 : Numeric.of(Int32)
            Dim define_num =
                Function(name As String)

                    Dim x As New RkStruct With {.Name = name}
                    x.Super = num.FixedGeneric(x)
                    Me.Local.Add(x.Name, x)
                    Return x
                End Function
            Dim int64 = define_num("Int64")
            Dim int32 = define_num("Int32")
            Dim int16 = define_num("Int16")
            Dim int8 = define_num("Int8")
            Me.Local.Add("Int", int32)
        End Sub

    End Class

End Namespace

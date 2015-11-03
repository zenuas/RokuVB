
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
            Me.AddNativeFunction(num, "+", RkOperator.Plus, num_t)
            Me.AddNativeFunction(num, "-", RkOperator.Minus, num_t)
            Me.AddNativeFunction(num, "*", RkOperator.Mul, num_t)
            Me.AddNativeFunction(num, "/", RkOperator.Div, num_t)
            Me.AddNativeFunction(num, "==", RkOperator.Equal, num_t)
            Me.AddNativeFunction(num, ">", RkOperator.Gt, num_t)
            Me.AddNativeFunction(num, ">=", RkOperator.Gte, num_t)
            Me.AddNativeFunction(num, "<", RkOperator.Lt, num_t)
            Me.AddNativeFunction(num, "<=", RkOperator.Lte, num_t)
            Me.Local.Add(num.Name, num)

            ' struct Int32 : Numeric.of(Int32)
            Dim int32 As New RkStruct With {.Name = "Int32"}
            int32.Super = num.FixedGeneric(New RkStruct With {.Name = "Int32"})
            Me.Local.Add(int32.Name, int32)
        End Sub

        Public Overridable Sub AddNativeFunction(v As RkStruct, name As String, op As RkOperator, t As IType)

            Dim f As New RkNativeFunction With {.Name = name, .Operator = op, .Return = t}
            f.Arguments.Add(New NamedValue(Of IType) With {.Name = "left", .Value = t})
            f.Arguments.Add(New NamedValue(Of IType) With {.Name = "right", .Value = t})
            v.Local.Add(name, f)
        End Sub
    End Class

End Namespace

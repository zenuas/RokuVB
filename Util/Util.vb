Imports System
Imports System.Collections.Generic


Public Class Util

    Public Shared Function T(Of A)([then] As A, [else] As A) As A

        Return [then]
    End Function

    Public Shared Function F(Of A)([then] As A, [else] As A) As A

        Return [else]
    End Function

    Public Shared Function Car(Of T)(ParamArray xs() As T) As T

        Return xs(0)
    End Function

    Public Shared Iterator Function Cdr(Of T)(ParamArray xs() As T) As IEnumerable(Of T)

        For i = 1 To xs.Length - 1

            Yield xs(i)
        Next
    End Function

    Public Shared Iterator Function Range(Of T)(xs() As T, from As Integer) As IEnumerable(Of T)

        For i = [from] To xs.Length - 1

            Yield xs(i)
        Next
    End Function

    Public Shared Iterator Function Range(Of T)(xs() As T, from As Integer, [to] As Integer) As IEnumerable(Of T)

        For i = [from] To [to]

            Yield xs(i)
        Next
    End Function

    Public Shared Function Split(Of T)(ParamArray xs() As T) As Tuple(Of T, IEnumerable(Of T))

        Return Tuple.Create(Car(xs), Cdr(xs))
    End Function

    Public Shared Function Split(Of T)(xs() As T, f As Func(Of T, Integer, Boolean)) As Tuple(Of List(Of T), List(Of T))

        Dim false_part As New List(Of T)
        Dim true_part As New List(Of T)
        For i = 0 To xs.Length - 1

            Dim x = xs(i)
            If f(x, i) Then

                true_part.Add(x)
            Else
                false_part.Add(x)
            End If
        Next
        Return Tuple.Create(false_part, true_part)
    End Function

    Public Shared Function Split(Of T)(xs() As T, f As Func(Of T, Boolean)) As Tuple(Of List(Of T), List(Of T))

        Return Split(xs, Function(x, i) f(x))
    End Function

    Public Shared Function Cons(Of T)(ParamArray xs() As T) As T()

        Return xs
    End Function

    Public Shared Iterator Function Join(Of T)(xs() As T, ys() As T) As IEnumerable(Of T)

        For Each x In xs : Yield x : Next
        For Each y In ys : Yield y : Next
    End Function

    Public Shared Function List(Of T)(ParamArray xs() As T) As List(Of T)

        Return New List(Of T)(xs)
    End Function

    Public Shared Function Null(Of T)(xs() As T) As Boolean

        Return xs.Length = 0
    End Function

    Public Shared Function Tee(Of T)(a As T, f As Action(Of T)) As T

        f(a)
        Return a
    End Function

    Public Shared Function These(Of R)(ParamArray fs() As Func(Of R)) As R

        For i = 0 To fs.Length - 1

            Try
                Return fs(i)()

            Catch

            End Try
        Next
        Return Nothing
    End Function

    Public Shared Function These(Of T, R)(a As T, ParamArray fs() As Func(Of T, R)) As R

        For i = 0 To fs.Length - 1

            Try
                Return fs(i)(a)

            Catch

            End Try
        Next
        Return Nothing
    End Function

    Public Shared Iterator Function Map(Of T, R)(xs() As T, f As Func(Of T, Integer, R)) As IEnumerable(Of R)

        For i = 0 To xs.Length - 1

            Yield f(xs(i), i)
        Next
    End Function

    Public Shared Iterator Function Map(Of T, R)(xs() As T, f As Func(Of T, R)) As IEnumerable(Of R)

        For i = 0 To xs.Length - 1

            Yield f(xs(i))
        Next
    End Function

    Public Shared Iterator Function Apply(Of T)(xs() As T, f As Func(Of T, Integer, T)) As IEnumerable(Of T)

        For i = 0 To xs.Length - 1

            Dim x = f(xs(i), i)
            xs(i) = x
            Yield x
        Next
    End Function

    Public Shared Iterator Function Apply(Of T)(xs() As T, f As Func(Of T, T)) As IEnumerable(Of T)

        For i = 0 To xs.Length - 1

            Dim x = f(xs(i))
            xs(i) = x
            Yield x
        Next
    End Function

    Public Shared Iterator Function Where(Of T)(xs() As T, f As Func(Of T, Integer, Boolean)) As IEnumerable(Of T)

        For i = 0 To xs.Length - 1

            Dim x = xs(i)
            If f(x, i) Then Yield x
        Next
    End Function

    Public Shared Iterator Function Where(Of T)(xs() As T, f As Func(Of T, Boolean)) As IEnumerable(Of T)

        For i = 0 To xs.Length - 1

            Dim x = xs(i)
            If f(x) Then Yield x
        Next
    End Function

End Class

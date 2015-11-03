Imports System
Imports System.Collections.Generic


Namespace Util

    Public Class Functions

        Public Shared Function T(Of A)([then] As A, [else] As A) As A

            Return [then]
        End Function

        Public Shared Function F(Of A)([then] As A, [else] As A) As A

            Return [else]
        End Function

        Public Shared Function Car(Of T)(xs As IEnumerable(Of T)) As T

            Dim e = xs.GetEnumerator
            e.MoveNext()
            Return e.Current
        End Function

        Public Shared Iterator Function Cdr(Of T)(xs As IEnumerable(Of T)) As IEnumerable(Of T)

            Dim e = xs.GetEnumerator
            If Not e.MoveNext Then Return
            Do While e.MoveNext

                Yield e.Current
            Loop
        End Function

        Public Shared Iterator Function Range(Of T)(xs As IList(Of T), from As Integer) As IEnumerable(Of T)

            For i = [from] To xs.Count - 1

                Yield xs(i)
            Next
        End Function

        Public Shared Iterator Function Range(Of T)(xs As IList(Of T), from As Integer, [to] As Integer) As IEnumerable(Of T)

            For i = [from] To [to]

                Yield xs(i)
            Next
        End Function

        Public Shared Function Split(Of T)(xs As IEnumerable(Of T)) As Tuple(Of T, IEnumerable(Of T))

            Return Tuple.Create(Car(xs), Cdr(xs))
        End Function

        Public Shared Function Split(Of T)(xs As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Tuple(Of List(Of T), List(Of T))

            Dim false_part As New List(Of T)
            Dim true_part As New List(Of T)
            Dim i = 0
            For Each x In xs

                If f(x, i) Then

                    true_part.Add(x)
                Else
                    false_part.Add(x)
                End If
                i += 1
            Next
            Return Tuple.Create(false_part, true_part)
        End Function

        Public Shared Function Split(Of T)(xs As IEnumerable(Of T), f As Func(Of T, Boolean)) As Tuple(Of List(Of T), List(Of T))

            Return Split(xs, Function(x, i) f(x))
        End Function

        Public Shared Function Cons(Of T)(ParamArray xs() As T) As T()

            Return xs
        End Function

        Public Shared Iterator Function Join(Of T)(xs As IEnumerable(Of T), ys As IEnumerable(Of T)) As IEnumerable(Of T)

            For Each x In xs : Yield x : Next
            For Each y In ys : Yield y : Next
        End Function

        Public Shared Function List(Of T)(ParamArray xs() As T) As List(Of T)

            Return New List(Of T)(xs)
        End Function

        Public Shared Function List(Of T)(xs As IEnumerable(Of T)) As List(Of T)

            Return New List(Of T)(xs)
        End Function

        Public Shared Function Null(Of T)(xs As IEnumerable(Of T)) As Boolean

            Return Not xs.GetEnumerator.MoveNext
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

        Public Shared Sub [Do](Of T)(xs As IEnumerable(Of T), f As Action(Of T, Integer))

            Dim i = 0
            For Each x In xs

                f(x, i)
                i += 1
            Next
        End Sub

        Public Shared Sub [Do](Of T)(xs As IEnumerable(Of T), f As Action(Of T))

            For Each x In xs

                f(x)
            Next
        End Sub

        Public Shared Iterator Function Map(Of T, R)(xs As IEnumerable(Of T), f As Func(Of T, Integer, R)) As IEnumerable(Of R)

            Dim i = 0
            For Each x In xs

                Yield f(x, i)
                i += 1
            Next
        End Function

        Public Shared Iterator Function Map(Of T, R)(xs As IEnumerable(Of T), f As Func(Of T, R)) As IEnumerable(Of R)

            For Each x In xs

                Yield f(x)
            Next
        End Function

        Public Shared Iterator Function Apply(Of T)(xs As IList(Of T), f As Func(Of T, Integer, T)) As IEnumerable(Of T)

            For i = 0 To xs.Count - 1

                Dim x = f(xs(i), i)
                xs(i) = x
                Yield x
            Next
        End Function

        Public Shared Iterator Function Apply(Of T)(xs As IList(Of T), f As Func(Of T, T)) As IEnumerable(Of T)

            For i = 0 To xs.Count - 1

                Dim x = f(xs(i))
                xs(i) = x
                Yield x
            Next
        End Function

        Public Shared Iterator Function Where(Of T)(xs As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As IEnumerable(Of T)

            Dim i = 0
            For Each x In xs

                If f(x, i) Then Yield x
            Next
        End Function

        Public Shared Iterator Function Where(Of T)(xs As IEnumerable(Of T), f As Func(Of T, Boolean)) As IEnumerable(Of T)

            For Each x In xs

                If f(x) Then Yield x
            Next
        End Function

        Public Shared Function [And](Of T)(xs As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Boolean

            Dim i = 0
            For Each x In xs

                If Not f(x, i) Then Return False
                i += 1
            Next

            Return True
        End Function

        Public Shared Function [And](Of T)(xs As IEnumerable(Of T), f As Func(Of T, Boolean)) As Boolean

            For Each x In xs

                If Not f(x) Then Return False
            Next

            Return True
        End Function

        Public Shared Function [Or](Of T)(xs As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Boolean

            Dim i = 0
            For Each x In xs

                If f(x, i) Then Return True
                i += 1
            Next

            Return False
        End Function

        Public Shared Function [Or](Of T)(xs As IEnumerable(Of T), f As Func(Of T, Boolean)) As Boolean

            For Each x In xs

                If f(x) Then Return True
            Next

            Return False
        End Function

    End Class


End Namespace

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

        Public Shared Function Cons(Of T)(ParamArray xs() As T) As T()

            Return xs
        End Function

        Public Shared Iterator Function Join(Of T)(xs As IEnumerable(Of T), ys As IEnumerable(Of T)) As IEnumerable(Of T)

            For Each x In xs : Yield x : Next
            For Each y In ys : Yield y : Next
        End Function

        Public Shared Function Tee(Of T)(a As T, f As Action(Of T)) As T

            f(a)
            Return a
        End Function

        Public Shared Function Memoization(f As Action) As Action

            Dim isfirst = True
            Return Sub()

                       If isfirst Then

                           f()
                           isfirst = False
                       End If
                   End Sub
        End Function

        Public Shared Function Memoization(Of T)(f As Func(Of T)) As Func(Of T)

            Dim var As T
            Dim isfirst = True
            Return Function() As T

                       If isfirst Then

                           var = f()
                           isfirst = False
                       End If
                       Return var
                   End Function
        End Function

        <Obsolete>
        Public Shared Function Singleton(Of T)(f As Func(Of T)) As Func(Of T)

            Return Memoization(f)
        End Function

#Region "Curry(Of T1[, R])"

        Public Shared Function Curry(Of T1, R)(f As Func(Of T1, R)) As Func(Of T1, Func(Of R))

            Return Function(x1) Function() f(x1)
        End Function

        Public Shared Function Curry(Of T1)(f As Action(Of T1)) As Func(Of T1, Action)

            Return Function(x1) Sub() f(x1)
        End Function

#End Region

#Region "Curry(Of T1, T2[, R])"

        Public Shared Function Curry(Of T1, T2, R)(f As Func(Of T1, T2, R)) As Func(Of T1, Func(Of T2, R))

            Return Function(x1) Function(x2) f(x1, x2)
        End Function

        Public Shared Function Curry(Of T1, T2)(f As Action(Of T1, T2)) As Func(Of T1, Action(Of T2))

            Return Function(x1) Sub(x2) f(x1, x2)
        End Function

#End Region

#Region "Curry(Of T1, T2, T3[, R])"

        Public Shared Function Curry(Of T1, T2, T3, R)(f As Func(Of T1, T2, T3, R)) As Func(Of T1, Func(Of T2, T3, R))

            Return Function(x1) Function(x2, x3) f(x1, x2, x3)
        End Function

        Public Shared Function Curry(Of T1, T2, T3)(f As Action(Of T1, T2, T3)) As Func(Of T1, Action(Of T2, T3))

            Return Function(x1) Sub(x2, x3) f(x1, x2, x3)
        End Function

#End Region

#Region "Curry(Of T1, T2, T3, T4[, R])"

        Public Shared Function Curry(Of T1, T2, T3, T4, R)(f As Func(Of T1, T2, T3, T4, R)) As Func(Of T1, Func(Of T2, T3, T4, R))

            Return Function(x1) Function(x2, x3, x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Curry(Of T1, T2, T3, T4)(f As Action(Of T1, T2, T3, T4)) As Func(Of T1, Action(Of T2, T3, T4))

            Return Function(x1) Sub(x2, x3, x4) f(x1, x2, x3, x4)
        End Function

#End Region

#Region "Bind(Of T1[, R])"

        Public Shared Function Bind(Of T1, R)(f As Func(Of T1, R), x1 As T1) As Func(Of R)

            Return Function() f(x1)
        End Function

        Public Shared Function Bind(Of T1)(f As Action(Of T1), x1 As T1) As Action

            Return Sub() f(x1)
        End Function

#End Region

#Region "Bind(Of T1, T2[, R])"

        Public Shared Function Bind(Of T1, T2, R)(f As Func(Of T1, T2, R), x1 As T1) As Func(Of T2, R)

            Return Function(x2) f(x1, x2)
        End Function

        Public Shared Function Bind(Of T1, T2, R)(f As Func(Of T1, T2, R), x1 As T1, x2 As T2) As Func(Of R)

            Return Function() f(x1, x2)
        End Function

        Public Shared Function Bind(Of T1, T2)(f As Action(Of T1, T2), x1 As T1) As Action(Of T2)

            Return Sub(x2) f(x1, x2)
        End Function

        Public Shared Function Bind(Of T1, T2)(f As Action(Of T1, T2), x1 As T1, x2 As T2) As Action

            Return Sub() f(x1, x2)
        End Function

#End Region

#Region "Bind(Of T1, T2, T3[, R])"

        Public Shared Function Bind(Of T1, T2, T3, R)(f As Func(Of T1, T2, T3, R), x1 As T1) As Func(Of T2, T3, R)

            Return Function(x2, x3) f(x1, x2, x3)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, R)(f As Func(Of T1, T2, T3, R), x1 As T1, x2 As T2) As Func(Of T3, R)

            Return Function(x3) f(x1, x2, x3)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, R)(f As Func(Of T1, T2, T3, R), x1 As T1, x2 As T2, x3 As T3) As Func(Of R)

            Return Function() f(x1, x2, x3)
        End Function

        Public Shared Function Bind(Of T1, T2, T3)(f As Action(Of T1, T2, T3), x1 As T1) As Action(Of T2, T3)

            Return Sub(x2, x3) f(x1, x2, x3)
        End Function

        Public Shared Function Bind(Of T1, T2, T3)(f As Action(Of T1, T2, T3), x1 As T1, x2 As T2) As Action(Of T3)

            Return Sub(x3) f(x1, x2, x3)
        End Function

        Public Shared Function Bind(Of T1, T2, T3)(f As Action(Of T1, T2, T3), x1 As T1, x2 As T2, x3 As T3) As Action

            Return Sub() f(x1, x2, x3)
        End Function

#End Region

#Region "Bind(Of T1, T2, T3, T4[, R])"

        Public Shared Function Bind(Of T1, T2, T3, T4, R)(f As Func(Of T1, T2, T3, T4, R), x1 As T1) As Func(Of T2, T3, T4, R)

            Return Function(x2, x3, x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4, R)(f As Func(Of T1, T2, T3, T4, R), x1 As T1, x2 As T2) As Func(Of T3, T4, R)

            Return Function(x3, x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4, R)(f As Func(Of T1, T2, T3, T4, R), x1 As T1, x2 As T2, x3 As T3) As Func(Of T4, R)

            Return Function(x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4, R)(f As Func(Of T1, T2, T3, T4, R), x1 As T1, x2 As T2, x3 As T3, x4 As T4) As Func(Of R)

            Return Function() f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4)(f As Action(Of T1, T2, T3, T4), x1 As T1) As Action(Of T2, T3, T4)

            Return Sub(x2, x3, x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4)(f As Action(Of T1, T2, T3, T4), x1 As T1, x2 As T2) As Action(Of T3, T4)

            Return Sub(x3, x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4)(f As Action(Of T1, T2, T3, T4), x1 As T1, x2 As T2, x3 As T3) As Action(Of T4)

            Return Sub(x4) f(x1, x2, x3, x4)
        End Function

        Public Shared Function Bind(Of T1, T2, T3, T4)(f As Action(Of T1, T2, T3, T4), x1 As T1, x2 As T2, x3 As T3, x4 As T4) As Action

            Return Sub() f(x1, x2, x3, x4)
        End Function

#End Region

    End Class

End Namespace

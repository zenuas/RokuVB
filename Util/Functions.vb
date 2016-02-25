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

        Public Function Tee(Of T)(a As T, f As Action(Of T)) As T

            f(a)
            Return a
        End Function

    End Class

End Namespace

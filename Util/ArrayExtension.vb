Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices


Namespace Util

    Public Module ArrayExtension

        <Extension>
        Public Function Car(Of T)(self As IEnumerable(Of T)) As T

            Dim e = self.GetEnumerator
            e.MoveNext()
            Return e.Current
        End Function

        <Extension>
        Public Iterator Function Cdr(Of T)(self As IEnumerable(Of T)) As IEnumerable(Of T)

            Dim e = self.GetEnumerator
            If Not e.MoveNext Then Return
            Do While e.MoveNext

                Yield e.Current
            Loop
        End Function

        <Extension>
        Public Iterator Function Range(Of T)(self As IList(Of T), from As Integer) As IEnumerable(Of T)

            For i = [from] To self.Count - 1

                Yield self(i)
            Next
        End Function

        <Extension>
        Public Iterator Function Range(Of T)(self As IList(Of T), from As Integer, [to] As Integer) As IEnumerable(Of T)

            For i = [from] To [to]

                Yield self(i)
            Next
        End Function

        <Extension>
        Public Function Split(Of T)(self As IEnumerable(Of T)) As Tuple(Of T, IEnumerable(Of T))

            Return Tuple.Create(self.Car, self.Cdr)
        End Function

        <Extension>
        Public Function Split(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Tuple(Of List(Of T), List(Of T))

            Dim false_part As New List(Of T)
            Dim true_part As New List(Of T)
            Dim i = 0
            For Each x In self

                If f(x, i) Then

                    true_part.Add(x)
                Else
                    false_part.Add(x)
                End If
                i += 1
            Next
            Return Tuple.Create(false_part, true_part)
        End Function

        <Extension>
        Public Function Split(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Tuple(Of List(Of T), List(Of T))

            Return Split(self, Function(x, i) f(x))
        End Function

        <Extension>
        Public Iterator Function Join(Of T)(self As IEnumerable(Of T), ParamArray xss() As IEnumerable(Of T)) As IEnumerable(Of T)

            For Each x In self

                Yield x
            Next
            For Each xs In xss

                For Each x In xs

                    Yield x
                Next
            Next
        End Function

        <Extension>
        Public Function ToList(Of T)(self As IEnumerable(Of T)) As List(Of T)

            Return New List(Of T)(self)
        End Function

        <Extension>
        Public Function ToArray(Of T)(self As IEnumerable(Of T)) As T()

            Return self.ToList.ToArray
        End Function

        <Extension>
        Public Function ToHash(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, R)) As Dictionary(Of T, R)

            Dim hash As New Dictionary(Of T, R)
            self.Do(Sub(x) hash(x) = f(x))
            Return hash
        End Function

        <Extension>
        Public Function ToHash(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, Integer, R)) As Dictionary(Of T, R)

            Dim hash As New Dictionary(Of T, R)
            self.Do(Sub(x, i) hash(x) = f(x, i))
            Return hash
        End Function

        <Extension>
        Public Iterator Function Reverse(Of T)(self As IList(Of T)) As IEnumerable(Of T)

            For i = self.Count - 1 To 0 Step -1

                Yield self(i)
            Next
        End Function

        <Extension>
        Public Function IsNull(Of T)(self As IEnumerable(Of T)) As Boolean

            Return Not self.GetEnumerator.MoveNext
        End Function

        <Extension>
        Public Sub [Do](Of T)(self As IEnumerable(Of T), f As Action(Of T, Integer))

            Dim i = 0
            For Each x In self

                f(x, i)
                i += 1
            Next
        End Sub

        <Extension>
        Public Sub [Do](Of T)(self As IEnumerable(Of T), f As Action(Of T))

            For Each x In self

                f(x)
            Next
        End Sub

        <Extension>
        Public Iterator Function Map(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, Integer, R)) As IEnumerable(Of R)

            Dim i = 0
            For Each x In self

                Yield f(x, i)
                i += 1
            Next
        End Function

        <Extension>
        Public Iterator Function Map(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, R)) As IEnumerable(Of R)

            For Each x In self

                Yield f(x)
            Next
        End Function

        <Extension>
        Public Iterator Function Apply(Of T)(self As IList(Of T), f As Func(Of T, Integer, T)) As IEnumerable(Of T)

            For i = 0 To self.Count - 1

                Dim x = f(self(i), i)
                self(i) = x
                Yield x
            Next
        End Function

        <Extension>
        Public Iterator Function Apply(Of T)(self As IList(Of T), f As Func(Of T, T)) As IEnumerable(Of T)

            For i = 0 To self.Count - 1

                Dim x = f(self(i))
                self(i) = x
                Yield x
            Next
        End Function

        <Extension>
        Public Iterator Function Where(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As IEnumerable(Of T)

            Dim i = 0
            For Each x In self

                If f(x, i) Then Yield x
            Next
        End Function

        <Extension>
        Public Iterator Function Where(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As IEnumerable(Of T)

            For Each x In self

                If f(x) Then Yield x
            Next
        End Function

        <Extension>
        Public Function Find(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As T

            Return self.Where(f).Car
        End Function

        <Extension>
        Public Function Find(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As T

            Return self.Where(f).Car
        End Function

        <Extension>
        Public Function FindLast(Of T)(self As IList(Of T), f As Func(Of T, Integer, Boolean)) As T

            Dim count = self.Count - 1
            Return self.Reverse.Where(Function(x, i) f(x, count - i)).Car
        End Function

        <Extension>
        Public Function FindLast(Of T)(self As IList(Of T), f As Func(Of T, Boolean)) As T

            Return self.Reverse.Where(f).Car
        End Function

        <Extension>
        Public Function [And](Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Boolean

            Dim i = 0
            For Each x In self

                If Not f(x, i) Then Return False
                i += 1
            Next

            Return True
        End Function

        <Extension>
        Public Function [And](Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Boolean

            For Each x In self

                If Not f(x) Then Return False
            Next

            Return True
        End Function

        <Extension>
        Public Function [Or](Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Boolean

            Dim i = 0
            For Each x In self

                If f(x, i) Then Return True
                i += 1
            Next

            Return False
        End Function

        <Extension>
        Public Function [Or](Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Boolean

            For Each x In self

                If f(x) Then Return True
            Next

            Return False
        End Function

        <Extension>
        Public Function IndexOf(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Integer

            Dim i = 0
            For Each x In self

                If f(x, i) Then Return i
                i += 1
            Next

            Return -1
        End Function

        <Extension>
        Public Function IndexOf(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Integer

            Dim i = 0
            For Each x In self

                If f(x) Then Return i
                i += 1
            Next

            Return -1
        End Function

    End Module

End Namespace

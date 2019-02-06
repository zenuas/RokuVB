Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Runtime.CompilerServices


Namespace Util

    Public Module Extensions

        <Extension>
        <DebuggerHidden>
        Public Function [If](Of T)(self As Boolean, f As Func(Of T)) As T

            If Not self Then Return Nothing
            Return f()
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [If](Of T)(self As Boolean?, f As Func(Of T)) As T

            If Not self.HasValue OrElse Not self.Value Then Return Nothing
            Return f()
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [If](Of T, R)(self As T, [then] As Func(Of T, R), [else] As Func(Of R)) As R

            If self IsNot Nothing Then Return [then](self)
            Return [else]()
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [If](Of T As Structure, R)(self As T?, [then] As Func(Of T, R), [else] As Func(Of R)) As R

            If self.HasValue Then Return [then](self.Value)
            Return [else]()
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [Then](Of T, R)(self As T, then_ As Func(Of T, R)) As R

            If self IsNot Nothing Then Return then_(self)
            Return Nothing
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [Then](Of T As Structure, R)(self As T?, then_ As Func(Of T, R)) As R

            If Not self.HasValue Then Return then_(self.Value)
            Return Nothing
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [Else](Of T)(self As T, else_ As Func(Of T)) As T

            If self IsNot Nothing Then Return self
            Return else_()
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [Else](Of T As Structure)(self As T?, else_ As Func(Of T)) As T

            If self.HasValue Then Return self.Value
            Return else_()
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [In](Of T As IEquatable(Of T))(x As T, ParamArray xs() As T) As Boolean

            Return Not xs.Where(Function(a) x.Equals(a)).IsNull
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function Car(Of T)(self As IEnumerable(Of T)) As T

            Dim e = self.GetEnumerator
            e.MoveNext()
            Return e.Current
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Cdr(Of T)(self As IEnumerable(Of T)) As IEnumerable(Of T)

            Dim e = self.GetEnumerator
            If Not e.MoveNext Then Return
            Do While e.MoveNext

                Yield e.Current
            Loop
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function First(Of T)(self As IEnumerable(Of T)) As T

            Dim e = self.GetEnumerator
            If Not e.MoveNext Then Throw New IndexOutOfRangeException
            Return e.Current
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Range(Of T)(self As IList(Of T), from As Integer) As IEnumerable(Of T)

            For i = [from] To self.Count - 1

                Yield self(i)
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Range(Of T)(self As IList(Of T), from As Integer, [to] As Integer) As IEnumerable(Of T)

            For i = [from] To [to]

                Yield self(i)
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function Split(Of T)(self As IEnumerable(Of T)) As Tuple(Of T, IEnumerable(Of T))

            Return Tuple.Create(self.Car, self.Cdr)
        End Function

        <Extension>
        <DebuggerHidden>
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
        <DebuggerHidden>
        Public Function Split(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Tuple(Of List(Of T), List(Of T))

            Return Split(self, Function(x, i) f(x))
        End Function

        <Extension>
        <DebuggerHidden>
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
        <DebuggerHidden>
        Public Iterator Function Pattern(Of T)(self As IEnumerable(Of T), n As Integer) As IEnumerable(Of T)

            For i = 1 To n

                For Each x In self

                    Yield x
                Next
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToList(Of T)(self As IEnumerable(Of T)) As List(Of T)

            Return New List(Of T)(self)
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToArray(Of T)(self As IEnumerable(Of T)) As T()

            Return self.ToList.ToArray
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToHash_KeyDerivation(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, R)) As Dictionary(Of R, T)

            Dim hash As New Dictionary(Of R, T)
            self.Each(Sub(x) hash(f(x)) = x)
            Return hash
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToHash_KeyDerivation(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, Integer, R)) As Dictionary(Of R, T)

            Dim hash As New Dictionary(Of R, T)
            self.Each(Sub(x, i) hash(f(x, i)) = x)
            Return hash
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToHash_ValueDerivation(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, R)) As Dictionary(Of T, R)

            Dim hash As New Dictionary(Of T, R)
            self.Each(Sub(x) hash(x) = f(x))
            Return hash
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToHash_ValueDerivation(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, Integer, R)) As Dictionary(Of T, R)

            Dim hash As New Dictionary(Of T, R)
            self.Each(Sub(x, i) hash(x) = f(x, i))
            Return hash
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function ToHashSet(Of T)(self As IEnumerable(Of T)) As HashSet(Of T)

            Dim hash As New HashSet(Of T)
            self.Each(Sub(x, i) hash.Add(x))
            Return hash
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Reverse(Of T)(self As IList(Of T)) As IEnumerable(Of T)

            For i = self.Count - 1 To 0 Step -1

                Yield self(i)
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function IsNull(Of T)(self As IEnumerable(Of T)) As Boolean

            Return Not self.GetEnumerator.MoveNext
        End Function

        <Extension>
        <DebuggerHidden>
        Public Sub [Each](Of T)(self As IEnumerable(Of T), f As Action(Of T, Integer))

            Dim i = 0
            For Each x In self

                f(x, i)
                i += 1
            Next
        End Sub

        <Extension>
        <DebuggerHidden>
        Public Sub [Each](Of T)(self As IEnumerable(Of T), f As Action(Of T))

            For Each x In self

                f(x)
            Next
        End Sub

        <Extension>
        <DebuggerHidden>
        Public Sub Done(Of T)(self As IList(Of T), f As Func(Of T, Integer, T))

            For i = 0 To self.Count - 1

                Dim x = f(self(i), i)
                self(i) = x
            Next
        End Sub

        <Extension>
        <DebuggerHidden>
        Public Sub Done(Of T)(self As IList(Of T), f As Func(Of T, T))

            For i = 0 To self.Count - 1

                Dim x = f(self(i))
                self(i) = x
            Next
        End Sub

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Map(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, Integer, R)) As IEnumerable(Of R)

            Dim i = 0
            For Each x In self

                Yield f(x, i)
                i += 1
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Map(Of T, R)(self As IEnumerable(Of T), f As Func(Of T, R)) As IEnumerable(Of R)

            For Each x In self

                Yield f(x)
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Apply(Of T)(self As IList(Of T), f As Func(Of T, Integer, T)) As IEnumerable(Of T)

            For i = 0 To self.Count - 1

                Dim x = f(self(i), i)
                self(i) = x
                Yield x
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Apply(Of T)(self As IList(Of T), f As Func(Of T, T)) As IEnumerable(Of T)

            For i = 0 To self.Count - 1

                Dim x = f(self(i))
                self(i) = x
                Yield x
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Merge(Of T)(self As IList(Of T), xs As IEnumerable(Of T)) As IEnumerable(Of T)

            Dim hash = xs.ToHash_ValueDerivation(Function(x) True)
            For Each v In self

                If hash.ContainsKey(v) Then Yield v
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Merge(Of T)(self As IList(Of T), xs As IEnumerable(Of T), match As Func(Of T, T, Boolean)) As IEnumerable(Of T)

            For Each v In self

                For Each x In xs

                    If match(v, x) Then

                        Yield v
                        Exit For
                    End If
                Next
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Where(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As IEnumerable(Of T)

            Dim i = 0
            For Each x In self

                If f(x, i) Then Yield x
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Where(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As IEnumerable(Of T)

            For Each x In self

                If f(x) Then Yield x
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function By(Of R As Class, T)(self As IEnumerable(Of T)) As IEnumerable(Of R)

            For Each x In self

                If TypeOf x Is R Then Yield TryCast(x, R)
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindFirst(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As T

            Return self.Where(f).First
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindFirst(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As T

            Return self.Where(f).First
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindFirstOrNull(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As T

            Return self.Where(f).Car
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindFirstOrNull(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As T

            Return self.Where(f).Car
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindLast(Of T)(self As IList(Of T), f As Func(Of T, Integer, Boolean)) As T

            Dim count = self.Count - 1
            Return self.Reverse.Where(Function(x, i) f(x, count - i)).First
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindLast(Of T)(self As IList(Of T), f As Func(Of T, Boolean)) As T

            Return self.Reverse.Where(f).First
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindLastOrNull(Of T)(self As IList(Of T), f As Func(Of T, Integer, Boolean)) As T

            Dim count = self.Count - 1
            Return self.Reverse.Where(Function(x, i) f(x, count - i)).Car
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FindLastOrNull(Of T)(self As IList(Of T), f As Func(Of T, Boolean)) As T

            Return self.Reverse.Where(f).Car
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [And](Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Boolean

            Dim i = 0
            For Each x In self

                If Not f(x, i) Then Return False
                i += 1
            Next

            Return True
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [And](Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Boolean

            For Each x In self

                If Not f(x) Then Return False
            Next

            Return True
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [Or](Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Boolean

            Dim i = 0
            For Each x In self

                If f(x, i) Then Return True
                i += 1
            Next

            Return False
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function [Or](Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Boolean

            For Each x In self

                If f(x) Then Return True
            Next

            Return False
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function IndexOf(Of T)(self As IEnumerable(Of T), f As Func(Of T, Integer, Boolean)) As Integer

            Dim i = 0
            For Each x In self

                If f(x, i) Then Return i
                i += 1
            Next

            Return -1
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function IndexOf(Of T)(self As IEnumerable(Of T), f As Func(Of T, Boolean)) As Integer

            Dim i = 0
            For Each x In self

                If f(x) Then Return i
                i += 1
            Next

            Return -1
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function SortToList(Of T)(self As IEnumerable(Of T)) As List(Of T)

            Dim xs = self.ToList
            xs.Sort()
            Return xs
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function SortToList(Of T)(self As IEnumerable(Of T), f As Func(Of T, T, Integer)) As List(Of T)

            Dim xs = self.ToList
            xs.Sort(New Comparison(Of T)(Function(a, b) f(a, b)))
            Return xs
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function StableSortToList(Of T)(self As IEnumerable(Of T), f As Func(Of T, T, Integer)) As List(Of T)

            Return self.Map(Function(x, i) New KeyValuePair(Of Integer, T)(i, x)).SortToList(
                Function(a, b)

                    Dim compare = f(a.Value, b.Value)
                    Return If(compare = 0, a.Key - b.Key, compare)

                End Function).Map(Function(x) x.Value).ToList
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Unique(Of T)(self As IEnumerable(Of T)) As IEnumerable(Of T)

            Dim first = True
            Dim prev As T = Nothing
            For Each x In self

                If first OrElse Not Object.Equals(prev, x) Then

                    Yield x
                    prev = x
                    first = False
                End If
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function UniqueList(Of T)(self As IEnumerable(Of T), f As Func(Of T, T, Boolean)) As IEnumerable(Of T)

            Dim first = True
            Dim ys As New List(Of T)
            For Each x In self

                If first OrElse Not ys.Or(Function(y) f(x, y)) Then

                    Yield x
                    ys.Add(x)
                    first = False
                End If
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function UniqueHash(Of T)(self As IEnumerable(Of T)) As IEnumerable(Of T)

            Dim hash As New Dictionary(Of T, Boolean)
            For Each x In self

                If Not hash.ContainsKey(x) Then

                    hash(x) = True
                    Yield x
                End If
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Flatten(Of T)(self As IEnumerable(Of IEnumerable(Of T))) As IEnumerable(Of T)

            For Each xs In self

                For Each x In xs

                    Yield x
                Next
            Next
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FoldLeft(Of T, R)(self As IEnumerable(Of T), f As Func(Of R, T, R), acc As R) As R

            Dim xs = self.GetEnumerator
            Do While xs.MoveNext

                acc = f(acc, xs.Current)
            Loop
            Return acc
        End Function

        <Extension>
        <DebuggerHidden>
        Public Function FoldRight(Of T, R)(xs As IEnumerable(Of T), f As Func(Of T, R, R), acc As R) As R

            Dim xf As Func(Of IEnumerator(Of T), R) =
                Function(a)

                    If a.MoveNext Then

                        Return f(a.Current, xf(a))
                    Else
                        Return acc
                    End If
                End Function
            Return xf(xs.GetEnumerator)
        End Function

        <Extension>
        <DebuggerHidden>
        Public Iterator Function Take(Of T)(self As IEnumerable(Of T), count As Integer) As IEnumerable(Of T)

            Dim xs = self.GetEnumerator
            Dim i = 0
            Do While i < count AndAlso xs.MoveNext

                Yield xs.Current
                i += 1
            Loop
        End Function
    End Module

End Namespace

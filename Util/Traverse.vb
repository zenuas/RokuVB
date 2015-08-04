Imports System
Imports System.Collections.Generic
Imports System.Reflection


Public Class Traverse

    Public Shared Iterator Function Fields(
            x As Object,
            Optional flag As BindingFlags = BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.GetField
        ) As IEnumerable(Of Tuple(Of Object, FieldInfo))

        If x Is Nothing Then Return

        If TypeOf x Is Array Then

            Dim xs = CType(x, Array)
            For i = 0 To xs.Length - 1

                Yield New Tuple(Of Object, FieldInfo)(xs.GetValue(i), Nothing)
            Next
        Else
            For Each m In x.GetType.GetFields(flag)

                Yield Tuple.Create(m.GetValue(x), m)
            Next
        End If

    End Function


End Class

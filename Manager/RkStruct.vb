Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkStruct
        Implements IType

        Public Overridable Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType)
        Public Overridable ReadOnly Property Generics As List(Of RkGenericEntry)

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry

            Dim x As New RkGenericEntry With {.Name = name}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Overridable Sub FixedGeneric(ParamArray values() As IType)

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            For i = 0 To values.Length - 1

                Me.Generics(i).Reference = values(i)
            Next
        End Sub

        Public Overridable Function HasGeneric() As Boolean

            Return Util.Functions.Or(Me.Generics, Function(x) x.Reference Is Nothing)
        End Function

    End Class

End Namespace

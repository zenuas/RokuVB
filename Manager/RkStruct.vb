Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkStruct
        Implements IType

        Public Overridable Property Super As IType
        Public Overridable Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x As New RkGenericEntry With {.Name = name}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(Util.Functions.List(Util.Functions.Map(values, Function(v, i) New NamedValue(Of IType) With {.Name = Me.Generics(i).Name, .Value = v})).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue(Of IType)) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            Dim x As New RkStruct With {.Name = Me.Name}
            If Me.Super IsNot Nothing Then x.Super = Me.Super.FixedGeneric(values)
            For Each v In Me.Local

                x.Local.Add(v.Key, v.Value.FixedGeneric(values))
            Next
            Return x
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0
        End Function

    End Class

End Namespace

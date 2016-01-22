Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkStruct
        Implements IType, IAddLet

        Public Overridable Property Super As IType
        Public Overridable Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable Property StructNode As StructNode = Nothing
        Public Overridable Property Initializer As RkNativeFunction = Nothing

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            If Me.Local.ContainsKey(name) Then Return Me.Local(name)
            If Me.Super IsNot Nothing Then Return Me.Super.GetValue(name)
            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.Find(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(values.Map(Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v}).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            Dim x = CType(Me.CloneGeneric, RkStruct)
            If Me.Super IsNot Nothing Then x.Super = Me.Super.FixedGeneric(values)
            For Each v In Me.Local

                x.Local.Add(v.Key, v.Value.FixedGeneric(values))
            Next
            Return x
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Return New RkStruct With {.Name = Me.Name}
        End Function

        Public Overridable Sub AddLet(x As LetNode) Implements IAddLet.AddLet
        End Sub

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} {Me.Name}"
        End Function

    End Class

End Namespace

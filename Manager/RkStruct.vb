Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkStruct
        Implements IType, IAddLet

        Public Overridable Property Super As IType
        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
        Public Overridable Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable ReadOnly Property Apply As New List(Of IType)
        Public Overridable Property StructNode As StructNode = Nothing
        Public Overridable Property Initializer As RkNativeFunction = Nothing
        Public Overridable Property ClosureEnvironment As Boolean = False

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            If Me.Local.ContainsKey(name) Then Return Me.Local(name)
            If Me.Super IsNot Nothing Then Return Me.Super.GetValue(name)
            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.FindFirstOrNull(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name, .Namespace = Me.Namespace, .ApplyIndex = Me.Generics.Count}
            Me.Generics.Add(x)
            Me.Apply.Add(Nothing)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(values.Map(Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v}).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            Dim apply_map = values.ToHash_KeyDerivation(Function(x) Me.Generics.FindFirst(Function(g) g.Name.Equals(x.Name)).ApplyIndex)
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.Namespace.Structs(Me.Name).Where(Function(g) g.Apply.Count = apply.Length)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim clone = CType(Me.CloneGeneric, RkStruct)
            values = values.Map(Function(v) New NamedValue With {.Name = v.Name, .Value = If(TypeOf v.Value Is RkGenericEntry, clone.DefineGeneric(v.Name), v.Value)}).ToArray
            If Me.Super IsNot Nothing Then clone.Super = Me.Super.FixedGeneric(values)
            For Each v In Me.Local

                clone.Local.Add(v.Key, v.Value?.FixedGeneric(values))
            Next
            clone.Apply.Clear()
            clone.Apply.AddRange(apply)
            clone.StructNode = Me.StructNode
            Return clone
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0 OrElse Me.Apply.Or(Function(x) x Is Nothing OrElse TypeOf x Is RkGenericEntry OrElse x.HasGeneric)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Dim x = New RkStruct With {.Name = Me.Name, .Namespace = Me.Namespace}
            x.Namespace.AddStruct(x)
            Return x
        End Function

        Public Overridable Sub AddLet(name As String, t As IType) Implements IAddLet.AddLet

            Me.Local.Add(name, t)
        End Sub

        Public Overridable Function CreateManglingName() As String

            Return Me.ToString
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}{If(Me.Apply.IsNull, "", $" {"(" + String.Join(", ", Me.Apply.Map(Function(x) x.ToString)) + ")"}")}"
        End Function

    End Class

End Namespace

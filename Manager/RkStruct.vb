Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkStruct
        Inherits RkScope
        Implements IStruct, IAddLet

        Public Overridable Property Super As IType
        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overrides Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable Property GenericBase As RkStruct = Nothing
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply
        Public Overridable Property StructNode As StructNode = Nothing
        Public Overridable Property Initializer As RkNativeFunction = Nothing
        Public Overridable Property ClosureEnvironment As Boolean = False


        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            If Me.Local.ContainsKey(name) Then Return Me.Local(name)
            If Me.Super IsNot Nothing Then Return Me.Super.GetValue(name)
            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If t Is Nothing Then Return False
            If TypeOf t Is RkByName Then Return Me.Is(CType(t, RkByName).Type)
            If TypeOf t Is RkUnionType Then Return t.Is(Me)

            If Me Is t Then Return True
            If Me.Scope Is t.Scope AndAlso Me.Name.Equals(t.Name) Then Return True

            Return False
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.FindFirstOrNull(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name, .Scope = Me.Scope, .ApplyIndex = Me.Generics.Count}
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

            Dim apply_map As New Dictionary(Of Integer, NamedValue)
            Me.Generics.Do(Sub(x) apply_map(x.ApplyIndex) = values.FindFirst(Function(v) v.Name.Equals(x.Name)))
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.GetBaseTypes.Where(Function(g) g.Apply.Count = apply.Length)

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
            If Me.Initializer IsNot Nothing Then clone.Initializer = CType(Me.Initializer.FixedGeneric(values), RkNativeFunction)
            Return clone
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0 OrElse Me.Apply.Or(Function(x) x Is Nothing OrElse TypeOf x Is RkGenericEntry OrElse x.HasGeneric)
        End Function

        Public Overridable Function HasGenericFixed() As Boolean

            Return Me.Generics.Count > 0 AndAlso Me.Apply.And(Function(x) x IsNot Nothing AndAlso TypeOf x IsNot RkGenericEntry AndAlso Not x.HasGeneric)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Dim x = New RkStruct With {.Name = Me.Name, .Scope = Me.Scope, .GenericBase = Me, .Parent = Me.Parent}
            x.Scope.AddStruct(x)
            Return x
        End Function

        Public Overridable Function GetBaseTypes() As List(Of IStruct)

            If Me.Scope.Structs.ContainsKey(Me.Name) AndAlso Me.Scope.Structs(Me.Name).Exists(Function(s) s Is Me) Then Return Me.Scope.Structs(Me.Name)
            Return Me.Scope.Structs.FindFirst(Function(x) x.Value.Exists(Function(s) s Is Me)).Value
        End Function

        Public Overridable Sub AddLet(name As String, t As IType) Implements IAddLet.AddLet

            Me.Local.Add(name, t)
        End Sub

        Public Overridable Function CreateManglingName() As String

            Dim s = Me.Name
            Dim p = Me.Parent
            Do While TypeOf p IsNot RkNamespace

                s = $"{p.Name}#{s}"
                p = p.Parent
            Loop
            Return s
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return Me.Apply.Or(Function(x) x IsNot Nothing AndAlso x.HasIndefinite)
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}{If(Me.Apply.IsNull, "", $"{"(" + String.Join(", ", Me.Apply.Map(Function(x) If(x Is Nothing, "_", x.ToString))) + ")"}")}"
        End Function

    End Class

End Namespace

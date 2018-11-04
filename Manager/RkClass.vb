Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkClass
        Inherits RkScope
        Implements IStruct

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overrides Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply
        Public Overridable Property ClassNode As ClassNode = Nothing

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Return Me.Is({t})
        End Function

        Public Overridable Function [Is](args() As IType) As Boolean

            Dim search_args =
                Function(x As IType)

                    If TypeOf x Is RkGenericEntry Then

                        Return Me.Apply.By(Of RkGenericEntry).FindFirstOrNull(Function(a) a.Name.Equals(x.Name)).Then(Function(a) args(a.ApplyIndex))
                    Else

                        Return x
                    End If
                End Function

            For Each kv In Me.Functions

                For Each f In kv.Value

                    Dim fx = SystemLibrary.TryLoadFunction(Me.Scope, kv.Key, f.Arguments.Map(Function(x) search_args(x.Value)).ToArray)
                    If fx Is Nothing Then Return False
                Next
            Next

            Return True
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

            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(values.Map(Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v}).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Dim apply_map As New Dictionary(Of Integer, NamedValue)
            Me.Generics.Each(Sub(x) apply_map(x.ApplyIndex) = values.FindFirst(Function(v) v.Name.Equals(x.Name)))
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.GetBaseTypes.Where(Function(g) g.Apply.Count = apply.Length)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim clone = CType(Me.CloneGeneric, RkClass)
            clone.Apply.Clear()
            clone.Apply.AddRange(apply)
            clone.ClassNode = Me.ClassNode
            Me.Functions.Each(Sub(x) clone.Functions.Add(x.Key, Me.Functions(x.Key).ToList))
            Return clone
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            Return {value}
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return True
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Dim x = New RkClass With {.Name = Me.Name, .Scope = Me.Scope, .Parent = Me.Parent}
            x.Scope.AddStruct(x)
            Return x
        End Function

        Public Overridable Function GetBaseTypes() As List(Of IStruct)

            If Me.Scope.Structs.ContainsKey(Me.Name) AndAlso Me.Scope.Structs(Me.Name).Exists(Function(s) s Is Me) Then Return Me.Scope.Structs(Me.Name)
            Return Me.Scope.Structs.FindFirst(Function(x) x.Value.Exists(Function(s) s Is Me)).Value
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return False
        End Function

        Public Overrides Function ToString() As String

            Return Me.Name
        End Function

    End Class

End Namespace

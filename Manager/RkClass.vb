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
        Public Overridable Property GenericBase As RkClass = Nothing
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply
        Public Overridable Property ClassNode As ClassNode = Nothing

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Return Me.Is({t})
        End Function

        Public Overridable Function [Is](args() As IType) As Boolean

            Dim gen = Me.GenericBase.Else(Function() Me)
            Dim named_hash = args.ToHash_KeyDerivation(Function(x, i) gen.Generics(i).Name)

            Dim search_args =
                Function(x As IType)

                    If TypeOf x Is RkGenericEntry Then

                        Dim g = CType(x, RkGenericEntry)
                        Return named_hash.ContainsKey(g.Name).Then(Function() named_hash(g.Name))
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

        Public Overridable Function Feedback(args() As IType) As Boolean

            If Not Me.Is(args) Then Return False

            Dim gen = Me.GenericBase.Else(Function() Me)
            Dim named_hash = args.ToHash_KeyDerivation(Function(x, i) gen.Generics(i).Name)
            Dim index_hash As New Dictionary(Of String, Integer)
            gen.Generics.Each(Sub(x) index_hash(x.Name) = x.ApplyIndex)
            Dim isset = False

            Dim search_args As Func(Of IType, IType) =
                Function(x)

                    If TypeOf x Is RkGenericEntry Then

                        Dim g = CType(x, RkGenericEntry)
                        Return named_hash.ContainsKey(g.Name).If(Function() search_args(named_hash(g.Name)))
                    Else

                        Return x
                    End If
                End Function

            For Each kv In Me.Functions

                For Each f In kv.Value

                    Dim fx = SystemLibrary.TryLoadFunction(Me.Scope, kv.Key, f.Arguments.Map(Function(x) search_args(x.Value)).ToArray)
                    If TypeOf fx Is RkUnionType AndAlso CType(fx, RkUnionType).HasIndefinite Then Continue For
                    If fx.GenericBase IsNot Nothing Then fx = fx.GenericBase

                    Dim remap = fx.Generics.Map(Function(x) x.Name).ToHash_ValueDerivation(Function(x) CType(Nothing, IType))
                    Dim compare_type As Action(Of IType, IType) =
                        Sub(left, right)

                            If TypeOf left Is RkGenericEntry Then

                                Dim x = search_args(right)
                                If x IsNot Nothing Then remap(left.Name) = x

                            ElseIf TypeOf left Is RkStruct Then

                                CType(left, RkStruct).Apply.Each(
                                    Sub(x, i)

                                        Dim rightx = search_args(right)
                                        If TypeOf rightx Is RkStruct Then compare_type(x, CType(rightx, RkStruct).Apply(i))
                                    End Sub)
                            End If
                        End Sub

                    Dim fixed_type =
                        Function(x As IType)

                            If x.HasGeneric Then

                                If TypeOf x Is RkStruct Then

                                    Dim struct = CType(x, RkStruct)
                                    Return struct.FixedGeneric(struct.Generics.Map(
                                        Function(g, i)

                                            Dim apply = struct.Apply(i)
                                            Return New NamedValue With {.Name = g.Name, .Value = If(TypeOf apply Is RkGenericEntry, remap(apply.Name), apply)}
                                        End Function).ToArray)
                                End If
                            End If

                            Return x
                        End Function

                    Dim set_type =
                        Sub(i As Integer, x As IType)

                            Dim fixed = fixed_type(x)
                            If args(i) Is Nothing OrElse Not fixed.HasGeneric Then

                                args(i) = fixed
                                isset = True
                            End If
                        End Sub

                    Dim apply_type =
                        Sub(left As IType, right As IType)

                            If TypeOf right Is RkGenericEntry Then

                                If index_hash.ContainsKey(right.Name) Then set_type(index_hash(right.Name), left)
                            End If
                        End Sub

                    fx.Arguments.Each(Sub(x, i) compare_type(x.Value, f.Arguments(i).Value))
                    compare_type(fx.Return, f.Return)

                    fx.Arguments.Each(Sub(x, i) apply_type(x.Value, f.Arguments(i).Value))
                    apply_type(fx.Return, f.Return)
                Next
            Next

            Return isset
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

            Dim x = New RkClass With {.Name = Me.Name, .Scope = Me.Scope, .GenericBase = Me, .Parent = Me.Parent}
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

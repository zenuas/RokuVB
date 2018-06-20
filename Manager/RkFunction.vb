﻿Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Reflection
Imports Roku.Node
Imports Roku.Manager.SystemLibrary
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkFunction
        Inherits RkScope
        Implements IFunction

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overrides Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue) Implements IFunction.Arguments
        Public Overridable Property [Return] As IType Implements IFunction.Return
        Public Overridable ReadOnly Property Body As New List(Of InCode0) Implements IFunction.Body
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry) Implements IFunction.Generics
        Public Overridable Property GenericBase As RkFunction = Nothing Implements IFunction.GenericBase
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply
        Public Overridable Property FunctionNode As FunctionNode = Nothing Implements IFunction.FunctionNode


        Public Overridable ReadOnly Property IsAnonymous As Boolean Implements IFunction.IsAnonymous
            Get
                Return String.IsNullOrEmpty(Me.Name)
            End Get
        End Property

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If Me Is t Then Return True
            If TypeOf t IsNot RkFunction Then Return False

            Dim f = CType(t, RkFunction)
            Dim is_return_either_instance =
                Function(a As IType, b As IType)

                    If a Is Nothing Then

                        Return b.Is(a)
                    Else
                        Return a.Is(b)
                    End If
                End Function

            If ((Me.Return Is Nothing AndAlso f.Return Is Nothing) OrElse is_return_either_instance(Me.Return, f.Return)) AndAlso
                (Me.Arguments.Count = f.Arguments.Count AndAlso Me.Arguments.And(Function(x, i) x.Value.Is(f.Arguments(i).Value))) Then Return True

            Return False
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.Find(Function(a) a.Name.Equals(name))
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
            Me.Generics.Each(Sub(x) apply_map(x.ApplyIndex) = values.FindFirst(Function(v) v.Name.Equals(x.Name)))
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.GetBaseFunctions().Where(Function(g) g.Apply.Count = apply.Length)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim apply_fix =
                Function(c As IType)

                    If Not c.HasGeneric Then

                        Return c

                    ElseIf TypeOf c Is IApply AndAlso CType(c, IApply).Apply.And(Function(x) x IsNot Nothing) Then

                        Return c.FixedGeneric(CType(c, IApply).Apply.Map(Function(x) If(TypeOf x Is RkGenericEntry, values(CType(x, RkGenericEntry).ApplyIndex).Value, x)).ToArray)
                    Else
                        Return c.FixedGeneric(values)
                    End If
                End Function

            Dim clone = CType(Me.CloneGeneric, RkFunction)
            values = values.Map(Function(v) New NamedValue With {.Name = v.Name, .Value = If(v.Value Is Nothing OrElse TypeOf v.Value Is RkGenericEntry, clone.DefineGeneric(v.Name), v.Value)}).ToArray
            If Me.Return IsNot Nothing Then clone.Return = apply_fix(Me.Return)
            Me.Arguments.Each(Sub(v, i) clone.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = apply_fix(v.Value)}))
            clone.Body.AddRange(Me.Body)
            clone.Apply.Clear()
            clone.Apply.AddRange(apply)
            clone.FunctionNode = Me.FunctionNode
            Me.Functions.Each(Sub(x) clone.Functions.Add(x.Key, Me.Functions(x.Key).ToList.Map(Function(f) CType(apply_fix(f), IFunction)).ToList))
            Return clone
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            If TypeOf value IsNot IFunction Then Throw New ArgumentException("generics parameter miss match")
            Return Me.ArgumentsToApply(CType(value, IFunction).Arguments.Map(Function(x) x.Value).ToArray)
        End Function

        Public Overridable Function ArgumentsToApply(ParamArray args() As IType) As IType() Implements IFunction.ArgumentsToApply

            Dim generic_match As Action(Of IType, IType, Action(Of RkGenericEntry, IType)) =
                Sub(arg, p, gen_to_type)

                    If TypeOf arg Is RkGenericEntry Then

                        gen_to_type(CType(arg, RkGenericEntry), p)

                    ElseIf arg.HasGeneric AndAlso TypeOf arg Is RkFunction Then

                        If TypeOf p Is RkFunction Then

                            Dim func = CType(p, RkFunction)
                            Dim argf = CType(arg, RkFunction)
                            argf.Generics.Each(Sub(x) If func.Apply.Count > x.ApplyIndex Then gen_to_type(x, func.Apply(x.ApplyIndex)))
                            If argf.Return IsNot Nothing Then generic_match(argf.Return, func.Return, gen_to_type)

                        ElseIf TypeOf p Is RkUnionType Then

                            Dim gens As New Dictionary(Of RkGenericEntry, List(Of IType))
                            Dim gen_add =
                                Sub(g As RkGenericEntry, t As IType)

                                    If t Is Nothing Then Return
                                    If Not gens.ContainsKey(g) Then gens.Add(g, New List(Of IType))
                                    If gens(g).And(Function(x) Not x.Is(t)) Then gens(g).Add(t)
                                End Sub
                            CType(p, RkUnionType).Types.Each(Sub(x) generic_match(arg, x, gen_add))
                            gens.Each(Sub(kv) gen_to_type(kv.Key, If(kv.Value.Count = 1, kv.Value(0), New RkUnionType(kv.Value))))
                        End If

                    ElseIf arg.HasGeneric AndAlso arg.Scope Is p.Scope AndAlso arg.Name.Equals(p.Name) Then

                        Dim struct = CType(arg, RkStruct)
                        struct.Generics.Each(
                            Sub(x, i)

                                Dim apply = CType(p, RkStruct).Apply(i)
                                Dim v As IType
                                If apply Is Nothing OrElse TypeOf apply Is RkGenericEntry Then

                                    v = Nothing

                                ElseIf TypeOf apply Is RkStruct OrElse
                                    TypeOf apply Is RkUnionType Then

                                    v = apply

                                ElseIf TypeOf apply Is RkByName Then

                                    v = CType(apply, RkByName).Type
                                Else

                                    Throw New Exception("unknown apply")
                                End If
                                generic_match(struct.Apply(x.ApplyIndex), v, gen_to_type)
                            End Sub)

                    ElseIf arg.HasGeneric AndAlso TypeOf arg Is RkStruct Then

                        Dim struct = CType(arg, RkStruct)
                        Dim t = FixedByName(p)
                        If TypeOf struct Is RkCILStruct AndAlso TypeOf t Is RkCILStruct Then

                            Dim cs = CType(struct, RkCILStruct)
                            Dim ts = CType(t, RkCILStruct)
                            If cs.TypeInfo = GetType(List(Of )) AndAlso ts.TypeInfo.IsArray Then

                                gen_to_type(cs.Generics(0), ts.FunctionNamespace.Root.LoadType(ts.TypeInfo.GetElementType.GetTypeInfo))
                                Return
                            End If
                        End If
                        struct.Generics.Each(
                            Sub(x, i)

                                Dim apply = CType(t, RkStruct).Apply(i)
                                gen_to_type(x, apply)
                            End Sub)
                    End If
                End Sub

            Dim xs(Me.Generics.Count - 1) As IType
            If xs.Length = 0 Then Return xs
            For i = 0 To Me.Arguments.Count - 1

                generic_match(Me.Arguments(i).Value, args(i),
                    Sub(atname, p)

                        If p Is Nothing Then Return

                        Dim x = xs(atname.ApplyIndex)
                        If x Is Nothing Then

                            xs(atname.ApplyIndex) = p

                        ElseIf TypeOf x Is RkUnionType Then

                            CType(x, RkUnionType).Merge(p)

                        ElseIf TypeOf p Is RkUnionType Then

                            CType(p, RkUnionType).Merge(x)
                            xs(atname.ApplyIndex) = p
                        Else

                            Debug.Assert(x.Is(p))
                            If x.HasGeneric Then xs(atname.ApplyIndex) = p
                        End If
                    End Sub)
            Next

            Return xs
        End Function

        Public Overridable Function ApplyFunction(ParamArray args() As IType) As IFunction Implements IFunction.ApplyFunction

            If Not Me.HasGeneric Then Return Me
            Return CType(Me.FixedGeneric(Me.ArgumentsToApply(args)), RkFunction)
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0 OrElse Me.Apply.Or(Function(x) x Is Nothing OrElse TypeOf x Is RkGenericEntry OrElse x.HasGeneric)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Dim x = New RkFunction With {.Name = Me.Name, .Scope = Me.Scope, .GenericBase = Me, .Parent = Me.Parent}
            x.Scope.AddFunction(x)
            Return x
        End Function

        Public Overridable Function GetBaseFunctions() As List(Of IFunction) Implements IFunction.GetBaseFunctions

            If Me.Scope.Functions.ContainsKey(Me.Name) AndAlso Me.Scope.Functions(Me.Name).Exists(Function(s) s Is Me) Then Return Me.Scope.Functions(Me.Name)
            Dim xs = Me.Scope.Functions.FindFirstOrNull(Function(x) x.Value.Exists(Function(s) s Is Me)).Value
            If xs IsNot Nothing Then Return xs
            Return {CType(Me, IFunction)}.ToList
        End Function

        Public Overridable Function CreateCall(ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCall

            'Debug.Assert(Me.Closure IsNot Nothing OrElse Me.Arguments.Count = args.Length, "unmatch arguments count")
            Dim x As InCall = If(Me.IsAnonymous, New InLambdaCall With {.Value = args(0)}, New InCall)
            x.Function = Me
            x.Arguments.AddRange(If(Me.IsAnonymous, args.Range(1).ToArray, args))
            Return New InCode0() {x}
        End Function

        Public Overridable Function CreateCallReturn(return_ As OpValue, ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCallReturn

            'Debug.Assert(Me.Closure IsNot Nothing OrElse Me.Arguments.Count = args.Length, "unmatch arguments count")
            Dim x As InCall = If(Me.IsAnonymous, New InLambdaCall With {.Value = args(0)}, New InCall)
            x.Function = Me
            x.Return = return_
            x.Arguments.AddRange(If(Me.IsAnonymous, args.Range(1).ToArray, args))
            Return New InCode0() {x}
        End Function

        Public Overridable Function CreateManglingName() As String Implements IFunction.CreateManglingName

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

            Return $"{Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.Value.ToString))})" + If(Me.Return IsNot Nothing, $" {Me.Return.ToString}", "")
        End Function
    End Class

End Namespace

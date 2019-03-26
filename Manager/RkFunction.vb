Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Reflection
Imports Roku.Node
Imports Roku.Manager.SystemLibrary
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkFunction
        Inherits RkScope
        Implements IFunction

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overrides Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue) Implements IFunction.Arguments
        Public Overridable Property [Return] As IType Implements IFunction.Return
        Public Overridable ReadOnly Property Where As New List(Of RkClass)
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

            x = New RkGenericEntry With {.Name = name, .Scope = Me.Scope, .ApplyIndex = Me.Generics.Count, .Reference = Me}
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

            Dim apply = Me.Apply.ToList
            values.Each(Sub(kv) apply(Me.Generics.FindFirst(Function(x) x.Name.Equals(kv.Name)).ApplyIndex) = kv.Value)
            For Each fix In Me.GetBaseFunctions().Where(Function(g) g.Apply.Count = apply.Count)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim clone = CType(Me.CloneGeneric, RkFunction)
            Me.Generics.Each(Sub(g) clone.Generics.Add(CopyGenericEntry(clone, g)))
            clone.Apply.AddRange(apply)
            clone.Return = CopyType(Me, clone, Me.Return)
            Me.Arguments.Each(Sub(v, i) clone.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = CopyType(Me, clone, v.Value)}))
            clone.Body.AddRange(Me.Body)
            clone.FunctionNode = Me.FunctionNode
            clone.Where.AddRange(Me.Where)
            Me.Functions.Each(Sub(x) clone.Functions.Add(x.Key, Me.Functions(x.Key).ToList))
            Return clone
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            If TypeOf value IsNot IFunction Then Throw New ArgumentException("generics parameter miss match")
            Dim f = CType(value, IFunction)
            Return Me.ArgumentsToApply(f.Scope, f.Arguments.Map(Function(x) x.Value).ToArray)
        End Function

        Public Overridable Function ArgumentsToApply(target As IScope, ParamArray args() As IType) As IType()

            Dim generic_match As Action(Of IType, IType, Action(Of RkGenericEntry, IType)) =
                Sub(arg, p, gen_to_type)

                    If TypeOf arg Is RkGenericEntry Then

                        gen_to_type(CType(arg, RkGenericEntry), p)

                    ElseIf TypeOf p Is RkUnionType Then

                        Dim gens As New Dictionary(Of RkGenericEntry, List(Of IType))
                        Dim gen_add =
                            Sub(g As RkGenericEntry, t As IType)

                                If t Is Nothing Then Return
                                If Not gens.ContainsKey(g) Then gens.Add(g, New List(Of IType))
                                If gens(g).And(Function(x) Not x.Is(t)) Then gens(g).Add(t)
                            End Sub
                        CType(p, RkUnionType).Types.Each(Sub(x) generic_match(arg, x, gen_add))
                        gens.Each(Sub(kv) gen_to_type(kv.Key, If(kv.Value.Count = 1, kv.Value(0), New RkUnionType(kv.Value) With {.Dynamic = False})))

                    ElseIf TypeOf arg Is RkFunction Then

                        If TypeOf p Is RkFunction Then

                            Dim func = CType(p, RkFunction)
                            Dim argf = CType(arg, RkFunction)
                            argf.Generics.Each(Sub(x) If func.Apply.Count > x.ApplyIndex Then gen_to_type(x, func.Apply(x.ApplyIndex)))
                            argf.Arguments.Take(func.Arguments.Count).Each(Sub(x, i) generic_match(x.Value, func.Arguments(i).Value, gen_to_type))
                            If argf.Return IsNot Nothing Then generic_match(argf.Return, func.Return, gen_to_type)
                        End If

                    ElseIf p IsNot Nothing AndAlso arg.HasGeneric AndAlso arg.Scope Is p.Scope AndAlso arg.Name.Equals(p.Name) Then

                        If TypeOf p Is RkUnionType Then p = CType(p, RkUnionType).GetDecideType
                        Dim struct = CType(arg, RkStruct)
                        struct.Generics.Each(
                            Sub(x, i)

                                Dim apply = FixedByName(CType(p, RkStruct).Apply(i))
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

                        ElseIf TypeOf struct Is RkCILNativeArray AndAlso TypeOf t Is RkCILStruct Then

                            Dim cs = CType(struct, RkCILNativeArray)
                            Dim ts = CType(t, RkCILStruct)
                            If ts.TypeInfo.IsArray Then

                                gen_to_type(cs.Generics(0), ts.FunctionNamespace.Root.LoadType(ts.TypeInfo.GetElementType.GetTypeInfo))
                                Return
                            End If
                        End If

                        If TypeOf t Is RkStruct Then

                            struct.Generics.Each(Sub(x, i) gen_to_type(x, CType(t, RkStruct).Apply(i)))
                        End If
                    End If
                End Sub

            Dim xs = Me.Generics.Map(Function(x) Me.Apply(x.ApplyIndex)).ToArray
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

            Return Me.ApplyToWhere(target, xs)
        End Function

        Public Overridable Function ApplyToWhere(target As IScope, ParamArray apply() As IType) As IType()

            Do While True

                Dim type_fix = False

                Me.Where.Each(
                    Sub(x)

                        Dim xargs = x.Apply.Map(Function(a) If(TypeOf a Is RkGenericEntry, apply(CType(a, RkGenericEntry).ApplyIndex), a)).ToArray
                        If x.Feedback(target, xargs) Then

                            x.Apply.Each(
                                Sub(a, i)

                                    If TypeOf a Is RkGenericEntry Then

                                        Dim xs_i = CType(a, RkGenericEntry).ApplyIndex
                                        If apply(xs_i) IsNot xargs(i) Then

                                            apply(xs_i) = xargs(i)
                                            type_fix = True
                                        End If
                                    End If
                                End Sub)
                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop

            Return apply
        End Function

        Public Overridable Function WhereFunction(target As IScope, ParamArray args() As IType) As Boolean Implements IFunction.WhereFunction

            If Me.Where.Count = 0 Then Return True

            args = args.Map(Function(x) If(x Is Nothing, Nothing, TypeHelper.MemberwiseClone(x))).ToArray
            Dim apply = Me.ArgumentsToApply(target, args)
            Return Me.Where.And(Function(x) x.Is(target, x.Apply.Map(Function(a) If(TypeOf a Is RkGenericEntry, apply(CType(a, RkGenericEntry).ApplyIndex), a)).ToArray))
        End Function

        Public Overridable Function ApplyFunction(target As IScope, ParamArray args() As IType) As IFunction Implements IFunction.ApplyFunction

            If Not Me.HasGeneric Then Return Me
            Return CType(Me.FixedGeneric(Me.ArgumentsToApply(target, args)), RkFunction)
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Apply.Or(Function(x) x Is Nothing OrElse x.HasGeneric)
        End Function

        Public Overridable Function HasArgumentsGeneric() As Boolean

            Return Me.Arguments.Or(Function(x) x.Value Is Nothing OrElse x.Value.HasGeneric)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Dim x = New RkFunction With {.Name = Me.Name, .Scope = Me.Scope, .GenericBase = Me, .Parent = Me.Parent}
            Me.CopyGeneric(x)
            x.Scope.AddFunction(x)
            Return x
        End Function

        Public Overridable Sub CopyGeneric(clone As RkFunction, Optional perfect_copy As Boolean = False)

            clone.Name = Me.Name
            clone.Scope = Me.Scope
            clone.GenericBase = Me
            clone.Parent = Me.Parent
            clone.Return = Me.Return
            clone.FunctionNode = Me.FunctionNode

            If Not perfect_copy Then Return

            clone.Generics.Clear()
            clone.Generics.AddRange(Me.Generics)
            clone.Apply.Clear()
            clone.Apply.AddRange(Me.Apply)
            clone.Arguments.Clear()
            Me.Arguments.Each(Sub(x) clone.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = x.Value}))
            clone.Where.Clear()
            clone.Where.AddRange(Me.Where)
            clone.Body.Clear()
            clone.Body.AddRange(Me.Body)
        End Sub

        Public Overridable Function GetBaseFunctions() As List(Of IFunction) Implements IFunction.GetBaseFunctions

            Dim xs As List(Of IFunction)
            If Me.Scope.Functions.ContainsKey(Me.Name) AndAlso Me.Scope.Functions(Me.Name).Exists(Function(s) s Is Me) Then

                xs = Me.Scope.Functions(Me.Name)
            Else

                xs = Me.Scope.Functions.FindFirstOrNull(Function(x) x.Value.Exists(Function(s) s Is Me)).Value
                If xs Is Nothing Then xs = {CType(Me, IFunction)}.ToList
            End If

            Return xs.Where(Function(x) Me.Arguments.Count = x.Arguments.Count AndAlso Me.Arguments.And(Function(arg, i) arg.Value.Is(x.Arguments(i).Value))).ToList
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

            Dim s = $"{Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.Value))})=>{Me.Return}"
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

        Public Overridable Function Clone(conv As Func(Of INode, INode)) As Node.ICloneable Implements Node.ICloneable.Clone

            Dim copy = CType(Me.MemberwiseClone, RkFunction)
            If copy.FunctionNode IsNot Nothing Then copy.FunctionNode = CType(conv(copy.FunctionNode), FunctionNode)
            Return copy
        End Function

        Public Overrides Function ToString() As String

            If String.IsNullOrEmpty(Me.Name) Then Return $"{{{String.Join(", ", Me.Arguments.Map(Function(x) x.Value)) + If(Me.Return IsNot Nothing, $" => {Me.Return}", "")}}}"
            Return $"{Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) TypeToString(x.Value)))})" + If(Me.Return IsNot Nothing, $" {TypeToString(Me.Return)}", "")
        End Function
    End Class

End Namespace

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Node
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkFunction
        Implements IFunction, IScope

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overridable Property Name As String Implements IEntry.Name, IScope.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue) Implements IFunction.Arguments
        Public Overridable Property [Return] As IType Implements IFunction.Return
        Public Overridable ReadOnly Property Body As New List(Of InCode0) Implements IFunction.Body
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry) Implements IFunction.Generics
        Public Overridable Property GenericBase As RkFunction = Nothing Implements IFunction.GenericBase
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply
        Public Overridable Property FunctionNode As FunctionNode = Nothing Implements IFunction.FunctionNode
        Public Overridable Property Closure As RkStruct = Nothing Implements IFunction.Closure
        Public Overridable Property Parent As IScope Implements IScope.Parent
        Public Overridable ReadOnly Property Structs As New Dictionary(Of String, List(Of RkStruct)) Implements IScope.Structs
        Public Overridable ReadOnly Property Functions As New Dictionary(Of String, List(Of IFunction)) Implements IScope.Functions


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
            If Me.Scope Is f.Scope AndAlso
                ((Me.Return Is Nothing AndAlso f.Return Is Nothing) OrElse (Me.Return IsNot Nothing AndAlso Me.Return.Is(f.Return))) AndAlso
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
            Me.Generics.Do(Sub(x) apply_map(x.ApplyIndex) = values.FindFirst(Function(v) v.Name.Equals(x.Name)))
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.GetBaseFunctions().Where(Function(g) g.Apply.Count = apply.Length)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim apply_fix =
                Function(c As IType)

                    If Not c.HasGeneric Then

                        Return c

                    ElseIf TypeOf c Is IApply Then

                        Return c.FixedGeneric(CType(c, IApply).Apply.Map(Function(x) values.FindFirst(Function(v) v.Name.Equals(x.Name)).Value).ToArray)
                    Else
                        Return c.FixedGeneric(values)
                    End If
                End Function

            Dim clone = CType(Me.CloneGeneric, RkFunction)
            values = values.Map(Function(v) New NamedValue With {.Name = v.Name, .Value = If(TypeOf v.Value Is RkGenericEntry, clone.DefineGeneric(v.Name), v.Value)}).ToArray
            If Me.Return IsNot Nothing Then clone.Return = apply_fix(Me.Return)
            Me.Arguments.Do(Sub(v, i) clone.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = apply_fix(v.Value)}))
            clone.Body.AddRange(Me.Body)
            clone.Apply.Clear()
            clone.Apply.AddRange(apply)
            clone.FunctionNode = Me.FunctionNode
            Me.Functions.Do(Sub(x) clone.Functions.Add(x.Key, Me.Functions(x.Key).Map(Function(f) CType(apply_fix(f), IFunction)).ToList))
            Return clone
        End Function

        Public Overridable Function ArgumentsToApply(ParamArray args() As IType) As IType() Implements IFunction.ArgumentsToApply

            Dim generic_match As Action(Of IType, IType, Action(Of RkGenericEntry, IType)) =
                Sub(arg, p, gen_to_type)

                    If TypeOf arg Is RkGenericEntry Then

                        gen_to_type(CType(arg, RkGenericEntry), p)

                    ElseIf arg.HasGeneric AndAlso arg.Scope Is p.Scope AndAlso arg.Name.Equals(p.Name) Then

                        Dim struct = CType(arg, RkStruct)
                        struct.Generics.Do(
                            Sub(x, i)

                                Dim apply = CType(p, RkStruct).Apply(i)
                                Dim v As IType
                                If apply Is Nothing OrElse TypeOf apply Is RkGenericEntry Then

                                    v = Nothing

                                ElseIf TypeOf apply Is RkStruct OrElse
                                    TypeOf apply Is RkSomeType Then

                                    v = apply

                                ElseIf TypeOf apply Is RkByName Then

                                    v = CType(apply, RkByName).Type
                                Else

                                    Throw New Exception("unknown apply")
                                End If
                                generic_match(x, v, gen_to_type)
                            End Sub)
                    End If
                End Sub

            Dim xs(Me.Generics.Count - 1) As IType
            For i = 0 To Me.Arguments.Count - 1

                generic_match(Me.Arguments(i).Value, args(i),
                    Sub(atname, p)

                        Dim x = xs(atname.ApplyIndex)
                        If x Is Nothing Then

                            xs(atname.ApplyIndex) = p

                        ElseIf TypeOf x Is RkSomeType Then

                            CType(x, RkSomeType).Merge(p)

                        ElseIf TypeOf p Is RkSomeType Then

                            CType(p, RkSomeType).Merge(x)
                            xs(atname.ApplyIndex) = p
                        Else

                            Debug.Assert(x.Is(p))
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
            Return Me.Scope.Functions.FindFirst(Function(x) x.Value.Exists(Function(s) s Is Me)).Value
        End Function

        Public Overridable Function CreateCall(self As OpValue, ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCall

            'Debug.Assert(Me.Closure IsNot Nothing OrElse Me.Arguments.Count = args.Length, "unmatch arguments count")
            Dim x As InCall = If(Me.IsAnonymous, New InLambdaCall With {.Value = self}, New InCall)
            x.Function = Me
            x.Arguments.AddRange(args)
            Return New InCode0() {x}
        End Function

        Public Overridable Function CreateCallReturn(self As OpValue, return_ As OpValue, ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCallReturn

            'Debug.Assert(Me.Closure IsNot Nothing OrElse Me.Arguments.Count = args.Length, "unmatch arguments count")
            Dim x As InCall = If(Me.IsAnonymous, New InLambdaCall With {.Value = self}, New InCall)
            x.Function = Me
            x.Return = return_
            x.Arguments.AddRange(args)
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

        Public Overridable Sub AddStruct(x As RkStruct) Implements IScope.AddStruct

            Me.AddStruct(x, x.Name)
        End Sub

        Public Overridable Sub AddStruct(x As RkStruct, name As String) Implements IScope.AddStruct

            If Not Me.Structs.ContainsKey(name) Then Me.Structs.Add(name, New List(Of RkStruct))
            Me.Structs(name).Add(x)
        End Sub

        Public Overridable Iterator Function FindCurrentStruct(name As String) As IEnumerable(Of RkStruct) Implements IScope.FindCurrentStruct

            If Me.Structs.ContainsKey(name) Then

                For Each s In Me.Structs(name)

                    Yield s
                Next
            End If
        End Function

        Public Overridable Sub AddFunction(x As IFunction) Implements IScope.AddFunction

            Me.AddFunction(x, x.Name)
        End Sub

        Public Overridable Sub AddFunction(x As IFunction, name As String) Implements IScope.AddFunction

            If Not Me.Functions.ContainsKey(name) Then Me.Functions.Add(name, New List(Of IFunction))
            Me.Functions(name).Add(x)
        End Sub

        Public Overridable Iterator Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction) Implements IScope.FindCurrentFunction

            If Me.Functions.ContainsKey(name) Then

                For Each f In Me.Functions(name)

                    Yield f
                Next
            End If
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.Value.ToString))})" + If(Me.Return IsNot Nothing, $" {Me.Return.ToString}", "")
        End Function
    End Class

End Namespace

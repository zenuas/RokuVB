Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Manager.SystemLibrary
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util
Imports Roku.Util.Extensions
Imports Roku.Util.TypeHelper
Imports System.Diagnostics


Namespace Compiler

    Public Class Translater

        Public Shared Sub ClosureTranslate(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Dim closures As New Dictionary(Of INamedFunction, RkStruct)
            Dim make_env =
                Function(owner As INamedFunction)

                    If closures.ContainsKey(owner) Then Return closures(owner)

                    Dim env As New RkStruct With {.Scope = root, .ClosureEnvironment = True, .Parent = owner.Function}
                    env.Name = $"##{owner.Name}:({String.Join(", ", owner.Function.Arguments.Map(Function(x) x.Value))})=>{owner.Function.Return}"
                    env.Initializer = CType(LoadFunction(root, "#Alloc", env), RkNativeFunction)
                    closures.Add(owner, env)
                    root.AddStruct(env)
                    owner.Function.Closure = env
                    Coverage.Case()
                    Return env
                End Function

            Dim make_closured As New Dictionary(Of IScopeNode, Boolean)
            Dim make_closure =
                Sub(scope As IScopeNode)

                    If make_closured.ContainsKey(scope) Then Return
                    make_closured(scope) = True

                    For Each var In scope.Lets.Where(Function(x) TypeOf x.Value Is VariableNode AndAlso CType(x.Value, VariableNode).ClosureEnvironment).Map(Function(x) CType(x.Value, VariableNode))

                        make_env(scope.Owner).AddLet($"{var.Name}:{var.Scope.LineNumber}", var.Type)
                    Next
                End Sub

            Util.Traverse.NodesOnce(
                pgm,
                ns,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is BlockNode Then

                        Dim node = CType(child, BlockNode)
                        If node.Owner.Function.HasGeneric Then Return
                        make_closure(node)
                    End If

                    If TypeOf child Is FunctionNode Then

                        Dim node = CType(child, FunctionNode)
                        Dim func = CType(node.Type, RkFunction)
                        If Not func.HasGeneric Then

                            node.Bind.Each(
                                Sub(x)

                                    make_closure(CType(x.Key, FunctionNode))
                                    Dim env = make_env(x.Key)
                                    func.Arguments.Insert(0, New NamedValue With {.Name = env.Name, .Value = env})
                                    Coverage.Case()
                                End Sub)
                            Coverage.Case()
                        End If
                    End If

                    next_(child, current)
                End Sub)
        End Sub

        Public Shared Sub MakeFunction(root As SystemLibrary, func As IFunction, scope As INode, func_stmts As List(Of IStatementNode))

            If func.Body.Count > 0 Then Return

            If TypeOf scope Is FunctionNode Then

                CType(scope, FunctionNode).Arguments.Map(Function(x) x.Name).Each(Sub(arg) If arg.ClosureEnvironment Then func.Arguments.FindFirst(Function(x) x.Name.Equals(arg.Name)).Name = $"{arg.Name}:{arg.Scope.LineNumber}")
            End If

            Dim closure As OpValue = Nothing

            Dim get_closure =
                Function(var As VariableNode)

                    Dim name = $"{var.Name}:{var.Scope.LineNumber}"
                    If closure IsNot Nothing AndAlso CType(closure.Type, RkStruct).Local.Or(Function(x) x.Key.Equals(name)) Then Return closure
                    Dim v = func.Arguments.FindFirst(
                        Function(arg) TypeOf arg.Value Is RkStruct AndAlso
                                CType(arg.Value, RkStruct).ClosureEnvironment AndAlso
                                CType(arg.Value, RkStruct).Local.Or(Function(x) x.Key.Equals(name))
                            ).Value
                    Return New OpValue With {.Name = v.Name, .Type = v, .Scope = func}
                End Function

            Dim to_value As Func(Of IEvaluableNode, OpValue) =
                Function(x)

                    Dim t = x.Type
                    If TypeOf t Is RkGenericEntry Then Return New OpValue With {.Name = t.Name, .Type = func.Apply(CType(t, RkGenericEntry).ApplyIndex), .Scope = func}

                    If TypeOf x Is VariableNode Then

                        Dim var = CType(x, VariableNode)
                        If var.ClosureEnvironment Then

                            Return New OpProperty With {.Receiver = get_closure(var), .Name = $"{var.Name}:{var.Scope.LineNumber}", .Type = t, .Scope = func}

                        ElseIf TypeOf t Is RkNativeFunction Then

                            Dim f = CType(t, RkNativeFunction)
                            Dim name = $"##{f.Name}:({String.Join(", ", f.Arguments.Map(Function(arg) arg.Value))})=>{f.Return}"
                            Dim native = root.FindCurrentFunction(name).ToList
                            If native.Count = 0 Then

                                Dim wrapper As New RkFunction With {.Name = name, .Return = f.Return, .Scope = root, .Parent = root}
                                wrapper.Arguments.AddRange(f.Arguments)
                                ' $ret = $1 op $2
                                ' return($ret)
                                Dim ret As New OpValue With {.Name = "$ret", .Type = f.Return, .Scope = wrapper}
                                wrapper.Body.AddRange(f.CreateCallReturn(ret, f.Arguments.Map(Function(arg) to_value(New VariableNode(arg.Name) With {.Type = arg.Value})).ToArray))
                                wrapper.Body.Add(New InCode With {.Operator = InOperator.Return, .Left = ret})
                                root.AddFunction(wrapper)

                                Return New OpValue With {.Name = name, .Type = wrapper, .Scope = func}
                            Else
                                Return New OpValue With {.Name = name, .Type = native(0), .Scope = func}
                            End If

                        ElseIf var.Scope IsNot Nothing AndAlso TypeOf var.Scope.Lets(var.Name) Is VariableNode AndAlso CType(var.Scope.Lets(var.Name), VariableNode).LocalVariable Then

                            Return New OpValue With {.Name = $"{var.Name}:{var.Scope.LineNumber}", .Type = t, .Scope = func}
                        Else

                            Return New OpValue With {.Name = var.Name, .Type = t, .Scope = func}
                        End If
                    End If

                    If TypeOf x Is StringNode Then Return New OpString With {.String = CType(x, StringNode).String.ToString, .Type = t, .Scope = func}
                    If TypeOf x Is NumericNode Then Return New OpNumeric32 With {.Numeric = CType(x, NumericNode).Numeric, .Type = t, .Scope = func}
                    If TypeOf x Is StructNode Then Return New OpValue With {.Name = CType(x, StructNode).Name, .Type = t, .Scope = func}
                    If TypeOf x Is NullNode Then Return New OpNull With {.Type = t, .Scope = func}
                    If TypeOf x Is BoolNode Then Return New OpBool With {.Value = CType(x, BoolNode).Value, .Type = t, .Scope = func}
                    Return New OpValue With {.Type = t, .Scope = func}
                End Function

            Dim anonymus_count = 0
            Dim create_anonymus =
                Function()

                    anonymus_count += 1
                    Return $"###{anonymus_count}"
                End Function

            Dim get_receiver As Func(Of IEvaluableNode, IEnumerable(Of OpValue)) =
                Function(e As IEvaluableNode)

                    If TypeOf e Is VariableNode Then

                        Return {to_value(e)}

                    ElseIf TypeOf e Is PropertyNode Then

                        Dim prop = CType(e, PropertyNode)
                        Return get_receiver(prop.Left)
                    End If

                    Debug.Fail("do not convert receiver")
                    Return Nothing
                End Function

            Dim make_stmt_ret =
                Function(ret As OpValue, stmt As IEvaluableNode)

                    If stmt Is Nothing Then

                        Coverage.Case()
                        Return {New InCode With {.Operator = InOperator.Bind, .Return = ret}}

                    ElseIf TypeOf stmt Is ExpressionNode Then

                        Coverage.Case()
                        Dim node = CType(stmt, ExpressionNode)
                        Return If(node.Right Is Nothing, node.Function.CreateCallReturn(ret, to_value(node.Left)), node.Function.CreateCallReturn(ret, to_value(node.Left), to_value(node.Right)))

                    ElseIf TypeOf stmt Is PropertyNode Then

                        Coverage.Case()
                        Dim node = CType(stmt, PropertyNode)
                        Return {New InCode With {.Operator = InOperator.Dot, .Return = ret, .Left = to_value(node.Left), .Right = to_value(node.Right)}}

                    ElseIf TypeOf stmt Is FunctionCallNode Then

                        Coverage.Case()
                        Dim node = CType(stmt, FunctionCallNode)
                        Dim args = node.Arguments.Map(Function(x) to_value(x)).ToList
                        If node.Function.IsAnonymous Then

                            args.Insert(0, to_value(node.Expression))

                        ElseIf TypeOf node.Function Is RkNativeFunction Then

                            Dim f = CType(node.Function, RkNativeFunction)
                            Select Case f.Operator
                                Case InOperator.Constructor

                                    Dim stmts As New List(Of InCode0)
                                    stmts.Add(New InCode With {.Operator = InOperator.Alloc, .Return = ret, .Left = New OpValue With {.Name = ret.Type.Name, .Type = ret.Type, .Scope = func}})
                                    node.Function.Arguments.Each(Sub(x, i) stmts.Add(New InCode With {.Operator = InOperator.Bind, .Return = New OpProperty With {.Name = x.Name, .Receiver = ret, .Type = x.Value, .Scope = func}, .Left = args(i)}))
                                    Return stmts.ToArray

                                Case InOperator.Alloc

                                    args.Insert(0, New OpValue With {.Name = node.Type.Name, .Type = node.Type, .Scope = func})
                                    Dim xs = node.Function.CreateCallReturn(ret, args.ToArray)
                                    If args.Count = 1 OrElse CType(node.Type, RkStruct).Generics.Count > 0 Then Return xs
                                    Dim stmts = xs.ToList
                                    node.Function.Arguments.Cdr.Each(Sub(x, i) stmts.Add(New InCode With {.Operator = InOperator.Bind, .Return = New OpProperty With {.Name = x.Name, .Receiver = ret, .Type = x.Value, .Scope = func}, .Left = args(i + 1)}))
                                    Return stmts.ToArray
                            End Select
                        End If
                        Return node.Function.CreateCallReturn(ret, args.ToArray)

                    ElseIf TypeOf stmt Is VariableNode OrElse
                            TypeOf stmt Is NumericNode OrElse
                            TypeOf stmt Is StringNode OrElse
                            TypeOf stmt Is FunctionNode OrElse
                            TypeOf stmt Is NullNode OrElse
                            TypeOf stmt Is BoolNode Then

                        Coverage.Case()
                        Return {New InCode With {.Operator = InOperator.Bind, .Return = ret, .Left = to_value(stmt)}}

                    ElseIf TypeOf stmt Is TupleNode Then

                        Coverage.Case()
                        Dim tuple = CType(stmt, TupleNode)
                        Dim body As New List(Of InCode0)
                        body.AddRange(LoadFunction(root, "#Alloc", tuple.Type).CreateCallReturn(ret, New OpValue With {.Type = tuple.Type}))
                        tuple.Items.Each(Sub(x, i) body.Add(New InCode With {
                                .Operator = InOperator.Bind,
                                .Return = New OpProperty With {.Receiver = ret, .Name = (i + 1).ToString, .Type = x.Type, .Scope = func},
                                .Left = to_value(x)
                            }))
                        Return body.ToArray

                    ElseIf IsGeneric(stmt.GetType, GetType(ListNode(Of ))) Then

                        Dim xs As New OpArray With {.Type = stmt.Type}
                        Dim list = stmt.GetType.GetProperty("List").GetValue(stmt)
                        Dim count = list.GetType.GetProperty("Count")
                        Dim item = list.GetType.GetProperty("Item")
                        Dim index = New Object() {0}
                        For i = 0 To CInt(count.GetValue(list)) - 1

                            index(0) = i
                            xs.List.Add(to_value(CType(item.GetValue(list, index), IEvaluableNode)))
                        Next
                        Coverage.Case()
                        Return {New InCode With {.Operator = InOperator.Array, .Return = ret, .Left = xs}}

                    Else

                        Throw New Exception("unknown stmt")
                    End If
                End Function

            Dim make_stmt_let =
                Function(let_ As LetNode, stmt As IEvaluableNode)

                    If Not let_.IsInstance Then Return {}

                    Dim ret As OpValue
                    If let_.Var.ClosureEnvironment Then

                        ret = New OpProperty With {.Receiver = closure, .Name = $"{let_.Var.Name}:{let_.Var.Scope.LineNumber}", .Type = let_.Var.Type, .Scope = func}
                        Coverage.Case()

                    ElseIf let_.Receiver Is Nothing Then

                        ret = to_value(let_.Var)
                        Coverage.Case()
                    Else

                        ret = New OpProperty With {.Receiver = to_value(let_.Receiver), .Name = let_.Var.UniqueName, .Type = let_.Var.Type, .Scope = func}
                        Coverage.Case()
                    End If

                    Return make_stmt_ret(ret, stmt)
                End Function
            Dim make_if As Func(Of IfNode, List(Of InCode0)) = Nothing
            Dim make_switch As Func(Of SwitchNode, List(Of InCode0)) = Nothing
            Dim break_point As New Stack(Of InLabel)
            Dim goto_label As New Dictionary(Of Integer, InLabel)
            Dim make_label =
                Function(label As Integer)

                    If goto_label.ContainsKey(label) Then Return goto_label(label)
                    goto_label.Add(label, New InLabel)
                    Return goto_label(label)
                End Function
            Dim make_stmts As Func(Of List(Of IStatementNode), List(Of InCode0)) =
                Function(stmts)

                    Dim body As New List(Of InCode0)
                    For Each stmt In stmts

                        If TypeOf stmt Is LetNode Then

                            Dim let_ = CType(stmt, LetNode)
                            body.AddRange(make_stmt_let(let_, let_.Expression))
                            Coverage.Case()

                        ElseIf TypeOf stmt Is IfNode Then

                            body.AddRange(make_if(CType(stmt, IfNode)))
                            Coverage.Case()

                        ElseIf TypeOf stmt Is SwitchNode Then

                            body.AddRange(make_switch(CType(stmt, SwitchNode)))
                            Coverage.Case()

                        ElseIf TypeOf stmt Is FunctionCallNode Then

                            Dim node = CType(stmt, FunctionCallNode)
                            Dim args = node.Arguments.Map(Function(x) to_value(x)).ToList
                            If node.Function.IsAnonymous Then args.Insert(0, to_value(node.Expression))
                            body.AddRange(node.Function.CreateCall(args.ToArray))
                            Coverage.Case()

                        ElseIf TypeOf stmt Is BreakNode Then

                            body.Add(New InGoto With {.Label = break_point.Peek})
                            Coverage.Case()

                        ElseIf TypeOf stmt Is GotoNode Then

                            Dim node = CType(stmt, GotoNode)
                            body.Add(New InGoto With {.Label = make_label(node.Label)})
                            Coverage.Case()

                        ElseIf TypeOf stmt Is LabelNode Then

                            Dim node = CType(stmt, LabelNode)
                            body.Add(make_label(node.Label))
                            Coverage.Case()
                        End If
                    Next
                    Return body
                End Function
            make_if =
                Function(node)

                    Dim if_ = New InIf
                    Dim then_ = If(node.Then Is Nothing, New List(Of InCode0), make_stmts(node.Then.Statements))
                    Dim body As New List(Of InCode0)

                    If TypeOf node Is IfCastNode Then

                        Dim ifcast = CType(node, IfCastNode)
                        Dim bool = LoadStruct(root, "Bool")
                        Dim eq_r As New OpValue With {.Name = create_anonymus(), .Type = bool, .Scope = func}

                        body.Add(New InCode With {
                                .Operator = InOperator.CanCast,
                                .Return = eq_r,
                                .Left = to_value(ifcast.Condition),
                                .Right = to_value(ifcast.Declare)
                            })
                        if_.Condition = eq_r
                        then_.Insert(0, New InCode With {
                                .Operator = InOperator.Cast,
                                .Return = New OpValue With {.Name = ifcast.Var.Name, .Type = ifcast.Declare.Type, .Scope = func},
                                .Left = to_value(ifcast.Condition),
                                .Right = to_value(ifcast.Declare)
                            })
                    Else

                        if_.Condition = to_value(node.Condition)
                    End If

                    Dim endif_ = New InLabel
                    body.Add(if_)
                    body.AddRange(then_)
                    If node.Else IsNot Nothing Then

                        body.Add(New InGoto With {.Label = endif_})
                        Dim else_ = make_stmts(node.Else.Statements)
                        If else_.Count = 1 AndAlso TypeOf else_(0) Is InGoto Then

                            if_.Else = CType(else_(0), InGoto).Label
                            Coverage.Case()
                        Else

                            if_.Else = New InLabel
                            else_.Insert(0, if_.Else)
                            body.AddRange(else_)
                            Coverage.Case()
                        End If
                    Else

                        if_.Else = endif_
                        Coverage.Case()
                    End If

                    body.Add(endif_)
                    Return body
                End Function
            make_switch =
                Function(switch)

                    Dim body As New List(Of InCode0)
                    Dim last_label As New InLabel
                    Dim case_labels = switch.Case.Cdr.Map(Function(x) New InLabel).ToList
                    case_labels.Add(last_label)

                    Dim int_ = LoadStruct(root, "Int")
                    Dim bool = LoadStruct(root, "Bool")
                    Dim count_r As New OpValue With {.Name = create_anonymus(), .Type = int_, .Scope = func}
                    Dim eq_r As New OpValue With {.Name = create_anonymus(), .Type = bool, .Scope = func}
                    Dim minus_r As New OpValue With {.Name = create_anonymus(), .Type = int_, .Scope = func}

                    Dim count = Util.Functions.Memoization(Function() TryLoadFunction(CType(CType(switch.Expression.Type, RkCILStruct).GenericBase, RkCILStruct).FunctionNamespace, "Count", switch.Expression.Type))
                    Dim index = Util.Functions.Memoization(Function() TryLoadFunction(CType(CType(switch.Expression.Type, RkCILStruct).GenericBase, RkCILStruct).FunctionNamespace, "Item", switch.Expression.Type, int_))
                    Dim get_range = Util.Functions.Memoization(Function() TryLoadFunction(CType(CType(switch.Expression.Type, RkCILStruct).GenericBase, RkCILStruct).FunctionNamespace, "GetRange", switch.Expression.Type, int_, int_))
                    Dim minus = Util.Functions.Memoization(Function() TryLoadFunction(root, "-", int_, int_))
                    Dim eq = Util.Functions.Memoization(Function() TryLoadFunction(root, "==", int_, int_))
                    Dim gte = Util.Functions.Memoization(Function() TryLoadFunction(root, ">=", int_, int_))
                    Dim get_count = Util.Functions.Memoization(Sub() body.AddRange(count().CreateCallReturn(count_r, to_value(switch.Expression))))

                    For i = 0 To switch.Case.Count - 1

                        Dim case_ = switch.Case(i)
                        Dim next_ = case_labels(i)
                        break_point.Push(next_)

                        If TypeOf case_ Is CaseValueNode Then

                            Dim case_value = CType(case_, CaseValueNode)
                            body.AddRange(make_stmts(case_value.Value.Statements))
                            If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                            body.Add(New InGoto With {.Label = last_label})

                        ElseIf TypeOf case_ Is CaseCastNode Then

                            Dim case_cast = CType(case_, CaseCastNode)
                            body.Add(New InCode With {
                                    .Operator = InOperator.CanCast,
                                    .Return = eq_r,
                                    .Left = to_value(switch.Expression),
                                    .Right = to_value(case_cast.Declare)
                                })
                            body.Add(New InIf With {.Condition = eq_r, .Else = next_})
                            body.Add(New InCode With {
                                    .Operator = InOperator.Cast,
                                    .Return = New OpValue With {.Name = case_cast.Var.Name, .Type = case_cast.Declare.Type, .Scope = func},
                                    .Left = to_value(switch.Expression),
                                    .Right = to_value(case_cast.Declare)
                                })
                            If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                            body.Add(New InGoto With {.Label = last_label})

                        ElseIf TypeOf case_ Is CaseArrayNode Then

                            Dim case_array = CType(case_, CaseArrayNode)
                            body.AddRange(make_stmts(case_array.Statements))
                            If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                            body.Add(New InGoto With {.Label = last_label})
                        End If
                        break_point.Pop()
                        body.Add(next_)
                    Next

                    Return body
                End Function

            If func.Closure IsNot Nothing Then

                closure = New OpValue With {.Name = func.Closure.Name, .Type = func.Closure, .Scope = func}
                func.Body.AddRange(func.Closure.Initializer.CreateCallReturn(closure, New OpValue With {.Type = func.Closure, .Scope = func}))
                func.Arguments.Where(Function(arg) func.Closure.Local.ContainsKey(arg.Name)).Each(
                    Sub(x)

                        func.Body.Add(
                            New InCode With {
                                .Operator = InOperator.Bind,
                                .Return = New OpProperty With {.Name = x.Name, .Receiver = closure, .Type = x.Value, .Scope = func},
                                .Left = New OpValue With {.Name = x.Name, .Type = x.Value, .Scope = func}})
                        Coverage.Case()
                    End Sub)
                Coverage.Case()
            End If
            If func_stmts IsNot Nothing Then func.Body.AddRange(make_stmts(func_stmts))
        End Sub

        Public Shared Sub Translate(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Dim compleat As New Dictionary(Of IFunction, Boolean)

            Dim make_func =
                Sub(func As IFunction, scope As INode, func_stmts As List(Of IStatementNode))

                    If compleat.ContainsKey(func) AndAlso compleat(func) Then Return
                    MakeFunction(root, func, scope, func_stmts)
                    compleat(func) = True
                End Sub

            make_func(pgm.Function, Nothing, pgm.Statements)

            Util.Traverse.NodesOnce(
                pgm,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node = CType(child, StructNode)
                        Dim struct = CType(node.Type, RkStruct)
                        For Each s In struct.Scope.FindCurrentStruct(struct.Name).Where(Function(x) Not x.HasGeneric).By(Of RkStruct)

                            s.Initializer = CType(LoadFunction(s.Scope, "#Alloc", s), RkNativeFunction)
                            make_func(s.Initializer, node, node.Statements)
                        Next
                        Coverage.Case()

                    ElseIf TypeOf child Is ProgramNode Then

                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node = CType(child, FunctionNode)
                        Dim func = node.Function
                        If Not func.HasGeneric Then make_func(func, node, node.Statements)
                        Coverage.Case()

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

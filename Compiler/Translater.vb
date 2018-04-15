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

        Public Shared Sub ClosureTranslate(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Dim closures As New Dictionary(Of IScopeNode, RkStruct)
            Dim make_closure =
                Function(scope As IScopeNode)

                    If closures.ContainsKey(scope) Then Return closures(scope)

                    Dim env As New RkStruct With {.Scope = root, .ClosureEnvironment = True, .Parent = scope.Owner.Scope}
                    env.Name = $"##{scope.Owner.Name}"
                    For Each var In scope.Lets.Where(Function(v) TypeOf v.Value Is VariableNode AndAlso CType(v.Value, VariableNode).ClosureEnvironment)

                        env.AddLet(var.Key, CType(var.Value, VariableNode).Type)
                    Next
                    env.Initializer = CType(LoadFunction(root, "#Alloc", env), RkNativeFunction)
                    closures.Add(scope, env)
                    root.AddStruct(env)
                    scope.Owner.Scope.Closure = env
                    Coverage.Case()
                    Return env
                End Function

            Util.Traverse.NodesOnce(
                node,
                ns,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = CType(node_func.Type, RkFunction)
                        node_func.Bind.Do(
                            Sub(x)

                                Dim env = make_closure(x.Key)
                                rk_func.Arguments.Insert(0, New NamedValue With {.Name = env.Name, .Value = env})
                                Coverage.Case()
                            End Sub)
                        Coverage.Case()
                    End If

                    next_(child, current)
                End Sub)
        End Sub

        Public Shared Sub Translate(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Dim compleat As New Dictionary(Of IFunction, Boolean)

            Dim make_func =
                Sub(rk_func As IFunction, scope As INode, func_stmts As List(Of IStatementNode))

                    If compleat.ContainsKey(rk_func) AndAlso compleat(rk_func) Then Return
                    If rk_func.Body.Count > 0 Then Return

                    'Dim fix_map As New Dictionary(Of String, IType)
                    'If TypeOf scope Is FunctionNode Then rk_func.Apply.Do(Sub(x, i) fix_map(CType(scope, FunctionNode).Function.Generics(i).Name) = x)
                    'If TypeOf scope Is StructNode Then rk_func.Apply.Do(Sub(x, i) fix_map(CType(scope, StructNode).Struct.Generics(i).Name) = x)

                    Dim closure As OpValue = Nothing

                    Dim get_closure =
                        Function(var As VariableNode)

                            If closure IsNot Nothing AndAlso CType(closure.Type, RkStruct).Local.Or(Function(x) x.Key.Equals(var.Name)) Then Return closure
                            Dim v = rk_func.Arguments.FindFirst(
                                Function(arg) TypeOf arg.Value Is RkStruct AndAlso
                                        CType(arg.Value, RkStruct).ClosureEnvironment AndAlso
                                        CType(arg.Value, RkStruct).Local.Or(Function(x) x.Key.Equals(var.Name))
                                    ).Value
                            Return New OpValue With {.Name = v.Name, .Type = v, .Scope = rk_func}
                        End Function

                    Dim to_value =
                        Function(x As IEvaluableNode)

                            Dim t = x.Type
                            If TypeOf t Is RkGenericEntry Then Return New OpValue With {.Name = t.Name, .Type = rk_func.Apply(CType(t, RkGenericEntry).ApplyIndex), .Scope = rk_func}

                            If TypeOf x Is VariableNode Then

                                Dim var = CType(x, VariableNode)
                                If var.ClosureEnvironment Then

                                    Return New RkProperty With {.Receiver = get_closure(var), .Name = var.Name, .Type = t, .Scope = rk_func}

                                ElseIf var.Scope IsNot Nothing AndAlso TypeOf var.Scope.Lets(var.Name) Is VariableNode AndAlso CType(var.Scope.Lets(var.Name), VariableNode).LocalVariable Then

                                    Return New OpValue With {.Name = $"{var.Name}:{var.Scope.LineNumber}", .Type = t, .Scope = rk_func}
                                Else

                                    Return New OpValue With {.Name = var.Name, .Type = t, .Scope = rk_func}
                                End If
                            End If

                            If TypeOf x Is StringNode Then Return New OpString With {.String = CType(x, StringNode).String.ToString, .Type = t, .Scope = rk_func}
                            If TypeOf x Is NumericNode Then Return New OpNumeric32 With {.Numeric = CType(x, NumericNode).Numeric, .Type = t, .Scope = rk_func}
                            If TypeOf x Is StructNode Then Return New OpValue With {.Name = CType(x, StructNode).Name, .Type = t, .Scope = rk_func}
                            If TypeOf x Is NullNode Then Return New OpNull With {.Type = t, .Scope = rk_func}
                            Return New OpValue With {.Type = t, .Scope = rk_func}
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
                                Dim expr = CType(stmt, ExpressionNode)
                                Return If(expr.Right Is Nothing, expr.Function.CreateCallReturn(ret, to_value(expr.Left)), expr.Function.CreateCallReturn(ret, to_value(expr.Left), to_value(expr.Right)))

                            ElseIf TypeOf stmt Is PropertyNode Then

                                Coverage.Case()
                                Dim prop = CType(stmt, PropertyNode)
                                Return {New InCode With {.Operator = InOperator.Dot, .Return = ret, .Left = to_value(prop.Left), .Right = to_value(prop.Right)}}

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Coverage.Case()
                                Dim func = CType(stmt, FunctionCallNode)
                                Dim args = func.Arguments.Map(Function(x) to_value(x)).ToList
                                If func.Function.IsAnonymous Then

                                    args.Insert(0, New OpValue With {.Type = func.Type, .Scope = rk_func})
                                    args(0).Name = CType(func.Expression, VariableNode).Name

                                ElseIf TypeOf func.Function Is RkNativeFunction AndAlso CType(func.Function, RkNativeFunction).Operator = InOperator.Alloc Then

                                    args.Insert(0, New OpValue With {.Type = func.Type, .Scope = rk_func})
                                End If
                                Return func.Function.CreateCallReturn(ret, args.ToArray)

                            ElseIf TypeOf stmt Is VariableNode OrElse
                                    TypeOf stmt Is NumericNode OrElse
                                    TypeOf stmt Is StringNode OrElse
                                    TypeOf stmt Is FunctionNode OrElse
                                    TypeOf stmt Is NullNode Then

                                Coverage.Case()
                                Return {New InCode With {.Operator = InOperator.Bind, .Return = ret, .Left = to_value(stmt)}}

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

                                ret = New RkProperty With {.Receiver = closure, .Name = let_.Var.Name, .Type = let_.Var.Type, .Scope = rk_func}
                                Coverage.Case()

                            ElseIf let_.Receiver Is Nothing Then

                                ret = to_value(let_.Var)
                                Coverage.Case()
                            Else

                                ret = New RkProperty With {.Receiver = to_value(let_.Receiver), .Name = let_.Var.Name, .Type = let_.Var.Type, .Scope = rk_func}
                                Coverage.Case()
                            End If

                            Return make_stmt_ret(ret, stmt)
                        End Function
                    Dim make_if As Func(Of IfNode, List(Of InCode0)) = Nothing
                    Dim make_switch As Func(Of SwitchNode, List(Of InCode0)) = Nothing
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

                                    Dim func = CType(stmt, FunctionCallNode)
                                    Dim args = func.Arguments.Map(Function(x) to_value(x)).ToList
                                    If func.Function.IsAnonymous Then args.Insert(0, to_value(func.Expression))
                                    body.AddRange(func.Function.CreateCall(args.ToArray))
                                    Coverage.Case()
                                End If
                            Next
                            Return body
                        End Function
                    make_if =
                        Function(if_)

                            Dim rk_if = New InIf
                            Dim then_ = make_stmts(if_.Then.Statements)
                            Dim body As New List(Of InCode0)

                            If TypeOf if_ Is IfCastNode Then

                                Dim ifcast = CType(if_, IfCastNode)
                                Dim bool = LoadStruct(root, "Bool")
                                Dim eq_r As New OpValue With {.Name = create_anonymus(), .Type = bool, .Scope = rk_func}

                                body.Add(New InCode With {
                                        .Operator = InOperator.CanCast,
                                        .Return = eq_r,
                                        .Left = to_value(ifcast.Condition),
                                        .Right = to_value(ifcast.Declare)
                                    })
                                rk_if.Condition = eq_r
                                then_.Insert(0, New InCode With {
                                        .Operator = InOperator.Cast,
                                        .Return = New OpValue With {.Name = ifcast.Var.Name, .Type = ifcast.Declare.Type, .Scope = rk_func},
                                        .Left = to_value(ifcast.Condition),
                                        .Right = to_value(ifcast.Declare)
                                    })
                            Else

                                rk_if.Condition = to_value(if_.Condition)
                            End If

                            Dim endif_ = New InLabel
                            body.Add(rk_if)
                            body.AddRange(then_)
                            If if_.Else IsNot Nothing Then

                                body.Add(New InGoto With {.Label = endif_})
                                Dim else_ = make_stmts(if_.Else.Statements)
                                rk_if.Else = New InLabel
                                else_.Insert(0, rk_if.Else)
                                body.AddRange(else_)
                                Coverage.Case()
                            Else

                                rk_if.Else = endif_
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
                            Dim count_r As New OpValue With {.Name = create_anonymus(), .Type = int_, .Scope = rk_func}
                            Dim eq_r As New OpValue With {.Name = create_anonymus(), .Type = bool, .Scope = rk_func}
                            Dim minus_r As New OpValue With {.Name = create_anonymus(), .Type = int_, .Scope = rk_func}

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

                                If TypeOf case_ Is CaseCastNode Then

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
                                            .Return = New OpValue With {.Name = case_cast.Var.Name, .Type = case_cast.Declare.Type, .Scope = rk_func},
                                            .Left = to_value(switch.Expression),
                                            .Right = to_value(case_cast.Declare)
                                        })
                                    If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                                    body.Add(New InGoto With {.Label = last_label})

                                ElseIf TypeOf case_ Is CaseArrayNode Then

                                    get_count()
                                    Dim case_array = CType(case_, CaseArrayNode)
                                    Select Case case_array.Pattern.Count
                                        Case 0
                                            ' $$ = switch.Expression.Count == 0
                                            ' if $$
                                            '     case_.Then.Statements
                                            '     goto last_label
                                            ' else ...
                                            body.AddRange(eq().CreateCallReturn(eq_r, count_r, New OpNumeric32 With {.Numeric = 0, .Type = int_, .Scope = rk_func}))
                                            Dim if_ As New InIf With {.Condition = eq_r, .Else = next_}
                                            body.Add(if_)
                                            If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                                            body.Add(New InGoto With {.Label = last_label})

                                        Case 1
                                            ' $$ = switch.Expression.Count == 1
                                            ' if $$
                                            '     x = switch.Expression[0]
                                            '     case_.Then.Statements
                                            '     goto last_label
                                            ' else ...
                                            body.AddRange(eq().CreateCallReturn(eq_r, count_r, New OpNumeric32 With {.Numeric = 1, .Type = int_, .Scope = rk_func}))
                                            Dim if_ As New InIf With {.Condition = eq_r, .Else = next_}
                                            body.Add(if_)
                                            index().CreateCallReturn(to_value(case_array.Pattern(0)), to_value(switch.Expression), New OpNumeric32 With {.Numeric = 0, .Type = int_, .Scope = rk_func})
                                            If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                                            body.Add(New InGoto With {.Label = last_label})

                                        Case Else
                                            ' $$ = switch.Expression.Count >= n
                                            ' if $$
                                            '     x  = switch.Expression[0]
                                            '     xs = switch.Expression[n...]
                                            '     case_.Then.Statements
                                            '     goto last_label
                                            ' else ...
                                            body.AddRange(gte().CreateCallReturn(eq_r, count_r, New OpNumeric32 With {.Numeric = CUInt(case_array.Pattern.Count - 1), .Type = int_, .Scope = rk_func}))
                                            Dim if_ As New InIf With {.Condition = eq_r, .Else = next_}
                                            body.Add(if_)
                                            For j = 0 To case_array.Pattern.Count - 2

                                                body.AddRange(index().CreateCallReturn(to_value(case_array.Pattern(j)), to_value(switch.Expression), New OpNumeric32 With {.Numeric = CUInt(j), .Type = int_, .Scope = rk_func}))
                                            Next
                                            body.AddRange(minus().CreateCallReturn(minus_r, count_r, New OpNumeric32 With {.Numeric = CUInt(case_array.Pattern.Count - 1), .Type = int_, .Scope = rk_func}))
                                            body.AddRange(
                                                get_range().CreateCallReturn(
                                                    to_value(case_array.Pattern(case_array.Pattern.Count - 1)),
                                                    to_value(switch.Expression),
                                                    New OpNumeric32 With {.Numeric = CUInt(case_array.Pattern.Count - 1), .Type = int_, .Scope = rk_func},
                                                    minus_r))
                                            If case_.Then IsNot Nothing Then body.AddRange(make_stmts(case_.Then.Statements))
                                            body.Add(New InGoto With {.Label = last_label})

                                    End Select
                                End If
                                body.Add(next_)
                            Next

                            Return body
                        End Function

                    If rk_func.Closure IsNot Nothing Then

                        closure = New OpValue With {.Name = rk_func.Closure.Name, .Type = rk_func.Closure, .Scope = rk_func}
                        rk_func.Body.AddRange(rk_func.Closure.Initializer.CreateCallReturn(closure, New OpValue With {.Type = rk_func.Closure, .Scope = rk_func}))
                        rk_func.Arguments.Where(Function(arg) rk_func.Closure.Local.ContainsKey(arg.Name)).Do(
                            Sub(x)

                                rk_func.Body.Add(
                                    New InCode With {
                                        .Operator = InOperator.Bind,
                                        .Return = New RkProperty With {.Name = x.Name, .Receiver = closure, .Type = x.Value, .Scope = rk_func},
                                        .Left = New OpValue With {.Name = x.Name, .Type = x.Value, .Scope = rk_func}})
                                Coverage.Case()
                            End Sub)
                        Coverage.Case()
                    End If
                    If func_stmts IsNot Nothing Then rk_func.Body.AddRange(make_stmts(func_stmts))

                    compleat(rk_func) = True
                End Sub

            make_func(node.Function, Nothing, node.Statements)

            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = CType(node_struct.Type, RkStruct)
                        For Each struct In rk_struct.Scope.FindCurrentStruct(rk_struct.Name).Where(Function(x) Not x.HasGeneric).By(Of RkStruct)

                            struct.Initializer = CType(LoadFunction(struct.Scope, "#Alloc", {CType(struct, IType)}.Join(struct.Apply).ToArray), RkNativeFunction)
                            make_func(struct.Initializer, node_struct, node_struct.Statements)
                        Next
                        Coverage.Case()

                    ElseIf TypeOf child Is ProgramNode Then

                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = node_func.Function
                        If Not rk_func.HasGeneric Then make_func(rk_func, node_func, node_func.Statements)
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        If node_call.Function?.FunctionNode IsNot Nothing Then make_func(node_call.Function, node_call.Function.FunctionNode, node_call.Function.FunctionNode.Statements)
                        Coverage.Case()

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

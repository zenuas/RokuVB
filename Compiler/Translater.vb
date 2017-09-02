Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Manager.SystemLirary
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util
Imports Roku.Util.ArrayExtension
Imports Roku.Util.TypeHelper
Imports System.Diagnostics


Namespace Compiler

    Public Class Translater

        Public Shared Sub ClosureTranslate(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

            Dim closures As New Dictionary(Of IScopeNode, RkStruct)
            Dim make_closure =
                Function(scope As IScopeNode)

                    If closures.ContainsKey(scope) Then Return closures(scope)

                    Dim env As New RkStruct With {.Scope = root, .ClosureEnvironment = True, .Parent = scope.Owner.Function}
                    env.Name = $"##{scope.Owner.Name}"
                    For Each var In scope.Scope.Where(Function(v) TypeOf v.Value Is VariableNode AndAlso CType(v.Value, VariableNode).ClosureEnvironment)

                        env.AddLet(var.Key, CType(var.Value, VariableNode).Type)
                    Next
                    env.Initializer = CType(LoadFunction(root, "#Alloc", env), RkNativeFunction)
                    closures.Add(scope, env)
                    root.AddStruct(env)
                    scope.Owner.Function.Closure = env
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

        Public Shared Sub Translate(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

            Dim compleat As New Dictionary(Of IFunction, Boolean)

            Dim make_func =
                Sub(rk_func As IFunction, scope As INode, func_stmts As List(Of IEvaluableNode))

                    If compleat.ContainsKey(rk_func) AndAlso compleat(rk_func) Then Return

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

                    Dim make_stmt =
                        Function(stmt As IEvaluableNode)

                            If TypeOf stmt Is ExpressionNode Then

                                Throw New NotSupportedException

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Coverage.Case()
                                Dim func = CType(stmt, FunctionCallNode)
                                Dim args = func.Arguments.Map(Function(x) to_value(x)).ToList
                                Return func.Function.CreateCall(to_value(func.Expression), args.ToArray)
                            Else

                                Throw New Exception("unknown stmt")
                            End If
                        End Function
                    Dim make_stmt_let =
                        Function(let_ As LetNode, stmt As IEvaluableNode)

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

                            If stmt Is Nothing Then

                                Coverage.Case()
                                Return {New InCode With {.Operator = InOperator.Bind, .Return = ret}}

                            ElseIf TypeOf stmt Is ExpressionNode Then

                                Coverage.Case()
                                Dim expr = CType(stmt, ExpressionNode)
                                Return If(expr.Right Is Nothing, expr.Function.CreateCallReturn(to_value(expr), ret, to_value(expr.Left)), expr.Function.CreateCallReturn(to_value(expr), ret, to_value(expr.Left), to_value(expr.Right)))

                            ElseIf TypeOf stmt Is PropertyNode Then

                                Coverage.Case()
                                Dim prop = CType(stmt, PropertyNode)
                                Return {New InCode With {.Operator = InOperator.Dot, .Return = ret, .Left = to_value(prop.Left), .Right = to_value(prop.Right)}}

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Coverage.Case()
                                Dim func = CType(stmt, FunctionCallNode)
                                Dim args = func.Arguments.Map(Function(x) to_value(x)).ToList
                                If TypeOf func.Function Is RkNativeFunction AndAlso CType(func.Function, RkNativeFunction).Operator = InOperator.Alloc Then args.Insert(0, New OpValue With {.Type = func.Type, .Scope = rk_func})
                                Return func.Function.CreateCallReturn(to_value(func.Expression), ret, args.ToArray)

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
                    Dim make_if As Func(Of IfNode, List(Of InCode0)) = Nothing
                    Dim make_stmts As Func(Of List(Of IEvaluableNode), List(Of InCode0)) =
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

                                Else
                                    body.AddRange(make_stmt(stmt))
                                    Coverage.Case()
                                End If
                            Next
                            Return body
                        End Function
                    make_if =
                        Function(if_)

                            Dim rk_if = New InIf With {.Condition = to_value(if_.Condition)}
                            Dim then_ = make_stmts(if_.Then.Statements)
                            Dim endif_ = New InLabel
                            If then_.Count > 0 Then

                                rk_if.Then = New InLabel
                                then_.Insert(0, rk_if.Then)
                                Coverage.Case()
                            Else
                                rk_if.Then = endif_
                                Debug.Fail("not test")
                            End If

                            Dim body As New List(Of InCode0)
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

                    If rk_func.Closure IsNot Nothing Then

                        closure = New OpValue With {.Name = rk_func.Closure.Name, .Type = rk_func.Closure, .Scope = rk_func}
                        rk_func.Body.AddRange(rk_func.Closure.Initializer.CreateCallReturn(Nothing, closure, New OpValue With {.Type = rk_func.Closure, .Scope = rk_func}))
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
                        For Each struct In rk_struct.Scope.FindCurrentStruct(rk_struct.Name).Where(Function(x) Not x.HasGeneric)

                            struct.Initializer = CType(LoadFunction(struct.Scope, "#Alloc", {CType(struct, IType)}.Join(struct.Apply).ToArray), RkNativeFunction)
                            make_func(struct.Initializer, node_struct, node_struct.Statements)
                        Next
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = node_func.Function
                        If Not rk_func.HasGeneric Then make_func(rk_func, node_func, node_func.Body.Statements)
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        If node_call.Function?.FunctionNode IsNot Nothing Then make_func(node_call.Function, node_call.Function.FunctionNode, node_call.Function.FunctionNode.Body.Statements)
                        Coverage.Case()

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

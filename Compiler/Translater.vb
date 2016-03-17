Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Util.ArrayExtension
Imports System.Diagnostics


Namespace Compiler

    Public Class Translater

        Public Shared Sub Translate(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

            Dim compleat As New Dictionary(Of RkFunction, Boolean)
            Dim returns = root.Functions("return")

            Dim make_func =
                Sub(rk_func As RkFunction, scope As INode, func_stmts As List(Of IEvaluableNode))

                    If compleat.ContainsKey(rk_func) AndAlso compleat(rk_func) Then Return

                    'Dim fix_map As New Dictionary(Of String, IType)
                    'If TypeOf scope Is FunctionNode Then rk_func.Apply.Do(Sub(x, i) fix_map(CType(scope, FunctionNode).Function.Generics(i).Name) = x)
                    'If TypeOf scope Is StructNode Then rk_func.Apply.Do(Sub(x, i) fix_map(CType(scope, StructNode).Struct.Generics(i).Name) = x)

                    Dim closure As RkValue = Nothing

                    Dim get_closure =
                        Function(var As VariableNode)

                            If closure IsNot Nothing AndAlso CType(closure.Type, RkStruct).Local.Or(Function(x) x.Key.Equals(var.Name)) Then Return closure
                            Dim v = rk_func.Arguments.FindFirst(
                                Function(arg) TypeOf arg.Value Is RkStruct AndAlso
                                        CType(arg.Value, RkStruct).ClosureEnvironment AndAlso
                                        CType(arg.Value, RkStruct).Local.Or(Function(x) x.Key.Equals(var.Name))
                                    ).Value
                            Return New RkValue With {.Name = v.Name, .Type = v, .Scope = rk_func}
                        End Function

                    Dim to_value =
                        Function(x As IEvaluableNode)

                            Debug.Assert(TypeOf x.Type IsNot RkGenericEntry)
                            Dim t = x.Type

                            If TypeOf x Is VariableNode Then

                                Dim var = CType(x, VariableNode)
                                If var.ClosureEnvironment Then

                                    Return New RkProperty With {.Receiver = get_closure(var), .Name = var.Name, .Type = t, .Scope = rk_func}
                                Else

                                    Return New RkValue With {.Name = var.Name, .Type = t, .Scope = rk_func}
                                End If
                            End If

                            If TypeOf x Is StringNode Then Return New RkString With {.String = CType(x, StringNode).String.ToString, .Type = t, .Scope = rk_func}
                            If TypeOf x Is NumericNode Then Return New RkNumeric32 With {.Numeric = CType(x, NumericNode).Numeric, .Type = t, .Scope = rk_func}
                            If TypeOf x Is StructNode Then Return New RkValue With {.Name = CType(x, StructNode).Name, .Type = t, .Scope = rk_func}
                            Return New RkValue With {.Type = t, .Scope = rk_func}
                        End Function

                    Dim make_stmt =
                        Function(stmt As IEvaluableNode)

                            If TypeOf stmt Is ExpressionNode Then

                                Throw New NotSupportedException

                            ElseIf TypeOf stmt Is PropertyNode Then

                                Dim prop = CType(stmt, PropertyNode)
                                Return {New RkCode With {.Operator = RkOperator.Dot, .Left = to_value(prop.Left), .Right = to_value(prop.Right)}}

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Dim func = CType(stmt, FunctionCallNode)
                                If returns.Or(Function(ret) func.Function Is ret) Then

                                    If func.Arguments.Length > 0 Then

                                        Return {New RkCode With {.Operator = RkOperator.Return, .Left = to_value(func.Arguments(0))}}
                                    Else
                                        Return {New RkCode0 With {.Operator = RkOperator.Return}}
                                    End If
                                Else

                                    If TypeOf func.Function Is RkNativeFunction AndAlso CType(func.Function, RkNativeFunction).Operator = RkOperator.Alloc Then

                                        Throw New NotSupportedException
                                    Else
                                        Return func.Function.CreateCall(to_value(func.Expression), func.Arguments.Map(Function(x) to_value(x)).ToArray)
                                    End If
                                End If
                            Else

                                Throw New Exception("unknown stmt")
                            End If
                        End Function
                    Dim make_stmt_let =
                        Function(let_ As LetNode, stmt As IEvaluableNode)

                            Dim ret As RkValue
                            If let_.Var.ClosureEnvironment Then

                                ret = New RkProperty With {.Receiver = closure, .Name = let_.Var.Name, .Type = let_.Var.Type, .Scope = rk_func}

                            ElseIf let_.Receiver Is Nothing Then

                                ret = to_value(let_.Var)
                            Else

                                ret = New RkProperty With {.Receiver = to_value(let_.Receiver), .Name = let_.Var.Name, .Type = let_.Var.Type, .Scope = rk_func}
                            End If

                            If stmt Is Nothing Then

                                Return {New RkCode With {.Operator = RkOperator.Bind, .Return = ret}}

                            ElseIf TypeOf stmt Is ExpressionNode Then

                                Dim expr = CType(stmt, ExpressionNode)
                                Return If(expr.Right Is Nothing, expr.Function.CreateCallReturn(to_value(expr), ret, to_value(expr.Left)), expr.Function.CreateCallReturn(to_value(expr), ret, to_value(expr.Left), to_value(expr.Right)))

                            ElseIf TypeOf stmt Is PropertyNode Then

                                Dim prop = CType(stmt, PropertyNode)
                                Return {New RkCode With {.Operator = RkOperator.Dot, .Return = ret, .Left = to_value(prop.Left), .Right = to_value(prop.Right)}}

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Dim func = CType(stmt, FunctionCallNode)
                                Dim args = func.Arguments.Map(Function(x) to_value(x)).ToList
                                If TypeOf func.Function Is RkNativeFunction AndAlso CType(func.Function, RkNativeFunction).Operator = RkOperator.Alloc Then args.Insert(0, New RkValue With {.Type = func.Type, .Scope = rk_func})
                                Return func.Function.CreateCallReturn(to_value(func.Expression), ret, args.ToArray)

                            ElseIf TypeOf stmt Is VariableNode OrElse
                                    TypeOf stmt Is NumericNode OrElse
                                    TypeOf stmt Is StringNode OrElse
                                    TypeOf stmt Is FunctionNode Then

                                Return {New RkCode With {.Operator = RkOperator.Bind, .Return = ret, .Left = to_value(stmt)}}

                            Else

                                Throw New Exception("unknown stmt")
                            End If
                        End Function
                    Dim make_if As Func(Of IfNode, List(Of RkCode0)) = Nothing
                    Dim make_stmts As Func(Of List(Of IEvaluableNode), List(Of RkCode0)) =
                        Function(stmts)

                            Dim body As New List(Of RkCode0)
                            For Each stmt In stmts

                                If TypeOf stmt Is LetNode Then

                                    Dim let_ = CType(stmt, LetNode)
                                    body.AddRange(make_stmt_let(let_, let_.Expression))

                                ElseIf TypeOf stmt Is IfNode Then

                                    body.AddRange(make_if(CType(stmt, IfNode)))

                                Else
                                    body.AddRange(make_stmt(stmt))
                                End If
                            Next
                            Return body
                        End Function
                    make_if =
                        Function(if_)

                            Dim rk_if = New RkIf With {.Condition = to_value(if_.Condition)}
                            Dim then_ = make_stmts(if_.Then.Statements)
                            Dim endif_ = New RkLabel
                            If then_.Count > 0 Then

                                rk_if.Then = New RkLabel
                                then_.Insert(0, rk_if.Then)
                            Else
                                rk_if.Then = endif_
                            End If

                            Dim body As New List(Of RkCode0)
                            body.Add(rk_if)
                            body.AddRange(then_)
                            If if_.Else IsNot Nothing Then

                                body.Add(New RkGoto With {.Label = endif_})
                                Dim else_ = make_stmts(if_.Else.Statements)
                                rk_if.Else = New RkLabel
                                else_.Insert(0, rk_if.Else)
                                body.AddRange(else_)
                            Else

                                rk_if.Else = endif_
                            End If
                            body.Add(endif_)
                            Return body
                        End Function

                    If rk_func.Closure IsNot Nothing Then

                        closure = New RkValue With {.Name = rk_func.Closure.Name, .Type = rk_func.Closure, .Scope = rk_func}
                        rk_func.Body.AddRange(rk_func.Closure.Initializer.CreateCallReturn(Nothing, closure, New RkValue With {.Type = rk_func.Closure, .Scope = rk_func}))
                        rk_func.Arguments.Where(Function(arg) rk_func.Closure.Local.ContainsKey(arg.Name)).Do(
                            Sub(x)

                                rk_func.Body.Add(
                                    New RkCode With {
                                        .Operator = RkOperator.Bind,
                                        .Return = New RkProperty With {.Name = x.Name, .Receiver = closure, .Type = x.Value, .Scope = rk_func},
                                        .Left = New RkValue With {.Name = x.Name, .Type = x.Value, .Scope = rk_func}})
                            End Sub)
                    End If
                    If func_stmts IsNot Nothing Then rk_func.Body.AddRange(make_stmts(func_stmts))

                    compleat(rk_func) = True
                End Sub

            Dim ctor As New RkFunction With {.Name = ".ctor", .FunctionNode = New FunctionNode("") With {.Body = CType(node, BlockNode)}, .Namespace = ns}
            make_func(ctor, Nothing, node.Statements)
            ns.AddFunction(ctor)

            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = CType(node_struct.Type, RkStruct)
                        For Each struct In rk_struct.Namespace.Structs(rk_struct.Name).Where(Function(x) Not x.HasGeneric)

                            make_func(struct.Initializer, node_struct, node_struct.Statements)
                        Next

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = node_func.Function
                        If Not rk_func.HasGeneric Then make_func(rk_func, node_func, node_func.Body.Statements)

                    ElseIf TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        If node_call.Function.FunctionNode IsNot Nothing Then make_func(node_call.Function, node_call.Function.FunctionNode, node_call.Function.FunctionNode.Body.Statements)

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

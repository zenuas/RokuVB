Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


Namespace Compiler

    Public Class Translater

        Public Shared Sub Translate(node As INode, root As RkNamespace)

            Dim compleat As New Dictionary(Of RkFunction, Boolean)
            Dim returns = root.Functions("return")

            Dim make_func =
                Sub(rk_func As RkFunction, node_func As FunctionNode, node_body As BlockNode)

                    If compleat.ContainsKey(rk_func) AndAlso compleat(rk_func) Then Return

                    Dim fix_map As New Dictionary(Of String, IType)
                    If rk_func.Apply IsNot Nothing AndAlso node_func IsNot Nothing Then rk_func.Apply.Do(Sub(x, i) fix_map(node_func.Function.Generics(i).Name) = x)

                    Dim to_value =
                        Function(x As IEvaluableNode)

                            Dim t = If(TypeOf x.Type Is RkGenericEntry, fix_map(x.Type.Name), x.Type)

                            If TypeOf x Is VariableNode Then Return New RkValue With {.Name = CType(x, VariableNode).Name, .Type = t, .Scope = rk_func}
                            If TypeOf x Is StringNode Then Return New RkString With {.String = CType(x, StringNode).String.ToString, .Type = t, .Scope = rk_func}
                            If TypeOf x Is NumericNode Then Return New RkNumeric32 With {.Numeric = CType(x, NumericNode).Numeric, .Type = t, .Scope = rk_func}
                            Return New RkValue With {.Type = t, .Scope = rk_func}
                        End Function
                    Dim make_expr = Function(x As ExpressionNode) If(x.Right Is Nothing, x.Function.CreateCall(to_value(x.Left)), x.Function.CreateCall(to_value(x.Left), to_value(x.Right)))
                    Dim make_expr_ret = Function(ret As RkValue, x As ExpressionNode) If(x.Right Is Nothing, x.Function.CreateCallReturn(ret, to_value(x.Left)), x.Function.CreateCallReturn(ret, to_value(x.Left), to_value(x.Right)))

                    If node_body IsNot Nothing Then

                        For Each stmt In node_body.Statements

                            If TypeOf stmt Is ExpressionNode Then

                                Dim expr = CType(stmt, ExpressionNode)
                                rk_func.Body.AddRange(make_expr(expr))

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Dim func = CType(stmt, FunctionCallNode)
                                If returns.Or(Function(ret) func.Function Is ret) Then

                                    If func.Arguments.Length > 0 Then

                                        rk_func.Body.Add(New RkCode With {.Operator = RkOperator.Return, .Left = to_value(func.Arguments(0))})
                                    Else
                                        rk_func.Body.Add(New RkCode0 With {.Operator = RkOperator.Return})
                                    End If
                                Else

                                    rk_func.Body.AddRange(func.Function.CreateCall(func.Arguments.Map(Function(x) to_value(x)).ToArray))
                                End If

                            ElseIf TypeOf stmt Is LetNode Then

                                Dim let_ = CType(stmt, LetNode)
                                If TypeOf let_.Expression Is ExpressionNode Then

                                    rk_func.Body.AddRange(make_expr_ret(let_.Var, CType(let_.Expression, ExpressionNode)))

                                ElseIf TypeOf let_.Expression Is FunctionCallNode Then

                                    Dim func = CType(let_.Expression, FunctionCallNode)
                                    rk_func.Body.AddRange(func.Function.CreateCallReturn(New RkValue With {.Name = let_.Var.Name, .Type = let_.Type}, func.Arguments.Map(Function(x) to_value(x)).ToArray))
                                End If

                            ElseIf TypeOf stmt Is IfNode Then

                                Dim if_ = CType(stmt, IfNode)

                            End If
                        Next
                    End If

                    compleat(rk_func) = True
                End Sub

            If TypeOf node Is BlockNode Then

                Dim ctor As New RkFunction With {.Name = ".ctor"}
                make_func(ctor, Nothing, CType(node, BlockNode))
                root.AddFunction(ctor)
            End If

            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = CType(node_struct.Type, RkStruct)

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = node_func.Function
                        If Not rk_func.HasGeneric Then make_func(rk_func, node_func, node_func.Body)

                    ElseIf TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        make_func(node_call.Function, node_call.Function.FunctionNode, node_call.Function.FunctionNode?.Body)

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

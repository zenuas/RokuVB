Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Translater

        Public Shared Sub Translate(node As INode, root As RkNamespace)

            Dim compleat As New Dictionary(Of RkFunction, Boolean)

            Dim make_func =
                Sub(rk_func As RkFunction, node_func As FunctionNode)

                    If compleat.ContainsKey(rk_func) AndAlso compleat(rk_func) Then Return

                    Dim fix_map As New Dictionary(Of String, IType)
                    If rk_func.Apply IsNot Nothing Then Util.Functions.Do(rk_func.Apply, Sub(x, i) fix_map(node_func.Function.Generics(i).Name) = x)

                    Dim to_value =
                        Function(x As IEvaluableNode)

                            Dim t = If(TypeOf x.Type Is RkGenericEntry, fix_map(x.Type.Name), x.Type)

                            If TypeOf x Is VariableNode Then Return New RkValue With {.Name = CType(x, VariableNode).Name, .Type = t}
                            If TypeOf x Is StringNode Then Return New RkString With {.String = CType(x, StringNode).String.ToString, .Type = t}
                            If TypeOf x Is NumericNode Then Return New RkNumeric32 With {.Numeric = CType(x, NumericNode).Numeric, .Type = t}
                            Return New RkValue With {.Type = t}
                        End Function
                    Dim make_expr = Function(x As ExpressionNode) If(x.Right Is Nothing, x.Function.CreateCall(to_value(x.Left)), x.Function.CreateCall(to_value(x.Left), to_value(x.Right)))
                    Dim make_expr_ret = Function(ret As RkValue, x As ExpressionNode) If(x.Right Is Nothing, x.Function.CreateCallReturn(ret, to_value(x.Left)), x.Function.CreateCallReturn(ret, to_value(x.Left), to_value(x.Right)))

                    For Each stmt In node_func.Body.Statements

                        If stmt.Type Is Nothing Then

                            Continue For
                        End If

                        If TypeOf stmt Is ExpressionNode Then

                            Dim expr = CType(stmt, ExpressionNode)
                            rk_func.Body.AddRange(make_expr(expr))

                        ElseIf TypeOf stmt Is FunctionCallNode Then

                            Dim func = CType(stmt, FunctionCallNode)

                        ElseIf TypeOf stmt Is LetNode Then

                            Dim let_ = CType(stmt, LetNode)
                            If TypeOf let_.Expression Is ExpressionNode Then

                                rk_func.Body.AddRange(make_expr_ret(let_.Var, CType(let_.Expression, ExpressionNode)))
                            End If

                        ElseIf TypeOf stmt Is IfNode Then

                            Dim if_ = CType(stmt, IfNode)

                        End If
                    Next

                    compleat(rk_func) = True
                End Sub

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
                        If Not rk_func.HasGeneric Then make_func(rk_func, node_func)

                    ElseIf TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        If TypeOf node_call.Expression Is FunctionNode Then make_func(node_call.Function, CType(node_call.Expression, FunctionNode))

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

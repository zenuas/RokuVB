Imports System
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
                Sub(rk_func As RkFunction, node_func As FunctionNode, func_stmts As List(Of IEvaluableNode))

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

                    Dim make_stmt =
                        Function(stmt As IEvaluableNode)

                            If TypeOf stmt Is ExpressionNode Then

                                Dim expr = CType(stmt, ExpressionNode)
                                Return If(expr.Right Is Nothing, expr.Function.CreateCall(to_value(expr.Left)), expr.Function.CreateCall(to_value(expr.Left), to_value(expr.Right)))

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

                                        Return func.Function.CreateCall(to_value(func.Expression))
                                    Else
                                        Return func.Function.CreateCall(func.Arguments.Map(Function(x) to_value(x)).ToArray)
                                    End If
                                End If
                            Else

                                Throw New Exception("unknown stmt")
                            End If
                        End Function
                    Dim make_stmt_let =
                        Function(let_ As LetNode, stmt As IEvaluableNode)

                            If stmt Is Nothing Then

                                Return {New RkCode With {.Operator = RkOperator.Bind, .Return = to_value(let_.Var)}}

                            ElseIf TypeOf stmt Is ExpressionNode Then

                                Dim expr = CType(stmt, ExpressionNode)
                                Dim ret = to_value(let_.Var)
                                Return If(expr.Right Is Nothing, expr.Function.CreateCallReturn(ret, to_value(expr.Left)), expr.Function.CreateCallReturn(ret, to_value(expr.Left), to_value(expr.Right)))

                            ElseIf TypeOf stmt Is PropertyNode Then

                                Dim prop = CType(stmt, PropertyNode)
                                Return {New RkCode With {.Operator = RkOperator.Dot, .Return = to_value(let_.Var), .Left = to_value(prop.Left), .Right = to_value(prop.Right)}}

                            ElseIf TypeOf stmt Is FunctionCallNode Then

                                Dim func = CType(stmt, FunctionCallNode)
                                If TypeOf func.Function Is RkNativeFunction AndAlso CType(func.Function, RkNativeFunction).Operator = RkOperator.Alloc Then

                                    Return func.Function.CreateCallReturn(to_value(let_.Var), to_value(func.Expression))
                                Else
                                    Return func.Function.CreateCallReturn(to_value(let_.Var), func.Arguments.Map(Function(x) to_value(x)).ToArray)
                                End If

                            ElseIf TypeOf stmt Is VariableNode OrElse
                                    TypeOf stmt Is NumericNode OrElse
                                    TypeOf stmt Is StringNode Then

                                Return {New RkCode With {.Operator = RkOperator.Bind, .Return = to_value(let_.Var), .Left = to_value(stmt)}}

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

                    If func_stmts IsNot Nothing Then rk_func.Body.AddRange(make_stmts(func_stmts))

                    compleat(rk_func) = True
                End Sub

            If TypeOf node Is BlockNode Then

                Dim ctor As New RkFunction With {.Name = ".ctor", .FunctionNode = New FunctionNode("") With {.Body = CType(node, BlockNode)}}
                make_func(ctor, Nothing, CType(node, BlockNode).Statements)
                root.AddFunction(ctor)
            End If

            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = CType(node_struct.Type, RkStruct)
                        rk_struct.Initializer = CType(current.GetFunction("#Alloc", rk_struct), RkNativeFunction)
                        make_func(rk_struct.Initializer, Nothing, node_struct.Statements)

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

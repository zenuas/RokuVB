Imports System
Imports Roku.Node
Imports Roku.Parser.MyParser
Imports Roku.Util
Imports Roku.Util.TypeHelper


Namespace Compiler

    Public Class Normalize

        Public Shared Sub Normalization(pgm As ProgramNode)

            Util.Traverse.NodesOnce(
                pgm,
                New With {.VarIndex = 0},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        Dim program_pointer = 0

                        Dim to_let_linenum =
                            Function(e As IEvaluableNode, linenum As INode)

                                Dim var As New VariableNode($"$${user.VarIndex}") With {.Scope = block}
                                block.Lets.Add(var.Name, var)
                                If linenum IsNot Nothing Then var.AppendLineNumber(linenum)
                                user.VarIndex += 1

                                Dim let_ As New LetNode With {.Var = var, .Expression = e}
                                If linenum IsNot Nothing Then let_.AppendLineNumber(linenum)
                                block.Statements.Insert(program_pointer, let_)
                                program_pointer += 1
                                Coverage.Case()
                                Return var
                            End Function

                        Dim to_let = Function(e As IEvaluableNode) to_let_linenum(e, e)

                        Dim to_block =
                            Function(var As VariableNode, e As IEvaluableNode) As BlockNode

                                Dim then_block As New BlockNode(e.LineNumber.Value) With {.InnerScope = True, .Parent = block}
                                then_block.AddStatement(CreateLetNode(var, e))
                                Return then_block
                            End Function

                        Dim insert_let As Func(Of IEvaluableNode, IEvaluableNode) =
                            Function(e)

                                If TypeOf e Is ExpressionNode Then

                                    Dim expr = CType(e, ExpressionNode)
                                    If expr.Operator.Equals("()") Then Return insert_let(expr.Left)

                                    expr.Left = insert_let(expr.Left)
                                    expr.Right = insert_let(expr.Right)
                                    Coverage.Case()
                                    Return to_let(expr)

                                ElseIf TypeOf e Is PropertyNode Then

                                    Dim prop = CType(e, PropertyNode)
                                    prop.Left = insert_let(prop.Left)
                                    Coverage.Case()
                                    Return to_let(prop)

                                ElseIf TypeOf e Is FunctionCallNode Then

                                    Dim call_ = CType(e, FunctionCallNode)
                                    call_.Expression = insert_let(call_.Expression)
                                    For i = 0 To call_.Arguments.Length - 1

                                        call_.Arguments(i) = insert_let(call_.Arguments(i))
                                    Next
                                    Coverage.Case()
                                    Return to_let(call_)

                                ElseIf TypeOf e Is TupleNode Then

                                    Dim tuple = CType(e, TupleNode)
                                    For i = 0 To tuple.Items.Length - 1

                                        tuple.Items(i) = insert_let(tuple.Items(i))
                                    Next
                                    Coverage.Case()
                                    Return to_let(e)

                                ElseIf TypeOf e Is IfExpressionNode Then

                                    Dim ifexpr = CType(e, IfExpressionNode)
                                    Dim var = to_let_linenum(Nothing, ifexpr)
                                    Dim if_ = CreateIfNode(insert_let(ifexpr.Condition), to_block(var, ifexpr.Then), to_block(var, ifexpr.Else))
                                    block.Statements.Insert(program_pointer, if_)
                                    user.VarIndex += 1
                                    Coverage.Case()
                                    Return var

                                ElseIf IsGeneric(e.GetType, GetType(ListNode(Of ))) Then

                                    Dim list = e.GetType.GetProperty("List").GetValue(e)
                                    Dim count = list.GetType.GetProperty("Count")
                                    Dim item = list.GetType.GetProperty("Item")
                                    For i = 0 To CInt(count.GetValue(list)) - 1

                                        Dim index = New Object() {i}
                                        Dim x = CType(item.GetValue(list, index), IEvaluableNode)
                                        item.SetValue(list, insert_let(x), index)
                                    Next
                                    Coverage.Case()
                                    Return to_let(e)

                                ElseIf TypeOf e Is VariableNode Then

                                    Coverage.Case()
                                End If

                                Return e
                            End Function

                        Dim to_flat As Func(Of IEvaluableNode, IEvaluableNode) =
                            Function(e)

                                If TypeOf e Is ExpressionNode Then

                                    Dim expr = CType(e, ExpressionNode)
                                    expr.Left = insert_let(expr.Left)
                                    expr.Right = insert_let(expr.Right)
                                    'Coverage.Case()

                                ElseIf TypeOf e Is PropertyNode Then

                                    Dim prop = CType(e, PropertyNode)
                                    prop.Left = insert_let(prop.Left)
                                    Coverage.Case()

                                ElseIf TypeOf e Is FunctionCallNode Then

                                    Dim func = CType(e, FunctionCallNode)
                                    func.Expression = insert_let(func.Expression)
                                    For i = 0 To func.Arguments.Length - 1

                                        func.Arguments(i) = insert_let(func.Arguments(i))
                                    Next
                                    Coverage.Case()

                                ElseIf TypeOf e Is TupleNode Then

                                    Dim tuple = CType(e, TupleNode)
                                    For i = 0 To tuple.Items.Length - 1

                                        tuple.Items(i) = insert_let(tuple.Items(i))
                                    Next
                                    Coverage.Case()

                                ElseIf TypeOf e Is IfExpressionNode Then

                                    insert_let(e)
                                    Coverage.Case()
                                    Return Nothing

                                ElseIf TypeOf e Is VariableNode Then

                                    Coverage.Case()
                                End If

                                Return e
                            End Function

                        Do While program_pointer < block.Statements.Count

                            Dim v = block.Statements(program_pointer)
                            If TypeOf v Is FunctionCallNode Then

                                Dim fcall = CType(v, FunctionCallNode)
                                If TypeOf fcall.Expression Is VariableNode AndAlso CType(fcall.Expression, VariableNode).Name.Equals("yield") Then

                                    block.Owner.Coroutine = True
                                    Coverage.Case()
                                End If
                                to_flat(fcall)
                                Coverage.Case()

                            ElseIf TypeOf v Is LambdaExpressionNode Then

                                Dim lambda = CType(v, LambdaExpressionNode)
                                lambda.Expression = to_flat(lambda.Expression)
                                Coverage.Case()

                            ElseIf TypeOf v Is LetNode Then

                                Dim let_ = CType(v, LetNode)
                                let_.Receiver = to_flat(let_.Receiver)
                                let_.Expression = to_flat(let_.Expression)
                                Coverage.Case()

                            ElseIf TypeOf v Is IfNode Then

                                Dim if_ = CType(v, IfNode)
                                if_.Condition = insert_let(if_.Condition)
                                Coverage.Case()

                            ElseIf TypeOf v Is SwitchNode Then

                                Dim switch = CType(v, SwitchNode)
                                switch.Expression = insert_let(switch.Expression)
                                Coverage.Case()

                            End If
                            program_pointer += 1
                        Loop
                    End If

                    next_(child, user)
                End Sub)
        End Sub

    End Class

End Namespace

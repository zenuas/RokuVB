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

                        Dim to_flat As Func(Of Boolean, IEvaluableNode, IEvaluableNode) =
                            Function(isnewlet, e)

                                If TypeOf e Is ExpressionNode Then

                                    Dim expr = CType(e, ExpressionNode)
                                    If expr.Operator.Equals("()") Then Return to_flat(isnewlet, expr.Left)

                                    expr.Left = to_flat(True, expr.Left)
                                    expr.Right = to_flat(True, expr.Right)
                                    Coverage.Case()
                                    If isnewlet Then Return to_let(e)

                                ElseIf TypeOf e Is PropertyNode Then

                                    Dim prop = CType(e, PropertyNode)
                                    prop.Left = to_flat(True, prop.Left)
                                    Coverage.Case()
                                    If isnewlet Then Return to_let(e)

                                ElseIf TypeOf e Is FunctionCallNode Then

                                    Dim func = CType(e, FunctionCallNode)
                                    func.Expression = to_flat(True, func.Expression)
                                    For i = 0 To func.Arguments.Length - 1

                                        func.Arguments(i) = to_flat(True, func.Arguments(i))
                                    Next
                                    Coverage.Case()
                                    If isnewlet Then Return to_let(e)

                                ElseIf TypeOf e Is TupleNode Then

                                    Dim tuple = CType(e, TupleNode)
                                    For i = 0 To tuple.Items.Length - 1

                                        tuple.Items(i) = to_flat(True, tuple.Items(i))
                                    Next
                                    Coverage.Case()
                                    If isnewlet Then Return to_let(e)

                                ElseIf TypeOf e Is IfExpressionNode Then

                                    Dim ifexpr = CType(e, IfExpressionNode)
                                    Dim var = to_let_linenum(Nothing, ifexpr)
                                    Dim if_ = CreateIfNode(to_flat(True, ifexpr.Condition), to_block(var, ifexpr.Then), to_block(var, ifexpr.Else))
                                    block.Statements.Insert(program_pointer, if_)
                                    user.VarIndex += 1
                                    Coverage.Case()
                                    Return var

                                ElseIf e IsNot Nothing AndAlso IsGeneric(e.GetType, GetType(ListNode(Of ))) Then

                                    Dim list = e.GetType.GetProperty("List").GetValue(e)
                                    Dim count = list.GetType.GetProperty("Count")
                                    Dim item = list.GetType.GetProperty("Item")
                                    For i = 0 To CInt(count.GetValue(list)) - 1

                                        Dim index = New Object() {i}
                                        Dim x = CType(item.GetValue(list, index), IEvaluableNode)
                                        item.SetValue(list, to_flat(True, x), index)
                                    Next
                                    Coverage.Case()
                                    If isnewlet Then Return to_let(e)

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
                                to_flat(False, fcall)
                                Coverage.Case()

                            ElseIf TypeOf v Is LambdaExpressionNode Then

                                Dim lambda = CType(v, LambdaExpressionNode)
                                lambda.Expression = to_flat(False, lambda.Expression)
                                Coverage.Case()

                            ElseIf TypeOf v Is LetNode Then

                                Dim let_ = CType(v, LetNode)
                                let_.Receiver = to_flat(True, let_.Receiver)
                                let_.Expression = to_flat(False, let_.Expression)
                                Coverage.Case()

                            ElseIf TypeOf v Is IfNode Then

                                Dim if_ = CType(v, IfNode)
                                if_.Condition = to_flat(True, if_.Condition)
                                Coverage.Case()

                            ElseIf TypeOf v Is SwitchNode Then

                                Dim switch = CType(v, SwitchNode)
                                switch.Expression = to_flat(True, switch.Expression)
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

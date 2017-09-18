Imports System
Imports Roku.Node
Imports Roku.Util
Imports Roku.Util.TypeHelper


Namespace Compiler

    Public Class Normalize

        Public Shared Sub Normalization(node As ProgramNode)

            Util.Traverse.NodesOnce(
                node,
                CType(node, BlockNode),
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        Dim var_index = 0
                        Dim program_pointer = 0

                        Dim to_let =
                            Function(e As IEvaluableNode)

                                Dim var As New VariableNode($"${var_index}") With {.Scope = block}
                                var.LineNumber = e.LineNumber
                                var.LineColumn = e.LineColumn
                                var_index += 1

                                Dim let_ As New LetNode With {.Var = var, .Expression = e}
                                block.Statements.Insert(program_pointer, let_)
                                program_pointer += 1
                                Coverage.Case()
                                Return var
                            End Function

                        Dim insert_let As Func(Of IEvaluableNode, IEvaluableNode) =
                            Function(e As IEvaluableNode) As IEvaluableNode

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

                                ElseIf IsGeneric(e.GetType, GetType(ListNode(Of ))) Then

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

                                ElseIf TypeOf e Is VariableNode Then

                                    Coverage.Case()
                                End If

                                Return e
                            End Function

                        Do While program_pointer < block.Statements.Count

                            Dim v = block.Statements(program_pointer)
                            If TypeOf v Is FunctionCallNode Then

                                to_flat(CType(v, FunctionCallNode))
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

                        next_(block, block)
                    Else
                        next_(child, user)
                    End If
                End Sub)
        End Sub

    End Class

End Namespace

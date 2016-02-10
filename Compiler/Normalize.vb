Imports System
Imports Roku.Node


Namespace Compiler

    Public Class Normalize

        Public Shared Sub Normalization(node As INode)

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
                                Return var
                            End Function

                        Dim insert_let As Func(Of IEvaluableNode, IEvaluableNode) =
                            Function(e As IEvaluableNode) As IEvaluableNode

                                If TypeOf e Is ExpressionNode Then

                                    Dim expr = CType(e, ExpressionNode)
                                    If expr.Operator.Equals("()") Then Return insert_let(expr.Left)

                                    expr.Left = insert_let(expr.Left)
                                    expr.Right = insert_let(expr.Right)
                                    Return to_let(expr)

                                ElseIf TypeOf e Is PropertyNode Then

                                    Dim prop = CType(e, PropertyNode)
                                    prop.Left = insert_let(prop.Left)
                                    Return to_let(prop)

                                ElseIf TypeOf e Is FunctionCallNode Then

                                    Dim call_ = CType(e, FunctionCallNode)
                                    For i = 0 To call_.Arguments.Length - 1

                                        call_.Arguments(i) = insert_let(call_.Arguments(i))
                                    Next
                                    Return to_let(call_)

                                ElseIf TypeOf e Is LetNode Then

                                    Dim let_ = CType(e, LetNode)
                                    let_.Expression = insert_let(let_.Expression)
                                    Return let_

                                ElseIf TypeOf e Is VariableNode Then

                                End If

                                Return e
                            End Function

                        Dim to_flat As Func(Of IEvaluableNode, IEvaluableNode) =
                            Function(e As IEvaluableNode) As IEvaluableNode

                                If TypeOf e Is ExpressionNode Then

                                    Dim expr = CType(e, ExpressionNode)
                                    expr.Left = insert_let(expr.Left)
                                    expr.Right = insert_let(expr.Right)

                                ElseIf TypeOf e Is PropertyNode Then

                                    Dim prop = CType(e, PropertyNode)
                                    prop.Left = insert_let(prop.Left)

                                ElseIf TypeOf e Is FunctionCallNode Then

                                ElseIf TypeOf e Is VariableNode Then

                                End If

                                Return e
                            End Function

                        Do While program_pointer < block.Statements.Count

                            Dim v = block.Statements(program_pointer)
                            If TypeOf v Is ExpressionNode Then

                                'Dim expr = CType(v, ExpressionNode)
                                'expr.Left = to_flat(expr.Left)
                                'expr.Right = to_flat(expr.Right)
                                to_flat(v)

                            ElseIf TypeOf v Is PropertyNode Then

                                to_flat(v)

                            ElseIf TypeOf v Is FunctionCallNode Then

                                Dim func = CType(v, FunctionCallNode)
                                func.Expression = insert_let(func.Expression)
                                For i = 0 To func.Arguments.Length - 1

                                    func.Arguments(i) = insert_let(func.Arguments(i))
                                Next

                            ElseIf TypeOf v Is LetNode Then

                                Dim let_ = CType(v, LetNode)
                                let_.Expression = to_flat(let_.Expression)

                            ElseIf TypeOf v Is IfNode Then

                                Dim if_ = CType(v, IfNode)
                                if_.Condition = insert_let(if_.Condition)
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

Imports System
Imports Roku.Node


Namespace Compiler

    Public Class Normalize

        Public Shared Sub Normalization(node As INode)

            Dim block_search As Action(Of INode) =
                Sub(x As INode)

                    If Not TypeOf x Is BlockNode Then Return

                    Util.Traverse.Nodes(x,
                        Function(parent, ref, child, isfirst)

                            If Not isfirst Then Return isfirst

                            If TypeOf child Is BlockNode Then

                                Dim block = CType(child, BlockNode)
                                Dim var_index = 0
                                Dim i = 0

                                Dim to_let =
                                    Function(e As IEvaluableNode)

                                        Dim var As New VariableNode(String.Format("${0}", var_index))
                                        var.LineNumber = e.LineNumber
                                        var.LineColumn = e.LineColumn
                                        var_index += 1

                                        Dim let_ As New LetNode With {.Var = var, .Expression = e}
                                        block.Statements.Insert(i, let_)
                                        i += 1
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

                                        ElseIf TypeOf e Is FunctionCallNode Then

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

                                        ElseIf TypeOf e Is FunctionCallNode Then

                                        ElseIf TypeOf e Is VariableNode Then

                                        End If

                                        Return e
                                    End Function

                                Do While i < block.Statements.Count

                                    Dim v = block.Statements(i)
                                    If TypeOf v Is ExpressionNode Then

                                        'Dim expr = CType(v, ExpressionNode)
                                        'expr.Left = to_flat(expr.Left)
                                        'expr.Right = to_flat(expr.Right)
                                        to_flat(v)

                                    ElseIf TypeOf v Is FunctionCallNode Then

                                        Dim func = CType(v, FunctionCallNode)
                                        func.Expression = insert_let(func.Expression)
                                        For j = 0 To func.Arguments.Length - 1

                                            func.Arguments(j) = insert_let(func.Arguments(j))
                                        Next

                                    ElseIf TypeOf v Is LetNode Then

                                        Dim let_ = CType(v, LetNode)
                                        let_.Expression = to_flat(let_.Expression)

                                    ElseIf TypeOf v Is IfNode Then

                                        Dim if_ = CType(v, IfNode)
                                        if_.Condition = insert_let(if_.Condition)
                                        block_search(if_.Then)
                                        block_search(if_.Else)
                                    Else
                                        block_search(v)
                                    End If
                                    i += 1
                                Loop

                                Return False
                            End If

                            Return isfirst
                        End Function)
                End Sub

            block_search(node)
        End Sub

    End Class

End Namespace

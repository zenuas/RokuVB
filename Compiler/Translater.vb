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

                    For Each stmt In node_func.Body.Statements

                        If stmt.Type Is Nothing Then

                            Continue For
                        End If

                        If TypeOf stmt Is ExpressionNode Then

                            Dim expr = CType(stmt, ExpressionNode)
                            'rk_func.Body.Add(New RkCall With {.Return =expr.})

                        ElseIf TypeOf stmt Is FunctionCallNode Then

                            Dim func = CType(stmt, FunctionCallNode)

                        ElseIf TypeOf stmt Is LetNode Then

                            Dim let_ = CType(stmt, LetNode)
                            rk_func.Body.Add(New RkCall With {.Return = let_.Var})

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

Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Typing

        Public Shared Sub TypeInference(node As INode, root As RkNamespace)

            ' create prototype
            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = New RkStruct With {.Name = node_struct.Name}
                        node_struct.Type = rk_struct
                        current.Local.Add(rk_struct.Name, rk_struct)

                        For Each x In node_struct.Scope.Values

                            If TypeOf x Is LetNode Then

                                Dim let_ = CType(x, LetNode)
                                rk_struct.Local.Add(let_.Var.Name, let_.Type)
                            End If
                        Next

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name}
                        node_func.Type = rk_func
                        current.Local.Add(rk_func.Name, rk_func)
                    End If

                    next_(child, current)
                End Sub)

            ' typing
            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    next_(child, current)

                    If TypeOf child Is NumericNode Then

                        Dim node_num = CType(child, NumericNode)
                        node_num.Type = root.LoadLibrary("Int32")

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node_type = CType(child, TypeNode)
                        node_type.Type = root.LoadLibrary(node_type.Name)

                    ElseIf TypeOf child Is LetNode Then

                        Dim node_let = CType(child, LetNode)
                        node_let.Type = If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)
                        node_let.Var.Type = node_let.Type

                    ElseIf TypeOf child Is ExpressionNode Then

                        Dim node_expr = CType(child, ExpressionNode)
                        node_expr.Type = CType(node_expr.Left.Type.GetValue(node_expr.Operator), RkFunction).Return

                    End If
                End Sub)

        End Sub

    End Class

End Namespace

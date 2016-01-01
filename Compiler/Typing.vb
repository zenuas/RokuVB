Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Typing

        Public Shared Sub Prototype(node As INode, root As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

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
        End Sub

        Public Shared Sub TypeInference(node As INode, root As RkNamespace)

            Do While True

                Dim type_fix = False

                Dim set_type =
                    Function(n As IEvaluableNode, f As Func(Of IType))

                        If n.Type IsNot Nothing Then Return False

                        n.Type = f()
                        If n.Type IsNot Nothing Then type_fix = True
                        Return True
                    End Function

                Util.Traverse.NodesOnce(
                    node,
                    root,
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        next_(child, current)

                        If TypeOf child Is NumericNode Then

                            Dim node_num = CType(child, NumericNode)
                            set_type(node_num, Function() root.LoadLibrary("Int32"))

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node_type = CType(child, TypeNode)
                            If node_type.IsGeneric Then Return
                            set_type(node_type, Function() root.LoadLibrary(node_type.Name))

                        ElseIf TypeOf child Is LetNode Then

                            Dim node_let = CType(child, LetNode)
                            If set_type(node_let, Function() If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)) Then

                                node_let.Var.Type = node_let.Type
                            End If

                        ElseIf TypeOf child Is ExpressionNode Then

                            Dim node_expr = CType(child, ExpressionNode)
                            set_type(node_expr, Function() current.GetFunction(node_expr.Operator, node_expr.Left.Type, node_expr.Right.Type).Return)

                        ElseIf TypeOf child Is DeclareNode Then

                            Dim node_declare = CType(child, DeclareNode)
                            set_type(node_declare.Name, Function() node_declare.Type.Type)

                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop

        End Sub

    End Class

End Namespace

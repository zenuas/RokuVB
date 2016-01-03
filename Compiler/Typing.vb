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
                        rk_func.Arguments.AddRange(Util.Functions.Map(node_func.Arguments, Function(x) New NamedValue With {.Name = x.Name.Name, .Value = If(x.Type.IsGeneric, rk_func.DefineGeneric(x.Type.Name), Nothing)}))
                        If node_func.Return?.IsGeneric Then rk_func.Return = rk_func.DefineGeneric(node_func.Return.Name)
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

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node_call = CType(child, FunctionCallNode)
                            Dim rk_function As RkFunction = Nothing

                            If TypeOf node_call.Expression Is FunctionNode Then rk_function = CType(CType(node_call.Expression, FunctionNode).Type, RkFunction)

                            If node_call.Function Is Nothing AndAlso
                                node_call.Expression.Type IsNot Nothing Then

                                node_call.Function = rk_function
                                type_fix = True
                            End If
                            'set_type(node_call, Function() current.GetFunction(node_call.Expression, node_call.Arguments).Return)

                            'If node_call.Type IsNot Nothing AndAlso node_call.Then Then


                            'End If

                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop

        End Sub

    End Class

End Namespace

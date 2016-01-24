Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


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
                        Dim rk_struct = New RkStruct With {.Name = node_struct.Name, .StructNode = node_struct}
                        node_struct.Type = rk_struct
                        current.AddStruct(rk_struct)

                        For Each x In node_struct.Scope.Values

                            If TypeOf x Is LetNode Then

                                Dim let_ = CType(x, LetNode)
                                rk_struct.Local.Add(let_.Var.Name, let_.Type)
                            End If
                        Next

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name, .FunctionNode = node_func}
                        node_func.Type = rk_func
                        rk_func.Arguments.AddRange(node_func.Arguments.Map(Function(x) New NamedValue With {.Name = x.Name.Name, .Value = If(x.Type.IsGeneric, rk_func.DefineGeneric(x.Type.Name), Nothing)}))
                        If node_func.Return?.IsGeneric Then rk_func.Return = rk_func.DefineGeneric(node_func.Return.Name)
                        current.AddFunction(rk_func)
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

                Dim set_func =
                    Function(node_func As FunctionNode) As RkFunction

                        Dim rk_function = node_func.Function
                        If Not rk_function.HasGeneric Then

                            For i = 0 To node_func.Arguments.Length - 1

                                rk_function.Arguments(i).Value = node_func.Arguments(i).Type.Type
                            Next
                            rk_function.Return = node_func.Return?.Type
                        End If

                        Return rk_function
                    End Function

                Dim get_generic =
                    Function(name As String, scope As INode)

                        If TypeOf scope Is FunctionNode Then

                            Return CType(scope, FunctionNode).Function.Generics.FindFirst(Function(x) name.Equals(x.Name))

                        ElseIf TypeOf scope Is StructNode Then

                            Return CType(scope, StructNode).Struct.Generics.FindFirst(Function(x) name.Equals(x.Name))
                        End If
                        Throw New Exception("generic not found")
                    End Function

                Util.Traverse.NodesOnce(
                    node,
                    New With {.Namespace = root, .Scope = CType(Nothing, INode)},
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        next_(child, If(TypeOf child Is FunctionNode OrElse TypeOf child Is StructNode, New With {.Namespace = current.Namespace, .Scope = child}, current))

                        If TypeOf child Is NumericNode Then

                            Dim node_num = CType(child, NumericNode)
                            set_type(node_num, Function() root.LoadLibrary("Int32"))

                        ElseIf TypeOf child Is StringNode Then

                            Dim node_str = CType(child, StringNode)
                            set_type(node_str, Function() root.LoadLibrary("String"))

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node_type = CType(child, TypeNode)
                            set_type(node_type, Function() If(node_type.IsGeneric, get_generic(node_type.Name, current.Scope), current.Namespace.LoadLibrary(node_type.Name)))

                        ElseIf TypeOf child Is LetNode Then

                            Dim node_let = CType(child, LetNode)
                            If set_type(node_let, Function() If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)) Then

                                node_let.Var.Type = node_let.Type
                            End If

                        ElseIf TypeOf child Is ExpressionNode Then

                            Dim node_expr = CType(child, ExpressionNode)
                            set_type(node_expr,
                                Function()

                                    If node_expr.Function Is Nothing Then node_expr.Function = current.Namespace.GetFunction(node_expr.Operator, node_expr.Left.Type, node_expr.Right.Type)
                                    Return node_expr.Function.Return
                                End Function)

                        ElseIf TypeOf child Is PropertyNode Then

                            Dim node_prop = CType(child, PropertyNode)
                            set_type(node_prop, Function() CType(node_prop.Left.Type, RkStruct).Local.FindFirst(Function(x) x.Key.Equals(node_prop.Right.Name)).Value)

                        ElseIf TypeOf child Is DeclareNode Then

                            Dim node_declare = CType(child, DeclareNode)
                            set_type(node_declare.Name, Function() node_declare.Type.Type)

                        ElseIf TypeOf child Is FunctionNode Then

                            set_func(CType(child, FunctionNode))

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node_call = CType(child, FunctionCallNode)
                            Dim rk_function As RkFunction = Nothing

                            If TypeOf node_call.Expression Is FunctionNode Then rk_function = set_func(CType(node_call.Expression, FunctionNode))
                            If TypeOf node_call.Expression Is VariableNode Then rk_function = current.Namespace.GetFunction(CType(node_call.Expression, VariableNode).Name, node_call.Arguments.Map(Function(x) x.Type).ToArray)
                            If TypeOf node_call.Expression Is StructNode Then rk_function = current.Namespace.GetFunction("#Alloc", node_call.Expression.Type)

                            If node_call.Function Is Nothing AndAlso
                                rk_function IsNot Nothing Then

                                If rk_function.HasGeneric Then

                                    rk_function = current.Namespace.GetFunction(rk_function.Name, node_call.Arguments.Map(Function(x) x.Type).ToArray)
                                End If
                                node_call.Function = rk_function
                                type_fix = True
                            End If

                        ElseIf TypeOf child Is StructNode Then

                            Dim node_struct = CType(child, StructNode)
                            Dim rk_struct = CType(node_struct.Type, RkStruct)

                            For Each s In node_struct.Scope.Where(Function(x) TypeOf x.Value Is LetNode)

                                rk_struct.Local(s.Key) = CType(s.Value, LetNode).Type
                            Next

                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop

        End Sub

    End Class

End Namespace

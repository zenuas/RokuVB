Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


Namespace Compiler

    Public Class Typing

        Public Shared Sub Prototype(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

            Dim closures As New Dictionary(Of IScopeNode, RkStruct)
            Dim make_closure =
                Function(scope As IScopeNode)

                    If closures.ContainsKey(scope) Then Return closures(scope)

                    Dim env As New RkStruct With {.Namespace = root, .ClosureEnvironment = True}
                    env.Name = $"##{CType(scope.Owner, FunctionNode).Name}"
                    For Each var In scope.Scope.Where(Function(v) TypeOf v.Value Is VariableNode AndAlso CType(v.Value, VariableNode).ClosureEnvironment)

                        env.AddLet(var.Key, Nothing)
                    Next
                    env.Initializer = CType(root.LoadFunction("#Alloc", env), RkNativeFunction)
                    closures.Add(scope, env)
                    root.AddStruct(env)
                    CType(scope.Owner, FunctionNode).Function.Closure = env
                    Return env
                End Function

            Util.Traverse.NodesOnce(
                node,
                ns,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = New RkStruct With {.Name = node_struct.Name, .StructNode = node_struct, .Namespace = current}
                        node_struct.Type = rk_struct
                        node_struct.Generics.Do(Sub(x) rk_struct.DefineGeneric(x.Name))
                        current.AddStruct(rk_struct)

                        For Each x In node_struct.Scope.Values

                            If TypeOf x Is LetNode Then

                                Dim let_ = CType(x, LetNode)
                                rk_struct.AddLet(let_.Var.Name, If(let_.Declare?.IsGeneric, rk_struct.Generics.FindFirst(Function(g) g.Name.Equals(let_.Declare.Name)), let_.Type))
                            End If
                        Next

                        If rk_struct.HasGeneric Then

                            Dim alloc = New RkNativeFunction With {.Name = "#Alloc", .Operator = RkOperator.Alloc, .Namespace = rk_struct.Namespace}
                            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = rk_struct})
                            node_struct.Generics.Do(Sub(x) alloc.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = alloc.DefineGeneric(x.Name)}))
                            alloc.Return = rk_struct.FixedGeneric(alloc.Arguments.Range(1).ToArray)
                            alloc.Namespace.AddFunction(alloc)
                        Else

                            rk_struct.Initializer = CType(root.LoadFunction("#Alloc", rk_struct), RkNativeFunction)
                        End If

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name, .FunctionNode = node_func, .Namespace = current}
                        node_func.Type = rk_func
                        rk_func.Arguments.AddRange(node_func.Arguments.Map(Function(x) New NamedValue With {.Name = x.Name.Name, .Value = If(x.Type.IsGeneric, rk_func.DefineGeneric(x.Type.Name), Nothing)}))
                        If node_func.Return?.IsGeneric Then rk_func.Return = rk_func.DefineGeneric(node_func.Return.Name)
                        node_func.Bind.Do(
                            Sub(x)

                                Dim env = make_closure(x.Key)
                                rk_func.Arguments.Insert(0, New NamedValue With {.Name = env.Name, .Value = env})
                            End Sub)
                        current.AddFunction(rk_func)
                    End If

                    next_(child, current)
                End Sub)
        End Sub

        Public Shared Sub TypeStatic(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                New With {.Namespace = ns, .Scope = CType(Nothing, INode), .Function = CType(Nothing, FunctionNode)},
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return
                    next_(child,
                        If(TypeOf child Is FunctionNode OrElse TypeOf child Is StructNode,
                            New With {.Namespace = current.Namespace, .Scope = child, .Function = If(TypeOf child Is FunctionNode, CType(child, FunctionNode), current.Function)},
                            current)
                        )

                    If TypeOf child Is NumericNode Then

                        Dim node_num = CType(child, NumericNode)
                        node_num.Type = root.LoadStruct("Int32")

                    ElseIf TypeOf child Is StringNode Then

                        Dim node_str = CType(child, StringNode)
                        node_str.Type = root.LoadStruct("String")

                    ElseIf TypeOf child Is TypeFunctionNode Then

                        Dim node_typef = CType(child, TypeFunctionNode)
                        Dim rk_func As New RkFunction With {.Namespace = current.Namespace}
                        node_typef.Arguments.Do(Sub(x) rk_func.Arguments.Add(New NamedValue With {.Value = x.Type}))
                        rk_func.Return = node_typef.Return?.Type
                        node_typef.Type = rk_func

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node_type = CType(child, TypeNode)
                        If Not node_type.IsGeneric Then node_type.Type = CType(current.Namespace.LoadStruct(node_type.Name), IType)

                    ElseIf TypeOf child Is DeclareNode Then

                        Dim node_declare = CType(child, DeclareNode)
                        node_declare.Name.Type = node_declare.Type.Type

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_function = node_func.Function
                        If Not rk_function.HasGeneric Then

                            For Each arg In node_func.Arguments

                                rk_function.Arguments.FindFirst(Function(x) x.Name.Equals(arg.Name.Name)).Value = arg.Type.Type
                            Next
                            rk_function.Return = node_func.Return?.Type
                        End If

                    End If
                End Sub)
        End Sub

        Public Shared Sub TypeInference(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

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

                            For Each arg In node_func.Arguments

                                rk_function.Arguments.FindFirst(Function(x) x.Name.Equals(arg.Name.Name)).Value = arg.Type.Type
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

                Dim get_struct =
                    Function(current As RkNamespace, n As IEvaluableNode)

                        If TypeOf n Is VariableNode Then Return current.LoadStruct(CType(n, VariableNode).Name)
                        Throw New Exception("struct not found")
                    End Function

                Dim get_closure =
                    Function(current As IScopeNode) As RkStruct

                        Return CType(current.Owner, FunctionNode).Function.Closure
                    End Function

                Util.Traverse.NodesOnce(
                    node,
                    New With {.Namespace = ns, .Scope = CType(Nothing, INode), .Function = CType(Nothing, FunctionNode)},
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        next_(child,
                            If(TypeOf child Is FunctionNode OrElse TypeOf child Is StructNode,
                                New With {.Namespace = current.Namespace, .Scope = child, .Function = If(TypeOf child Is FunctionNode, CType(child, FunctionNode), current.Function)},
                                current)
                            )

                        If TypeOf child Is VariableNode Then

                            If Not (TypeOf parent Is LetNode AndAlso ref.Equals("Var")) Then

                                Dim node_var = CType(child, VariableNode)
                                set_type(node_var,
                                    Function()

                                        If current.Function IsNot Nothing Then

                                            Dim v = current.Function.Arguments.FindFirstOrNull(Function(x) x.Name.Name.Equals(node_var.Name))
                                            If v IsNot Nothing Then Return v.Type.Type
                                        End If
                                        Return New RkByName With {.Namespace = current.Namespace, .Name = node_var.Name}
                                    End Function)
                            End If

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node_type = CType(child, TypeNode)
                            If node_type.IsGeneric Then set_type(node_type, Function() get_generic(node_type.Name, current.Scope))

                        ElseIf TypeOf child Is LetNode Then

                            Dim node_let = CType(child, LetNode)
                            If set_type(node_let, Function() If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)) Then

                                node_let.Var.Type = node_let.Type
                                If node_let.Var.ClosureEnvironment Then

                                    get_closure(node_let.Var.Scope).Local(node_let.Var.Name) = node_let.Type
                                End If
                            End If

                        ElseIf TypeOf child Is ExpressionNode Then

                            Dim node_expr = CType(child, ExpressionNode)
                            set_type(node_expr,
                                Function()

                                    If node_expr.Function Is Nothing Then node_expr.Function = current.Namespace.LoadFunction(node_expr.Operator, node_expr.Left.Type, node_expr.Right.Type)
                                    Return node_expr.Function.Return
                                End Function)

                        ElseIf TypeOf child Is PropertyNode Then

                            Dim node_prop = CType(child, PropertyNode)
                            set_type(node_prop, Function() CType(node_prop.Left.Type, RkStruct).Local.FindFirst(Function(x) x.Key.Equals(node_prop.Right.Name)).Value)

                        ElseIf TypeOf child Is DeclareNode Then

                            Dim node_declare = CType(child, DeclareNode)
                            If node_declare.Name.ClosureEnvironment Then get_closure(node_declare.Name.Scope).Local(node_declare.Name.Name) = node_declare.Type.Type

                        ElseIf TypeOf child Is FunctionNode Then

                            set_func(CType(child, FunctionNode))

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node_call = CType(child, FunctionCallNode)
                            Dim rk_function As RkFunction = Nothing

                            If TypeOf node_call.Expression Is FunctionNode Then

                                rk_function = set_func(CType(node_call.Expression, FunctionNode))

                            ElseIf TypeOf node_call.Expression Is VariableNode Then

                                If TypeOf node_call.Expression.Type Is RkFunction Then

                                    rk_function = CType(node_call.Expression.Type, RkFunction)

                                ElseIf TypeOf node_call.Expression.Type Is RkByName Then

                                    rk_function = current.Namespace.LoadFunction(CType(node_call.Expression.Type, RkByName).Name, node_call.Arguments.Map(Function(x) x.Type).ToArray)
                                    node_call.Expression.Type = rk_function
                                End If

                            ElseIf TypeOf node_call.Expression Is StructNode Then

                                Dim node_struct = CType(node_call.Expression, StructNode)
                                Dim args = {node_call.Expression.Type}.ToList
                                If node_struct.Struct.HasGeneric Then args.AddRange(node_call.Arguments.Map(Function(x) get_struct(current.Namespace, x)).ToArray)
                                rk_function = root.LoadFunction("#Alloc", args.ToArray)
                            End If

                            If node_call.Function Is Nothing AndAlso
                                rk_function IsNot Nothing Then

                                If rk_function.HasGeneric Then

                                    rk_function = current.Namespace.LoadFunction(rk_function.Name, node_call.Arguments.Map(Function(x) x.Type).ToArray)
                                End If
                                node_call.Function = rk_function
                                type_fix = True
                            End If

                        ElseIf TypeOf child Is StructNode Then

                            Dim node_struct = CType(child, StructNode)
                            Dim rk_struct = CType(node_struct.Type, RkStruct)

                            For Each s In node_struct.Scope.Where(Function(x) TypeOf x.Value Is LetNode)

                                Dim t = CType(s.Value, LetNode).Type
                                If rk_struct.Local(s.Key) Is Nothing Then

                                    rk_struct.Local(s.Key) = t
                                    If rk_struct.Local(s.Key) IsNot Nothing Then type_fix = True
                                End If
                                If TypeOf t IsNot RkGenericEntry Then

                                    For Each fix In rk_struct.Namespace.Structs(rk_struct.Name)

                                        If fix.Local(s.Key) Is Nothing Then

                                            fix.Local(s.Key) = t
                                            If fix.Local(s.Key) IsNot Nothing Then type_fix = True
                                        End If
                                    Next
                                End If
                            Next

                        ElseIf child.GetType.Name.Equals("ListNode`1") Then

                            set_type(CType(child, IEvaluableNode),
                                Function()

                                    Dim list = child.GetType.GetProperty("List").GetValue(child)
                                    Dim count = list.GetType.GetProperty("Count")
                                    If CInt(count.GetValue(list)) = 0 Then Debug.Fail("not yet")

                                    Dim item = list.GetType.GetProperty("Item")
                                    Return root.LoadStruct("Array", CType(item.GetValue(list, New Object() {0}), IEvaluableNode).Type)
                                End Function)
                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop

        End Sub

    End Class

End Namespace

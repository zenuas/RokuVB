Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Reflection
Imports Roku.Node
Imports Roku.Manager
Imports Roku.IntermediateCode
Imports Roku.Util
Imports Roku.Util.ArrayExtension
Imports Roku.Util.TypeHelper


Namespace Compiler

    Public Class Typing

        Public Shared Sub Prototype(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

            Dim closures As New Dictionary(Of IScopeNode, RkStruct)
            Dim make_closure =
                Function(scope As IScopeNode)

                    If closures.ContainsKey(scope) Then Return closures(scope)

                    Dim env As New RkStruct With {.Namespace = root, .ClosureEnvironment = True}
                    env.Name = $"##{scope.Owner.Name}"
                    For Each var In scope.Scope.Where(Function(v) TypeOf v.Value Is VariableNode AndAlso CType(v.Value, VariableNode).ClosureEnvironment)

                        env.AddLet(var.Key, Nothing)
                    Next
                    env.Initializer = CType(root.LoadFunction("#Alloc", env), RkNativeFunction)
                    closures.Add(scope, env)
                    root.AddStruct(env)
                    scope.Owner.Function.Closure = env
                    Coverage.Case()
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
                                Coverage.Case()
                            End If
                        Next

                        If rk_struct.HasGeneric Then

                            Dim alloc = New RkNativeFunction With {.Name = "#Alloc", .Operator = InOperator.Alloc, .Namespace = rk_struct.Namespace}
                            Dim gens = node_struct.Generics.Map(Function(x) alloc.DefineGeneric(x.Name)).ToArray
                            Dim self = rk_struct.FixedGeneric(gens)
                            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = self})
                            gens.Do(Sub(x) alloc.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = x}))
                            alloc.Return = self
                            alloc.Namespace.AddFunction(alloc)
                            Coverage.Case()
                        Else

                            'rk_struct.Initializer = CType(root.LoadFunction("#Alloc", rk_struct), RkNativeFunction)
                            Coverage.Case()
                        End If

                    ElseIf TypeOf child Is ProgramNode Then

                        Dim node_pgm = CType(child, ProgramNode)
                        Dim ctor As New RkFunction With {.Name = node_pgm.Name, .FunctionNode = New FunctionNode("") With {.Body = CType(node, BlockNode)}, .Namespace = current}
                        node_pgm.Function = ctor
                        node_pgm.Owner = node_pgm
                        current.AddFunction(ctor)

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
                                Coverage.Case()
                            End Sub)
                        current.AddFunction(rk_func)
                        Coverage.Case()
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
                        Coverage.Case()

                    ElseIf TypeOf child Is StringNode Then

                        Dim node_str = CType(child, StringNode)
                        node_str.Type = root.LoadStruct("String")
                        Coverage.Case()

                    ElseIf TypeOf child Is NullNode Then

                        Dim node_null = CType(child, NullNode)
                        node_null.Type = root.LoadType(GetType(Object).GetTypeInfo)
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeFunctionNode Then

                        Dim node_typef = CType(child, TypeFunctionNode)
                        Dim rk_func As New RkFunction With {.Namespace = current.Namespace}
                        node_typef.Arguments.Do(Sub(x) rk_func.Arguments.Add(New NamedValue With {.Value = x.Type}))
                        rk_func.Return = node_typef.Return?.Type
                        node_typef.Type = rk_func
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node_type = CType(child, TypeNode)
                        If Not node_type.IsGeneric Then node_type.Type = CType(current.Namespace.LoadStruct(node_type.Name), IType)
                        Coverage.Case()

                    ElseIf TypeOf child Is DeclareNode Then

                        Dim node_declare = CType(child, DeclareNode)
                        node_declare.Name.Type = node_declare.Type.Type
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_function = node_func.Function
                        If Not rk_function.HasGeneric Then

                            For Each arg In node_func.Arguments

                                rk_function.Arguments.FindFirst(Function(x) x.Name.Equals(arg.Name.Name)).Value = arg.Type.Type
                            Next
                            rk_function.Return = node_func.Return?.Type
                        End If
                        Coverage.Case()

                    End If
                End Sub)
        End Sub

        Public Shared Sub TypeInference(node As ProgramNode, root As SystemLirary, ns As RkNamespace)

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

                        Coverage.Case()
                        Return CType(scope, FunctionNode).Function.Generics.FindFirst(Function(x) name.Equals(x.Name))

                    ElseIf TypeOf scope Is StructNode Then

                        Coverage.Case()
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

                    Return current.Owner.Function.Closure
                End Function

            Dim node_deep_copy =
                Function(n As INode)

                    Dim cache As New Dictionary(Of INode, INode)
                    Dim copy As Func(Of INode, INode) =
                        Function(v)

                            If cache.ContainsKey(v) Then Return cache(v)
                            Dim clone = v.Clone
                            cache(v) = clone

                            For Each p In Util.Traverse.Fields(clone)

                                If TypeOf p.Item1 Is INode Then

                                    p.Item2.SetValue(clone, copy(CType(p.Item1, INode)))
                                Else

                                    Dim t = p.Item2.FieldType
                                    If t.IsArray AndAlso IsInterface(t.GetElementType, GetType(INode)) Then

                                        Dim arr = CType(CType(p.Item1, Array).Clone, Array)
                                        For i = 0 To arr.Length - 1

                                            arr.SetValue(copy(CType(arr.GetValue(i), INode)), i)
                                        Next
                                        p.Item2.SetValue(clone, arr)

                                    ElseIf IsGeneric(t, GetType(List(Of ))) AndAlso IsInterface(t.GenericTypeArguments(0), GetType(INode)) Then

                                        Dim base = CType(p.Item1, System.Collections.IList)
                                        Dim arr = CType(Activator.CreateInstance(GetType(List(Of )).MakeGenericType(t.GenericTypeArguments(0))), System.Collections.IList)
                                        For i = 0 To base.Count - 1

                                            arr.Add(copy(CType(base(i), INode)))
                                        Next
                                        p.Item2.SetValue(clone, arr)
                                    End If
                                End If
                            Next

                            Return clone
                        End Function

                    Return copy(n)
                End Function

            Dim function_generic_fixed_to_node =
                Function(f As RkFunction)

                    Dim base = f.GenericBase.FunctionNode
                    Dim clone = CType(node_deep_copy(base), FunctionNode)

                    For i = 0 To clone.Arguments.Length - 1

                        clone.Arguments(i).Type.Type = f.Arguments(base.Bind.Count + i).Value
                    Next
                    If clone.Return IsNot Nothing Then clone.Return.Type = f.Return
                    clone.Type = f
                    f.FunctionNode = clone

                    Return clone
                End Function

            Do While True

                Dim type_fix = False

                Dim set_type =
                    Function(n As IEvaluableNode, f As Func(Of IType))

                        If n.Type IsNot Nothing Then Return False

                        n.Type = f()
                        If n.Type IsNot Nothing Then

                            type_fix = True
                            Return True
                        Else

                            Coverage.Case()
                            Return False
                        End If
                    End Function

                Util.Traverse.NodesOnce(
                    node,
                    New With {.Namespace = ns, .Scope = CType(Nothing, INode), .Function = CType(Nothing, FunctionNode)},
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        If TypeOf child Is FunctionNode AndAlso CType(child, FunctionNode).Function.HasGeneric Then Return

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

                                            Coverage.Case()
                                            Dim v = current.Function.Arguments.FindFirstOrNull(Function(x) x.Name.Name.Equals(node_var.Name))
                                            If v IsNot Nothing Then Return v.Type.Type

                                        ElseIf Not (TypeOf parent Is PropertyNode AndAlso ref.Equals("Right")) Then

                                            Coverage.Case()
                                            Dim t = current.Namespace.TryLoadStruct(node_var.Name)
                                            If t IsNot Nothing Then Return New RkLateBind With {.Value = t}

                                            Coverage.Case()
                                            Dim f = current.Namespace.TryLoadFunction(node_var.Name)
                                            If f IsNot Nothing Then Return f

                                            Coverage.Case()
                                            Dim n = current.Namespace.TryLoadNamespace(node_var.Name)
                                            If n IsNot Nothing Then Return n
                                        End If

                                        Coverage.Case()
                                        Return New RkByName With {.Namespace = current.Namespace, .Name = node_var.Name}
                                    End Function)
                            End If

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node_type = CType(child, TypeNode)
                            If node_type.IsGeneric Then set_type(node_type, Function() get_generic(node_type.Name, current.Scope))
                            Coverage.Case()

                        ElseIf TypeOf child Is LetNode Then

                            Dim node_let = CType(child, LetNode)
                            If set_type(node_let, Function() If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)) Then

                                node_let.Var.Type = node_let.Type
                                If node_let.Var.ClosureEnvironment Then

                                    get_closure(node_let.Var.Scope).Local(node_let.Var.Name) = node_let.Type
                                    Coverage.Case()
                                End If
                                Coverage.Case()
                            End If

                        ElseIf TypeOf child Is ExpressionNode Then

                            Dim node_expr = CType(child, ExpressionNode)
                            set_type(node_expr,
                                Function()

                                    If node_expr.Function Is Nothing Then node_expr.Function = current.Namespace.LoadFunction(node_expr.Operator, node_expr.Left.Type, node_expr.Right.Type)
                                    Coverage.Case()
                                    Return node_expr.Function.Return
                                End Function)

                        ElseIf TypeOf child Is PropertyNode Then

                            Dim node_prop = CType(child, PropertyNode)
                            If TypeOf node_prop.Left.Type Is RkStruct Then

                                If set_type(node_prop,
                                    Function()

                                        Coverage.Case()
                                        Dim struct = CType(node_prop.Left.Type, RkStruct)
                                        Dim v = struct.Local.FindFirstOrNull(Function(x) x.Key.Equals(node_prop.Right.Name)).Value
                                        If v IsNot Nothing Then Return v

                                        ' method call syntax sugar
                                        Coverage.Case()
                                        Return New RkByNameWithReceiver With {.Namespace = struct.Namespace?.TryGetNamespace(struct.Name), .Name = node_prop.Right.Name, .Receiver = node_prop.Left}
                                    End Function) Then

                                    node_prop.Right.Type = node_prop.Type
                                End If

                            ElseIf TypeOf node_prop.Left.Type Is RkLateBind AndAlso TypeOf CType(node_prop.Left.Type, RkLateBind).Value Is RkStruct Then

                                If set_type(node_prop,
                                    Function()

                                        Coverage.Case()
                                        Dim struct = CType(CType(node_prop.Left.Type, RkLateBind).Value, RkStruct)
                                        Dim v = struct.Local.FindFirstOrNull(Function(x) x.Key.Equals(node_prop.Right.Name)).Value
                                        If v IsNot Nothing Then

                                            Coverage.Case()
                                            node_prop.Left.Type = struct
                                            Return v
                                        End If

                                        ' DotNET static-function support
                                        Coverage.Case()
                                        Dim n = struct.Namespace?.TryGetNamespace(struct.Name)
                                        node_prop.Left.Type = n
                                        Return New RkByNameWithReceiver With {.Namespace = n, .Name = node_prop.Right.Name}
                                    End Function) Then

                                    node_prop.Right.Type = node_prop.Type
                                End If

                            ElseIf TypeOf node_prop.Left.Type Is RkNamespace Then

                                If set_type(node_prop,
                                    Function()

                                        Dim left = CType(node_prop.Left.Type, RkNamespace)
                                        Dim right = node_prop.Right.Name

                                        Coverage.Case()
                                        Dim t = left.TryGetStruct(right)
                                        If t IsNot Nothing Then Return New RkLateBind With {.Value = t}

                                        Coverage.Case()
                                        Dim f = left.TryGetFunction(right)
                                        If f IsNot Nothing Then Return f

                                        Coverage.Case()
                                        Dim n = left.TryGetNamespace(right)
                                        If n IsNot Nothing Then Return n

                                        Return Nothing
                                    End Function) Then

                                    node_prop.Right.Type = node_prop.Type
                                End If

                            ElseIf TypeOf node_prop.Left.Type Is RkByName OrElse
                                node_prop.Left.Type Is Nothing Then

                                ' nothing
                                set_type(node_prop, Function() New RkByNameWithReceiver With {.Namespace = Nothing, .Name = node_prop.Right.Name, .Receiver = node_prop.Left})
                                Coverage.Case()
                            Else

                                Debug.Fail("not yet")
                            End If

                        ElseIf TypeOf child Is DeclareNode Then

                            Dim node_declare = CType(child, DeclareNode)
                            If node_declare.Name.ClosureEnvironment Then get_closure(node_declare.Name.Scope).Local(node_declare.Name.Name) = node_declare.Type.Type
                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionNode Then

                            set_func(CType(child, FunctionNode))
                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node_call = CType(child, FunctionCallNode)
                            If node_call.Function Is Nothing Then

                                Dim rk_function As RkFunction = Nothing

                                If TypeOf node_call.Expression Is FunctionNode Then

                                    rk_function = set_func(CType(node_call.Expression, FunctionNode))
                                    Coverage.Case()

                                ElseIf TypeOf node_call.Expression.Type Is RkCILStruct Then

                                    Dim struct = CType(node_call.Expression.Type, RkCILStruct)
                                    Dim args = node_call.Arguments.Map(Function(x) x.Type).ToArray
                                    rk_function = struct.LoadConstructor(root, args)
                                    Coverage.Case()

                                ElseIf TypeOf node_call.Expression.Type Is RkLateBind AndAlso TypeOf CType(node_call.Expression.Type, RkLateBind).Value Is RkCILStruct Then

                                    Dim struct = CType(CType(node_call.Expression.Type, RkLateBind).Value, RkCILStruct)
                                    Dim args = node_call.Arguments.Map(Function(x) x.Type).ToArray
                                    rk_function = struct.LoadConstructor(root, args)
                                    node_call.Expression.Type = struct
                                    Coverage.Case()

                                ElseIf TypeOf node_call.Expression Is VariableNode Then

                                    If TypeOf node_call.Expression.Type Is RkFunction Then

                                        rk_function = CType(node_call.Expression.Type, RkFunction)
                                        Coverage.Case()

                                    ElseIf TypeOf node_call.Expression.Type Is RkByName Then

                                        Dim args = node_call.Arguments.Map(Function(x) x.Type).ToArray
                                        Dim receiver As IEvaluableNode = Nothing
                                        Dim name = CType(node_call.Expression.Type, RkByName).Name
                                        If TypeOf node_call.Expression.Type Is RkByNameWithReceiver Then

                                            Dim v = CType(node_call.Expression.Type, RkByNameWithReceiver)
                                            If TypeOf v.Receiver?.Type Is RkStruct Then

                                                receiver = v.Receiver
                                                'args.Insert(0, v.Receiver.Type)
                                                'node_call.Arguments = {v.Receiver}.Join(node_call.Arguments).ToArray
                                                Coverage.Case()

                                            ElseIf v.Name.Equals("of") Then

                                                node_call.Expression.Type = current.Namespace.TryLoadStruct(CType(v.Receiver, VariableNode).Name, args)
                                                node_call.Arguments = New IEvaluableNode() {}
                                                rk_function = CType(root.Functions("#Type")(0).FixedGeneric(node_call.Expression.Type), RkFunction)
                                                Coverage.Case()
                                                GoTo CIL_OF_FIX_
                                            End If
                                        End If

                                        Dim args_with_receiver = If(receiver Is Nothing, args, {receiver.Type}.Join(args).ToArray)
                                        Dim struct = node_call.Expression.Type.Namespace.TryLoadStruct(name, args_with_receiver)
                                        If TypeOf struct Is RkCILStruct Then

                                            rk_function = CType(struct, RkCILStruct).LoadConstructor(root)
                                            node_call.Arguments = New IEvaluableNode() {}
                                            Coverage.Case()
                                        Else

                                            rk_function = node_call.Expression.Type.Namespace.TryLoadFunction(name, args)

                                            If rk_function Is Nothing AndAlso receiver IsNot Nothing Then

                                                rk_function = node_call.Expression.Type.Namespace.LoadFunction(name, args_with_receiver)
                                                node_call.Arguments = {receiver}.Join(node_call.Arguments).ToArray
                                                Coverage.Case()
                                            End If
                                            Coverage.Case()
                                        End If
CIL_OF_FIX_:
                                        If rk_function IsNot Nothing Then node_call.Expression.Type = rk_function

                                    ElseIf TypeOf node_call.Expression.Type Is RkLateBind Then

                                        rk_function = node_call.Expression.Type.Namespace.LoadFunction(CType(node_call.Expression.Type, RkLateBind).Name, node_call.Arguments.Map(Function(x) x.Type).ToArray)
                                        node_call.Expression.Type = rk_function
                                        Debug.Fail("??")
                                    End If

                                ElseIf TypeOf node_call.Expression Is StructNode Then

                                    Dim node_struct = CType(node_call.Expression, StructNode)
                                    Dim args = {node_call.Expression.Type}.ToList
                                    If node_struct.Struct.HasGeneric Then args.AddRange(node_call.Arguments.Map(Function(x) get_struct(current.Namespace, x)).ToArray)
                                    rk_function = node_struct.Struct.Namespace.LoadFunction("#Alloc", args.ToArray)
                                    Coverage.Case()

                                ElseIf TypeOf node_call.Expression Is PropertyNode Then

                                    Coverage.Case()
                                    Dim prop = CType(node_call.Expression, PropertyNode)
                                    Dim args = {prop.Left.Type}.Join(node_call.Arguments.Map(Function(x) x.Type)).ToArray
                                    rk_function = prop.Left.Type.Namespace.TryGetFunction(prop.Right.Name, args)

                                    ' DotNET method-function support
                                    Coverage.Case()
                                    If rk_function Is Nothing Then rk_function = prop.Left.Type.Namespace.TryGetNamespace(prop.Left.Type.Name)?.TryGetFunction(prop.Right.Name, args)
                                End If

                                Debug.Assert(rk_function IsNot Nothing, "function is not found")
                                If rk_function IsNot Nothing Then

                                    If rk_function.HasGeneric Then

                                        rk_function = CType(rk_function.FixedGeneric(node_call.Arguments.Map(Function(x) x.Type).ToArray), RkFunction)
                                        node_call.FixedGenericFunction = function_generic_fixed_to_node(rk_function)
                                        Coverage.Case()
                                    End If
                                    node_call.Function = rk_function
                                    type_fix = True
                                    Coverage.Case()
                                End If
                            End If

                        ElseIf TypeOf child Is StructNode Then

                            Dim node_struct = CType(child, StructNode)
                            Dim rk_struct = CType(node_struct.Type, RkStruct)

                            For Each s In node_struct.Scope.Where(Function(x) TypeOf x.Value Is LetNode)

                                Dim t = CType(s.Value, LetNode).Type
                                If rk_struct.Local(s.Key) Is Nothing Then

                                    rk_struct.Local(s.Key) = t
                                    If rk_struct.Local(s.Key) IsNot Nothing Then type_fix = True
                                    Coverage.Case()
                                End If
                                If TypeOf t IsNot RkGenericEntry Then

                                    For Each fix In rk_struct.Namespace.Structs(rk_struct.Name)

                                        If fix.Local(s.Key) Is Nothing Then

                                            fix.Local(s.Key) = t
                                            If fix.Local(s.Key) IsNot Nothing Then type_fix = True
                                            Coverage.Case()
                                        End If
                                    Next
                                End If
                            Next
                            Coverage.Case()

                        ElseIf child.GetType.Name.Equals("ListNode`1") Then

                            set_type(CType(child, IEvaluableNode),
                                Function()

                                    Dim list = child.GetType.GetProperty("List").GetValue(child)
                                    Dim count = list.GetType.GetProperty("Count")
                                    If CInt(count.GetValue(list)) = 0 Then Debug.Fail("not yet")

                                    Coverage.Case()
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

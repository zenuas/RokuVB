Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Reflection
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Manager.SystemLibrary
Imports Roku.IntermediateCode
Imports Roku.Util
Imports Roku.Util.ArrayExtension
Imports Roku.Util.TypeHelper


Namespace Compiler

    Public Class Typing

        Public Shared Sub Prototype(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                CType(ns, IScope),
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = New RkStruct With {.Name = node_struct.Name, .StructNode = node_struct, .Scope = current, .Parent = current}
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

                            Dim alloc = New RkNativeFunction With {.Name = "#Alloc", .Operator = InOperator.Alloc, .Scope = rk_struct.Scope, .Parent = current}
                            Dim gens = node_struct.Generics.Map(Function(x) alloc.DefineGeneric(x.Name)).ToArray
                            Dim self = rk_struct.FixedGeneric(gens)
                            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = self})
                            gens.Do(Sub(x) alloc.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = x}))
                            alloc.Return = self
                            alloc.Scope.AddFunction(alloc)
                            Coverage.Case()
                        Else

                            'rk_struct.Initializer = CType(root.LoadFunction("#Alloc", rk_struct), RkNativeFunction)
                            Coverage.Case()
                        End If

                        next_(child, rk_struct)
                        Return

                    ElseIf TypeOf child Is ProgramNode Then

                        Dim node_pgm = CType(child, ProgramNode)
                        Dim ctor As New RkFunction With {.Name = node_pgm.Name, .FunctionNode = New FunctionNode("") With {.Body = CType(node, BlockNode)}, .Scope = ns, .Parent = ns}
                        node_pgm.Function = ctor
                        node_pgm.Owner = node_pgm
                        current.AddFunction(ctor)

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name, .FunctionNode = node_func, .Scope = ns, .Parent = current}
                        node_func.Type = rk_func
                        rk_func.Arguments.AddRange(node_func.Arguments.Map(Function(x) New NamedValue With {.Name = x.Name.Name, .Value = If(x.Type.IsGeneric, rk_func.DefineGeneric(x.Type.Name), Nothing)}))

                        If node_func.Return?.IsGeneric Then

                            Dim r = rk_func.DefineGeneric(node_func.Return.Name)
                            Dim ret = New RkNativeFunction With {.Operator = InOperator.Return, .Scope = ns, .Name = "return", .Parent = rk_func}
                            ret.Arguments.Add(New NamedValue With {.Name = "x", .Value = r})
                            rk_func.Return = r
                            current.AddFunction(ret)
                            Coverage.Case()
                        End If
                        current.AddFunction(rk_func)
                        Coverage.Case()

                        next_(child, rk_func)
                        Return
                    End If

                    next_(child, current)
                End Sub)
        End Sub

        Public Shared Sub TypeStatic(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return
                    next_(child, current + 1)

                    If TypeOf child Is NumericNode Then

                        Dim node_num = CType(child, NumericNode)
                        node_num.Type = New RkSomeType({
                            LoadStruct(root, "Int32"),
                            LoadStruct(root, "Int64"),
                            LoadStruct(root, "Int16"),
                            LoadStruct(root, "Byte")})
                        Coverage.Case()

                    ElseIf TypeOf child Is StringNode Then

                        Dim node_str = CType(child, StringNode)
                        node_str.Type = LoadStruct(root, "String")
                        Coverage.Case()

                    ElseIf TypeOf child Is NullNode Then

                        Dim node_null = CType(child, NullNode)
                        node_null.Type = root.LoadType(GetType(Object).GetTypeInfo)
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeFunctionNode Then

                        Dim node_typef = CType(child, TypeFunctionNode)
                        Dim rk_func As New RkFunction With {.Scope = ns}
                        node_typef.Arguments.Do(Sub(x) rk_func.Arguments.Add(New NamedValue With {.Value = x.Type}))
                        rk_func.Return = node_typef.Return?.Type
                        node_typef.Type = rk_func
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node_type = CType(child, TypeNode)
                        If Not node_type.IsGeneric Then

                            node_type.Type = CType(LoadStruct(ns, node_type.Name), IType)
                            If node_type.IsArray Then node_type.Type = LoadStruct(root, "Array", node_type.Type)
                        End If
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
                            Dim ret = New RkNativeFunction With {.Operator = InOperator.Return, .Scope = ns, .Name = "return", .Parent = rk_function}
                            If node_func.Return IsNot Nothing Then

                                ret.Arguments.Add(New NamedValue With {.Name = "x", .Value = node_func.Return.Type})
                                rk_function.Return = node_func.Return.Type
                            End If
                            rk_function.AddFunction(ret)
                            Coverage.Case()
                        End If
                        Coverage.Case()

                    End If
                End Sub)
        End Sub

        Public Shared Sub TypeInference(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

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
                Function(name As String, scope As IScope)

                    If TypeOf scope Is IFunction Then

                        Coverage.Case()
                        Return CType(scope, IFunction).Generics.FindFirst(Function(x) name.Equals(x.Name))

                    ElseIf TypeOf scope Is RkStruct Then

                        Coverage.Case()
                        Return CType(scope, RkStruct).Generics.FindFirst(Function(x) name.Equals(x.Name))
                    End If
                    Throw New Exception("generic not found")
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
                Function(f As IFunction)

                    Dim base = f.GenericBase.FunctionNode
                    Dim clone = CType(node_deep_copy(base), FunctionNode)

                    For i = 0 To clone.Arguments.Length - 1

                        clone.Arguments(i).Type.Type = f.Arguments(i).Value
                    Next
                    If clone.Return IsNot Nothing Then clone.Return.Type = f.Return
                    clone.Type = f

                    Return clone
                End Function

            Dim fixed_var As Func(Of IEvaluableNode, Boolean, IType) =
                Function(e As IEvaluableNode, fix_byname As Boolean) As IType

                    If TypeOf e.Type Is RkByNameWithReceiver Then

                        Dim receiver = CType(e.Type, RkByNameWithReceiver).Receiver
                        fixed_var(receiver, True)
                        Coverage.Case()
                    End If

                    If TypeOf e.Type Is RkByName AndAlso (fix_byname OrElse TypeOf e.Type IsNot RkByNameWithReceiver) Then

                        Dim byname = CType(e.Type, RkByName)

                        Coverage.Case()
                        Dim t = TryLoadStruct(byname.Scope, byname.Name)
                        If t IsNot Nothing Then

                            Coverage.Case()
                            If TypeOf t Is RkCILStruct Then byname.Scope = CType(t, RkCILStruct).FunctionNamespace
                            byname.Type = t
                            Return t
                        End If

                        Coverage.Case()
                        Dim fs = byname.Scope.FindCurrentFunction(byname.Name).ToList
                        If fs.Count = 1 Then

                            Coverage.Case()
                            byname.Type = fs(0)
                            Return fs(0)

                        ElseIf fs.Count >= 2 Then

                            Coverage.Case()
                            Dim some = New RkSomeType(fs)
                            byname.Type = some
                            Return some
                        End If

                        Coverage.Case()
                        Dim n = TryLoadNamespace(byname.Scope, byname.Name)
                        If n IsNot Nothing Then

                            Coverage.Case()
                            byname.Scope = n
                            byname.Type = n
                            Return n
                        End If
                    End If

                    Coverage.Case()
                    Return e.Type
                End Function

            Dim apply_function =
                Function(some As RkSomeType, f As FunctionCallNode)

                    Dim before = some.Types.Count
                    Dim args = f.Arguments.ToList
                    some.Types = some.Types.Where(
                        Function(x)

                            Dim r = CType(x, IFunction)
                            Return r.Arguments.Count = args.Count AndAlso r.Arguments.And(Function(arg, i) arg.Value.Is(args(i).Type))
                        End Function).ToList
                    Return before <> some.Types.Count
                End Function

            Dim fixed_function =
                Function(f As FunctionCallNode) As IFunction

                    Dim expr = fixed_var(f.Expression, False)
                    Dim args = f.Arguments.Map(Function(x) fixed_var(x, True)).ToList

                    If TypeOf expr Is RkSomeType Then

                        Coverage.Case()
                        Dim some = CType(expr, RkSomeType)
                        apply_function(some, f)
                        If Not some.HasIndefinite Then expr = some.Types(0)
                    End If

                    If TypeOf expr Is RkByName Then

                        Coverage.Case()
                        Dim byname = CType(expr, RkByName)
                        Dim t = TryLoadStruct(byname.Scope, byname.Name, args.ToArray)
                        If t IsNot Nothing Then expr = t
                    End If

                    If TypeOf expr Is RkByName Then

                        Coverage.Case()
                        Dim byname = CType(expr, RkByName)
                        Dim r = TryLoadFunction(byname.Scope, byname.Name, args.ToArray)
                        If r IsNot Nothing Then Return r

                        If TypeOf expr Is RkByNameWithReceiver Then

                            Dim receiver = CType(expr, RkByNameWithReceiver).Receiver

                            If byname.Name.Equals("of") Then

                                Coverage.Case()
                                f.Expression.Type = TryLoadStruct(byname.Scope, CType(receiver, VariableNode).Name, args.ToArray)
                                f.Arguments = New IEvaluableNode() {}
                                Return CType(root.Functions("#Type")(0).FixedGeneric(f.Expression.Type), RkFunction)
                            End If

                            args.Insert(0, receiver.Type)
                            r = TryLoadFunction(byname.Scope, byname.Name, args.ToArray)

                            If r IsNot Nothing Then

                                f.Arguments = {receiver}.Join(f.Arguments).ToArray
                                Return r
                            End If

                        End If

                    ElseIf TypeOf expr Is RkCILStruct Then

                        Coverage.Case()
                        Dim struct = CType(expr, RkCILStruct)
                        Return struct.LoadConstructor(root, args.ToArray)

                    ElseIf TypeOf expr Is RkStruct Then

                        Coverage.Case()
                        Dim struct = CType(expr, RkStruct)
                        args.Insert(0, expr)
                        Return LoadFunction(struct.Scope, "#Alloc", args.ToArray)

                    ElseIf TypeOf expr Is RkNamespace Then

                        Coverage.Case()
                        Dim nsname = CType(expr, RkNamespace)
                        Return TryLoadFunction(nsname, nsname.Name, args.ToArray)

                    ElseIf TypeOf expr Is RkFunction Then

                        Coverage.Case()
                        Dim r = CType(expr, RkFunction)
                        If Not r.HasGeneric Then Return r

                        Coverage.Case()
                        Return CType(r.FixedGeneric(r.ArgumentsToApply(f.Arguments.Map(Function(x) x.Type).ToArray)), IFunction)

                    ElseIf TypeOf expr Is RkSomeType Then

                        Coverage.Case()
                        Return CType(expr, RkSomeType)
                    End If

                    Debug.Fail("not yet")
                    Return Nothing
                End Function

            Dim var_feedback As Func(Of IType, IType, IType) =
                Function(from, to_)

                    If from.HasIndefinite Then

                        If TypeOf from Is RkSomeType Then

                            CType(from, RkSomeType).Merge(to_)
                            Coverage.Case()
                        Else

                            Coverage.Case()
                            Return to_
                        End If

                    ElseIf TypeOf from Is RkByName Then

                        Coverage.Case()
                        Dim byname = CType(from, RkByName)
                        byname.Type = var_feedback(byname.Type, to_)
                    End If

                    Return from
                End Function

            Dim apply_feedback =
                Sub(f As IFunction, node_call As FunctionCallNode)

                    If TypeOf f Is RkNativeFunction AndAlso CType(f, RkNativeFunction).Operator = InOperator.Alloc Then

                        Coverage.Case()

                    ElseIf f.HasGeneric Then

                        Dim apply = f.ArgumentsToApply(node_call.Arguments.Map(Function(x) x.Type).ToArray)
                        Coverage.Case()
                    Else

                        f.Arguments.Where(Function(x) TypeOf x.Value IsNot RkStruct OrElse Not CType(x.Value, RkStruct).ClosureEnvironment).Do(Sub(x, i) node_call.Arguments(i).Type = var_feedback(node_call.Arguments(i).Type, x.Value))
                        Coverage.Case()
                    End If
                End Sub

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
                    CType(ns, IScope),
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        If TypeOf child Is FunctionNode AndAlso CType(child, FunctionNode).Function.HasGeneric Then Return

                        next_(child, If(TypeOf child Is IEvaluableNode AndAlso TypeOf CType(child, IEvaluableNode).Type Is IScope, CType(CType(child, IEvaluableNode).Type, IScope), current))

                        If TypeOf child Is VariableNode Then

                            Dim node_var = CType(child, VariableNode)
                            set_type(node_var, Function() New RkByName With {.Scope = current, .Name = node_var.Name})

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node_type = CType(child, TypeNode)
                            If node_type.IsGeneric Then set_type(node_type, Function() get_generic(node_type.Name, current))
                            Coverage.Case()

                        ElseIf TypeOf child Is LetNode Then

                            Dim node_let = CType(child, LetNode)
                            If set_type(node_let, Function() If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)) Then

                                node_let.Var.Type = node_let.Type
                                Coverage.Case()

                            ElseIf node_let.Type IsNot Nothing Then

                                If node_let.Type.HasIndefinite Then

                                    If CType(node_let.Expression, IFeedback).Feedback(node_let.Var.Type) Then

                                        node_let.Type = node_let.Expression.Type
                                        type_fix = True
                                        Coverage.Case()
                                    End If

                                ElseIf node_let.Type.HasGeneric AndAlso TypeOf node_let.Expression Is IFeedback Then

                                    If CType(node_let.Expression, IFeedback).Feedback(node_let.Var.Type) Then

                                        node_let.Type = node_let.Expression.Type
                                        type_fix = True
                                        Coverage.Case()
                                    End If

                                End If

                            End If

                        ElseIf TypeOf child Is ExpressionNode Then

                            Dim node_expr = CType(child, ExpressionNode)
                            set_type(node_expr,
                                Function()

                                    If node_expr.Function Is Nothing Then node_expr.Function = LoadFunction(current, node_expr.Operator, node_expr.Left.Type, node_expr.Right.Type)
                                    Coverage.Case()
                                    Return node_expr.Function.Return
                                End Function)

                        ElseIf TypeOf child Is PropertyNode Then

                            Dim node_prop = CType(child, PropertyNode)
                            Dim r = fixed_var(node_prop.Left, True)
                            If TypeOf r Is RkStruct Then

                                If set_type(node_prop,
                                    Function()

                                        Coverage.Case()
                                        Dim struct = CType(r, RkStruct)
                                        Dim v = struct.Local.FindFirstOrNull(Function(x) x.Key.Equals(node_prop.Right.Name)).Value
                                        If v IsNot Nothing Then Return v

                                        ' method call syntax sugar
                                        Coverage.Case()
                                        Return New RkByNameWithReceiver With {.Scope = CurrentNamespace(struct.Scope)?.TryGetNamespace(struct.Name), .Name = node_prop.Right.Name, .Receiver = node_prop.Left}
                                    End Function) Then

                                    node_prop.Right.Type = node_prop.Type
                                End If
                            Else

                                set_type(node_prop, Function() New RkByNameWithReceiver With {.Scope = node_prop.Left.Type.Scope, .Name = node_prop.Right.Name, .Receiver = node_prop.Left})
                                Coverage.Case()
                            End If

                        ElseIf TypeOf child Is DeclareNode Then

                            Dim node_declare = CType(child, DeclareNode)
                            node_declare.Name.Type = node_declare.Type.Type
                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionNode Then

                            set_func(CType(child, FunctionNode))
                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node_call = CType(child, FunctionCallNode)
                            If node_call.Function Is Nothing Then

                                node_call.Function = fixed_function(node_call)
                                Debug.Assert(node_call.Function IsNot Nothing, "function is not found")

                                If TypeOf node_call.Function Is RkSomeType AndAlso CType(node_call.Function, RkSomeType).Types.Count > 1 Then

                                    Coverage.Case()
                                Else

                                    apply_feedback(node_call.Function, node_call)
                                    If node_call.Function.GenericBase?.FunctionNode IsNot Nothing Then

                                        node_call.Function.FunctionNode = function_generic_fixed_to_node(node_call.Function)
                                        node_call.FixedGenericFunction = node_call.Function.FunctionNode
                                        Coverage.Case()
                                    End If
                                End If
                                Coverage.Case()

                            ElseIf TypeOf node_call.Function Is RkSomeType Then

                                Dim some = CType(node_call.Function, RkSomeType)
                                Dim before = some.Types.Count
                                If some.Return Is Nothing OrElse (TypeOf some.Return Is RkSomeType AndAlso CType(some.Return, RkSomeType).Types.Count = 0) Then

                                    If apply_function(some, node_call) Then type_fix = True
                                    Coverage.Case()
                                Else

                                    If before > 1 Then

                                        some.Types = some.Types.Where(Function(x) some.Return.Is(CType(x, RkFunction).Return)).ToList
                                        If before <> some.Types.Count Then type_fix = True
                                        Coverage.Case()
                                    End If
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

                                    For Each fix In rk_struct.Scope.FindCurrentStruct(rk_struct.Name)

                                        If fix.Local(s.Key) Is Nothing Then

                                            fix.Local(s.Key) = t
                                            If fix.Local(s.Key) IsNot Nothing Then type_fix = True
                                            Coverage.Case()
                                        End If
                                    Next
                                End If
                            Next
                            Coverage.Case()

                        ElseIf IsGeneric(child.GetType, GetType(ListNode(Of ))) Then

                            set_type(CType(child, IEvaluableNode),
                                Function()

                                    Dim list = child.GetType.GetProperty("List").GetValue(child)
                                    Dim count = list.GetType.GetProperty("Count")
                                    If CInt(count.GetValue(list)) = 0 Then

                                        Coverage.Case()
                                        Return LoadStruct(root, "Array", New RkSomeType)
                                    Else

                                        Coverage.Case()
                                        Dim item = list.GetType.GetProperty("Item")
                                        Return LoadStruct(root, "Array", fixed_var(CType(item.GetValue(list, New Object() {0}), IEvaluableNode), True))
                                    End If

                                End Function)
                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop

            Dim var_normalize As Func(Of IType, IType) =
                Function(t)

                    If TypeOf t Is RkByName Then

                        Coverage.Case()
                        t = var_normalize(CType(t, RkByName).Type)

                    ElseIf TypeOf t Is RkSomeType Then

                        'ToDo: priority check
                        Coverage.Case()
                        Dim some = CType(t, RkSomeType)
                        If some.Types.Count > 1 Then some.Types.RemoveRange(1, some.Types.Count - 1)
                        t = var_normalize(some.Types(0))

                    ElseIf TypeOf t Is RkFunction Then

                        Coverage.Case()
                        Dim f = CType(t, RkFunction)
                        f.Arguments.Do(Sub(x) x.Value = var_normalize(x.Value))
                        f.Return = var_normalize(f.Return)

                    ElseIf TypeOf t Is RkStruct Then

                        Coverage.Case()
                        Dim s = CType(t, RkStruct)
                        s.Local.Keys.ToList.Do(Sub(x) s.Local(x) = var_normalize(s.Local(x)))
                    End If

                    If TypeOf t Is IApply Then

                        Coverage.Case()
                        Dim apply = CType(t, IApply)
                        apply.Apply.Done(Function(x) var_normalize(x))
                    End If

                    Return t
                End Function

            Util.Traverse.NodesOnce(
                node,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    next_(child, current + 1)

                    If TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        node_call.Function = CType(var_normalize(node_call.Function), IFunction)

                    ElseIf TypeOf child Is IEvaluableNode Then

                        Dim e = CType(child, IEvaluableNode)
                        e.Type = var_normalize(e.Type)

                        'Debug.Assert(Not t.HasIndefinite)
                        'Debug.Assert(Not t.HasGeneric)
                    End If

                End Sub)

        End Sub

    End Class

End Namespace

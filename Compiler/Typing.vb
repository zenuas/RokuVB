Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Manager.SystemLibrary
Imports Roku.IntermediateCode
Imports Roku.Util
Imports Roku.Util.Extensions
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
                        Dim rk_func = New RkFunction With {.Name = node_func.Name, .FunctionNode = node_func, .Scope = current, .Parent = current}
                        node_func.Type = rk_func

                        Dim create_generic As Action(Of TypeNode) =
                            Sub(x)

                                If x.IsGeneric Then

                                    rk_func.DefineGeneric(x.Name)

                                ElseIf TypeOf x Is TypeArrayNode Then

                                    create_generic(CType(x, TypeArrayNode).Item)
                                End If
                            End Sub

                        node_func.Arguments.Do(Sub(x) create_generic(x.Type))
                        If node_func.Return IsNot Nothing Then create_generic(node_func.Return)

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
                        node_num.Type = New RkUnionType({
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
                        node_null.Type = New RkUnionType
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeFunctionNode Then

                        Dim node_typef = CType(child, TypeFunctionNode)
                        Dim rk_func As New RkFunction With {.Scope = ns}
                        node_typef.Arguments.Do(Sub(x) rk_func.Arguments.Add(New NamedValue With {.Value = x.Type}))
                        rk_func.Return = node_typef.Return?.Type
                        node_typef.Type = rk_func
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeArrayNode Then

                        Dim node_typearr = CType(child, TypeArrayNode)
                        If Not node_typearr.HasGeneric Then node_typearr.Type = LoadStruct(root, "Array", node_typearr.Item.Type)
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node_type = CType(child, TypeNode)
                        If Not node_type.IsGeneric Then node_type.Type = CType(LoadStruct(ns, node_type.Name), IType)
                        Coverage.Case()

                    ElseIf TypeOf child Is DeclareNode Then

                        Dim node_declare = CType(child, DeclareNode)
                        node_declare.Name.Type = node_declare.Type.Type
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_function = node_func.Function

                        Dim define_type As Func(Of RkFunction, TypeNode, IType) =
                            Function(f, x)

                                If x.Type Is Nothing Then

                                    If x.IsGeneric Then

                                        x.Type = f.DefineGeneric(x.Name)

                                    ElseIf TypeOf x Is TypeArrayNode Then

                                        x.Type = LoadStruct(root, "Array", define_type(f, CType(x, TypeArrayNode).Item))
                                    End If
                                End If

                                Return x.Type
                            End Function

                        node_func.Arguments.Do(Sub(x) rk_function.Arguments.Add(New NamedValue With {.Name = x.Name.Name, .Value = define_type(rk_function, x.Type)}))

                        Dim ret = New RkNativeFunction With {.Operator = InOperator.Return, .Scope = rk_function, .Name = "return", .Parent = rk_function}
                        If node_func.Return IsNot Nothing Then

                            Dim t = define_type(ret, node_func.Return)
                            ret.Arguments.Add(New NamedValue With {.Name = "x", .Value = t})
                            rk_function.Return = t
                        End If
                        rk_function.AddFunction(ret)
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

                        Dim f = CType(scope, IFunction)
                        If f.HasGeneric Then

                            Coverage.Case()
                            Return f.Generics.FindFirst(Function(x) name.Equals(x.Name))
                        Else

                            Coverage.Case()
                            Return f.Apply(f.GenericBase.Generics.FindFirst(Function(x) name.Equals(x.Name)).ApplyIndex)
                        End If

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

            Dim fixed_var As Func(Of IEvaluableNode, IType) =
                Function(e As IEvaluableNode) As IType

                    If TypeOf e.Type Is RkByNameWithReceiver Then

                        Dim byname = CType(e.Type, RkByNameWithReceiver)
                        Dim receiver = byname.Receiver
                        Dim r = fixed_var(receiver)

                        If TypeOf r Is RkStruct Then

                            Coverage.Case()
                            Dim struct = CType(r, RkStruct)
                            If struct.Local.ContainsKey(byname.Name) Then

                                Dim t = struct.Local(byname.Name)
                                If t IsNot Nothing Then

                                    Coverage.Case()
                                    byname.Type = t
                                    Return t
                                End If

                                t = struct.GenericBase?.Local(byname.Name)
                                If t IsNot Nothing Then

                                    Coverage.Case()
                                    byname.Type = t
                                    Return t
                                End If
                            End If

                        ElseIf TypeOf r Is RkNamespace Then

                            Coverage.Case()
                            Dim ns2 = CType(r, RkNamespace)
                            Dim n = TryLoadNamespace(ns2, byname.Name)
                            If n IsNot Nothing Then

                                Coverage.Case()
                                byname.Type = n
                                Return n
                            End If

                            Coverage.Case()
                            byname.Scope = ns2
                        End If

                    ElseIf TypeOf e.Type Is RkByName Then

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
                            Dim union = New RkUnionType(fs)
                            byname.Type = union
                            Return union
                        End If

                        Coverage.Case()
                        Dim n = TryLoadNamespace(byname.Scope, byname.Name)
                        If n IsNot Nothing Then

                            Coverage.Case()
                            byname.Scope = n
                            byname.Type = n
                            Return n
                        End If

                        Coverage.Case()
                        If byname.Type IsNot Nothing Then Return byname.Type
                    End If

                    Coverage.Case()
                    Return e.Type
                End Function

            Dim fixed_byname As Func(Of IType, IType) =
                Function(t)

                    If TypeOf t Is RkByNameWithReceiver Then

                        Coverage.Case()
                        Return fixed_byname(CType(t, RkByNameWithReceiver).Type)

                    ElseIf TypeOf t Is RkByName Then

                        Coverage.Case()
                        Return fixed_byname(CType(t, RkByName).Type)
                    Else

                        Coverage.Case()
                        Return t
                    End If
                End Function

            Dim apply_function =
                Function(union As RkUnionType, f As FunctionCallNode)

                    Dim before = union.Types.Count
                    Dim args = f.Arguments.Map(Function(x) fixed_var(x)).ToList
                    union.Types = union.Types.Where(
                        Function(x)

                            Dim r = CType(x, IFunction)
                            Return r.Arguments.Count = args.Count AndAlso r.Arguments.And(Function(arg, i) arg.Value.Is(args(i)))
                        End Function).ToList
                    Return before <> union.Types.Count
                End Function

            Dim fixed_function =
                Function(f As FunctionCallNode) As IFunction

                    Dim expr = fixed_var(f.Expression)
                    Dim args = f.Arguments.Map(Function(x) fixed_byname(fixed_var(x))).ToList

                    If TypeOf expr Is RkUnionType Then

                        Coverage.Case()
                        Dim union = CType(expr, RkUnionType)
                        apply_function(union, f)
                        If Not union.HasIndefinite Then expr = union.Types(0)
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

                            args.Insert(0, fixed_var(receiver))
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

                    ElseIf TypeOf expr Is RkCILNamespace Then

                        Coverage.Case()
                        Dim struct = CType(expr, RkCILNamespace).BaseType
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

                    ElseIf TypeOf expr Is RkUnionType Then

                        Coverage.Case()
                        Return CType(expr, RkUnionType)
                    End If

                    Return Nothing
                End Function

            Dim var_feedback As Func(Of IType, IType, IType) =
                Function(from, to_)

                    If to_ Is Nothing Then Return from
                    If from Is Nothing Then Return to_

                    If from.HasIndefinite Then

                        If TypeOf from Is RkUnionType Then

                            CType(from, RkUnionType).Merge(to_)
                            Coverage.Case()

                        ElseIf TypeOf from Is IApply Then

                            Coverage.Case()
                            Dim to_apply = CType(to_, IApply)
                            CType(from, IApply).Apply.Done(Function(x, i) If(i >= to_apply.Apply.Count, x, var_feedback(x, to_apply.Apply(i))))
                        Else

                            Debug.Fail("feedback error")
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

                        If TypeOf child Is CaseCastNode Then

                            Dim node_case = CType(child, CaseCastNode)
                            node_case.Var.Type = node_case.Declare.Type

                        ElseIf TypeOf child Is CaseArrayNode Then

                            Dim node_switch = CType(parent, SwitchNode)
                            Dim node_case = CType(child, CaseArrayNode)

                            Dim xs = node_switch.Expression.Type
                            If root.IsArray(xs) Then

                                Dim x = root.GetArrayType(xs)
                                If x IsNot Nothing Then

                                    If node_case.Pattern.Count = 1 Then

                                        node_case.Pattern(0).Type = x
                                        Coverage.Case()

                                    ElseIf node_case.Pattern.Count > 1 Then

                                        node_case.Pattern.Do(Sub(p, i) p.Type = If(i < node_case.Pattern.Count - 1, x, xs))
                                        Coverage.Case()
                                    End If
                                End If
                            End If
                        End If

                        next_(child, If(TypeOf child Is IHaveScopeType, CType(CType(child, IHaveScopeType).Type, IScope), current))

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
                                If node_let.Expression IsNot Nothing Then node_let.IsInstance = node_let.Expression.IsInstance
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
                            Dim r = fixed_var(node_prop.Left)
                            If TypeOf r Is RkStruct Then

                                If set_type(node_prop,
                                    Function()

                                        Coverage.Case()
                                        Dim struct = CType(r, RkStruct)
                                        Dim v = struct.Local.FindFirstOrNull(Function(x) x.Key.Equals(node_prop.Right.Name)).Value
                                        If v IsNot Nothing Then Return v

                                        ' method call syntax sugar
                                        If struct.GenericBase IsNot Nothing Then struct = struct.GenericBase
                                        If TypeOf struct Is RkCILStruct Then

                                            Coverage.Case()
                                            Dim cstruct = CType(struct, RkCILStruct)
                                            Return New RkByNameWithReceiver With {.Scope = cstruct.FunctionNamespace, .Name = node_prop.Right.Name, .Receiver = node_prop.Left}
                                        Else

                                            Coverage.Case()
                                            Return New RkByNameWithReceiver With {.Scope = struct, .Name = node_prop.Right.Name, .Receiver = node_prop.Left}
                                        End If
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

                                If TypeOf node_call.Function Is RkUnionType AndAlso CType(node_call.Function, RkUnionType).Types.Count > 1 Then

                                    Coverage.Case()
                                Else

                                    apply_feedback(node_call.Function, node_call)
                                    If node_call.Function.GenericBase?.FunctionNode IsNot Nothing Then

                                        node_call.Function.FunctionNode = function_generic_fixed_to_node(node_call.Function)
                                        node_call.FixedGenericFunction = node_call.Function.FunctionNode
                                        Coverage.Case()
                                    End If
                                End If

                                type_fix = True
                                Coverage.Case()

                            ElseIf TypeOf node_call.Function Is RkUnionType Then

                                Dim union = CType(node_call.Function, RkUnionType)
                                Dim before = union.Types.Count
                                If union.Return Is Nothing OrElse (TypeOf union.Return Is RkUnionType AndAlso CType(union.Return, RkUnionType).Types.Count = 0) Then

                                    If apply_function(union, node_call) Then type_fix = True
                                    Coverage.Case()
                                Else

                                    If before > 1 Then

                                        union.Types = union.Types.Where(Function(x) union.Return.Is(CType(x, RkFunction).Return)).ToList
                                        If before <> union.Types.Count Then type_fix = True
                                        Coverage.Case()
                                    End If
                                End If

                                Debug.Assert(union.Types.Count > 0)
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
                                        Return LoadStruct(root, "Array", New RkUnionType)
                                    Else

                                        Coverage.Case()
                                        Dim item = list.GetType.GetProperty("Item")
                                        Dim item0 = CType(item.GetValue(list, New Object() {0}), IEvaluableNode)
                                        CType(child, IEvaluableNode).IsInstance = item0.IsInstance
                                        Return LoadStruct(root, "Array", fixed_var(item0))
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

                    ElseIf TypeOf t Is RkUnionType Then

                        'ToDo: priority check
                        Coverage.Case()
                        Dim union = CType(t, RkUnionType)
                        If union.Types.Count > 1 Then union.Types.RemoveRange(1, union.Types.Count - 1)
                        t = var_normalize(union.Types(0))

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

                    If TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        If block.Statements.Count > 0 AndAlso TypeOf block.Statements(block.Statements.Count - 1) Is LambdaExpressionNode Then

                            Dim func = CType(block.Owner, FunctionNode)
                            Dim lambda = CType(block.Statements(block.Statements.Count - 1), LambdaExpressionNode)
                            If CType(func?.Type, RkFunction)?.Return Is Nothing Then

                                If TypeOf lambda.Expression IsNot IStatementNode Then Throw New Exception("lambda isnot statement")
                                block.Statements(block.Statements.Count - 1) = CType(lambda.Expression, IStatementNode)
                            Else

                                Throw New Exception("lambda expression not yet support")
                            End If
                            Coverage.Case()
                        End If
                    End If

                    next_(child, current + 1)

                    If TypeOf child Is FunctionCallNode Then

                        Dim node_call = CType(child, FunctionCallNode)
                        node_call.Function = CType(var_normalize(node_call.Function), IFunction)
                        Coverage.Case()

                    ElseIf TypeOf child Is IEvaluableNode Then

                        Dim e = CType(child, IEvaluableNode)
                        e.Type = var_normalize(e.Type)
                        Coverage.Case()

                        'Debug.Assert(Not t.HasIndefinite)
                        'Debug.Assert(Not t.HasGeneric)

                    ElseIf TypeOf child Is IHaveScopeType Then

                        Dim e = CType(child, IHaveScopeType)
                        var_normalize(e.Type)
                        Coverage.Case()
                    End If

                End Sub)

        End Sub

    End Class

End Namespace

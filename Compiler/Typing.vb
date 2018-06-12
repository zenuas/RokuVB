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
                New With {.Scope = CType(ns, IScope), .Block = CType(ns, IScope)},
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = New RkStruct With {.Name = node_struct.Name, .StructNode = node_struct, .Scope = current.Scope, .Parent = current.Scope}
                        node_struct.Type = rk_struct
                        node_struct.Generics.Each(Sub(x) rk_struct.DefineGeneric(x.Name))
                        current.Scope.AddStruct(rk_struct)

                        If node_struct.Parent IsNot node_struct.Owner Then

                            node_struct.Owner.Function.AddStruct(rk_struct, $"##{node_struct.LineNumber.Value}")
                        End If

                        For Each x In node_struct.Lets.Values

                            If TypeOf x Is LetNode Then

                                Dim let_ = CType(x, LetNode)
                                rk_struct.AddLet(let_.Var.Name, If(let_.Declare?.IsGeneric, rk_struct.Generics.FindFirst(Function(g) g.Name.Equals(let_.Declare.Name)), let_.Type))
                                Coverage.Case()
                            End If
                        Next

                        If rk_struct.HasGeneric Then

                            Dim alloc = New RkNativeFunction With {.Name = "#Alloc", .Operator = InOperator.Alloc, .Scope = rk_struct.Scope, .Parent = current.Scope}
                            Dim gens = node_struct.Generics.Map(Function(x) alloc.DefineGeneric(x.Name)).ToArray
                            Dim self = rk_struct.FixedGeneric(gens)
                            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = self})
                            gens.Each(Sub(x) alloc.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = x}))
                            alloc.Return = self
                            alloc.Scope.AddFunction(alloc)
                            Coverage.Case()
                        Else

                            'rk_struct.Initializer = CType(root.LoadFunction("#Alloc", rk_struct), RkNativeFunction)
                            Coverage.Case()
                        End If

                        next_(child, New With {.Scope = CType(rk_struct, IScope), .Block = CType(rk_struct, IScope)})
                        Return

                    ElseIf TypeOf child Is UnionNode Then

                        Dim node_union = CType(child, UnionNode)
                        Dim rk_union = New RkUnionType With {.UnionName = node_union.Name}
                        node_union.Type = rk_union
                        If Not String.IsNullOrEmpty(node_union.Name) Then current.Scope.AddStruct(rk_union)

                    ElseIf TypeOf child Is ProgramNode Then

                        Dim node_pgm = CType(child, ProgramNode)
                        Dim ctor As New RkFunction With {.Name = node_pgm.Name, .FunctionNode = node, .Scope = ns, .Parent = ns}
                        node_pgm.Scope = ctor
                        node_pgm.Function = ctor
                        current.Scope.AddFunction(ctor)
                        Coverage.Case()

                        next_(child, New With {.Scope = current.Scope, .Block = CType(ctor, IScope)})
                        Return

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name, .FunctionNode = node_func, .Scope = current.Scope, .Parent = current.Scope}
                        node_func.Type = rk_func

                        Dim create_generic As Action(Of TypeNode) =
                            Sub(x)

                                If x.IsGeneric Then

                                    rk_func.DefineGeneric(x.Name)

                                ElseIf TypeOf x Is TypeArrayNode Then

                                    create_generic(CType(x, TypeArrayNode).Item)
                                End If
                            End Sub

                        node_func.Arguments.Each(Sub(x) create_generic(x.Type))
                        If node_func.Return IsNot Nothing Then

                            create_generic(node_func.Return)

                        ElseIf node_func.ImplicitReturn Then

                            rk_func.Return = New RkUnionType
                        End If

                        current.Scope.AddFunction(rk_func)
                        Coverage.Case()

                        next_(child, New With {.Scope = CType(rk_func, IScope), .Block = CType(rk_func, IScope)})
                        Return

                    ElseIf TypeOf child Is BlockNode Then

                        Dim node_block = CType(child, BlockNode)
                        Dim rk_scope = New RkScope With {.Name = $"#{child.LineNumber}", .Parent = current.Scope}
                        node_block.Scope = rk_scope
                        current.Block.AddInnerScope(rk_scope)
                        Coverage.Case()

                        next_(child, New With {.Scope = CType(rk_scope, IScope), .Block = CType(rk_scope, IScope)})
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
                        node_num.Type = New RkUnionType(root.NumericTypes)
                        Coverage.Case()

                    ElseIf TypeOf child Is StringNode Then

                        Dim node_str = CType(child, StringNode)
                        node_str.Type = LoadStruct(root, "String")
                        Coverage.Case()

                    ElseIf TypeOf child Is NullNode Then

                        Dim node_null = CType(child, NullNode)
                        node_null.Type = root.NullType
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeFunctionNode Then

                        Dim node_typef = CType(child, TypeFunctionNode)
                        Dim rk_func As New RkFunction With {.Scope = ns}
                        Dim define_type As Func(Of RkFunction, TypeNode, IType) =
                            Function(f, x)

                                If x Is Nothing Then Return Nothing

                                If x.IsGeneric Then

                                    x.Type = f.DefineGeneric(x.Name)
                                End If

                                Return x.Type
                            End Function
                        node_typef.Arguments.Each(Sub(x) rk_func.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = define_type(rk_func, x)}))
                        rk_func.Return = define_type(rk_func, node_typef.Return)
                        node_typef.Type = rk_func
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeArrayNode Then

                        Dim node_typearr = CType(child, TypeArrayNode)
                        If Not node_typearr.HasGeneric Then node_typearr.Type = LoadStruct(root, "Array", node_typearr.Item.Type)
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeTupleNode Then

                        Dim node_typetuple = CType(child, TypeTupleNode)
                        If Not node_typetuple.HasGeneric Then node_typetuple.Type = root.CreateTuple(node_typetuple.Items.List.Map(Function(x) x.Type).ToArray)
                        Coverage.Case()

                    ElseIf TypeOf child Is UnionNode Then

                        Dim node_union = CType(child, UnionNode)
                        If Not node_union.IsGeneric Then

                            Dim t = CType(node_union.Type, RkUnionType)
                            t.Merge(node_union.Union.List.Map(Function(x) LoadStruct(ns, x.Name)))
                            If node_union.Nullable Then

                                t.Add(root.NullType)
                                node_union.NullAdded = True
                            End If
                        End If
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node_type = CType(child, TypeNode)
                        If Not node_type.IsGeneric Then

                            Dim t = CType(LoadStruct(ns, node_type.Name), IType)
                            If node_type.Nullable Then

                                If TypeOf t IsNot RkUnionType Then t = New RkUnionType({t})
                                CType(t, RkUnionType).Add(root.NullType)
                                node_type.NullAdded = True
                            End If
                            node_type.Type = t
                        End If
                        Coverage.Case()

                    ElseIf TypeOf child Is DeclareNode Then

                        Dim node_declare = CType(child, DeclareNode)
                        node_declare.Name.Type = node_declare.Type.Type
                        Coverage.Case()

                    ElseIf TypeOf child Is ProgramNode Then

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

                        node_func.Arguments.Each(Sub(x) rk_function.Arguments.Add(New NamedValue With {.Name = x.Name.Name, .Value = define_type(rk_function, x.Type)}))

                        If Not node_func.ImplicitReturn Then

                            Dim ret = New RkNativeFunction With {.Operator = InOperator.Return, .Scope = rk_function, .Name = "return", .Parent = rk_function}
                            If node_func.Return IsNot Nothing Then

                                Dim t = define_type(ret, node_func.Return)
                                ret.Arguments.Add(New NamedValue With {.Name = "x", .Value = t})
                                rk_function.Return = t
                            End If
                            rk_function.AddFunction(ret)
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

                        If node_func.ImplicitReturn Then

                            Dim t = CType(node_func.Statements(node_func.Statements.Count - 1), LambdaExpressionNode).Type
                            If t Is Nothing Then

                                CType(rk_function.Return, RkUnionType).Merge(root.VoidType)
                            Else

                                CType(rk_function.Return, RkUnionType).Merge({root.VoidType, t})
                            End If
                        Else

                            rk_function.Return = node_func.Return?.Type
                        End If
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

            Dim fixed_var As Func(Of IType, IType) =
                Function(e)

                    If TypeOf e Is RkByNameWithReceiver Then

                        Dim byname = CType(e, RkByNameWithReceiver)
                        Dim r = fixed_var(byname.Receiver.Type)

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

                        ElseIf TypeOf r Is RkTuple Then

                            Coverage.Case()
                            Dim tuple = CType(r, RkTuple)
                            If tuple.Local.ContainsKey(byname.Name) Then

                                Dim t = tuple.Local(byname.Name)
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

                    ElseIf TypeOf e Is RkByName Then

                        Dim byname = CType(e, RkByName)
                        If byname.Type IsNot Nothing Then Return fixed_var(byname.Type)

                        Coverage.Case()
                        Dim t = TryLoadStruct(byname.Scope, byname.Name)
                        If t IsNot Nothing Then

                            Coverage.Case()
                            If TypeOf t Is RkCILStruct Then byname.Scope = CType(t, RkCILStruct).FunctionNamespace
                            byname.Type = t
                            Return t
                        End If

                        Coverage.Case()
                        Dim fs = FindLoadFunction(byname.Scope, byname.Name).ToList
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
                    Return e
                End Function

            Dim apply_function =
                Function(union As RkUnionType, f As FunctionCallNode)

                    Dim before = union.Types.Count
                    Dim args = f.Arguments.Map(Function(x) FixedByName(fixed_var(x.Type))).ToArray
                    union.Types = union.Types.
                        By(Of IFunction).
                        Where(Function(x) x.Arguments.Count = args.Length AndAlso x.Arguments.And(Function(arg, i) args(i) Is Nothing OrElse arg.Value.Is(args(i)))).
                        Map(Function(x) CType(x, IFunction).ApplyFunction(args)).
                        By(Of IType).
                        ToList
                    Return before <> union.Types.Count
                End Function

            Dim fixed_function =
                Function(f As FunctionCallNode) As IFunction

                    Dim expr = fixed_var(f.Expression.Type)
                    Dim args = f.Arguments.Map(Function(x) FixedByName(fixed_var(x.Type))).ToList

                    If TypeOf expr Is RkUnionType Then

                        Coverage.Case()
                        Dim union = CType(expr, RkUnionType)
                        apply_function(union, f)
                        If Not union.HasIndefinite Then

                            expr = union.Types(0)
                            f.Expression.Type = expr
                        End If
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

                            args.Insert(0, fixed_var(receiver.Type))
                            If TypeOf byname.Scope Is RkCILNamespace Then r = TryLoadFunction(ns, byname.Name, args.ToArray)
                            If r Is Nothing Then r = TryLoadFunction(byname.Scope, byname.Name, args.ToArray)

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
                        Return r.ApplyFunction(args.ToArray)

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

                    ElseIf from.HasGeneric Then

                        Dim x = from.FixedGeneric(from.TypeToApply(to_))
                        If TypeOf x Is IFunction Then

                            node.AddFixedGenericFunction(CType(x, IFunction))
                        End If
                        Coverage.Case()
                        Return x

                    ElseIf TypeOf from Is RkByName Then

                        Coverage.Case()
                        Dim byname = CType(from, RkByName)
                        byname.Type = var_feedback(byname.Type, to_)

                    End If

                    If TypeOf from Is RkFunction AndAlso CType(from, RkFunction).Return IsNot Nothing AndAlso
                        TypeOf to_ Is RkFunction Then

                        Dim from_return = CType(from, RkFunction).Return
                        Dim to_return = CType(to_, RkFunction).Return

                        If TypeOf from_return Is RkUnionType Then

                            CType(from_return, RkUnionType).Merge(If(to_return, root.VoidType))
                        End If
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

                        f.Arguments.Where(Function(x) TypeOf x.Value IsNot RkStruct OrElse Not CType(x.Value, RkStruct).ClosureEnvironment).Each(Sub(x, i) node_call.Arguments(i).Type = var_feedback(node_call.Arguments(i).Type, x.Value))
                        Coverage.Case()
                    End If
                End Sub

            Do While True

                Dim type_fix = False

                Dim set_type =
                    Function(n As IEvaluableNode, f As Func(Of IType))

                        If n.Type IsNot Nothing Then

                            If TypeOf n.Type Is RkByName Then

                                Dim byname = CType(n.Type, RkByName)
                                Do While byname.Type IsNot Nothing

                                    If TypeOf byname.Type Is RkByNameWithReceiver Then Return False
                                    If TypeOf byname.Type IsNot RkByName Then Return False
                                    byname = CType(byname.Type, RkByName)
                                Loop

                                Dim x = f()
                                If x Is n.Type Then Return False
                                byname.Type = x
                                If x IsNot Nothing Then

                                    type_fix = True
                                    Return True
                                End If
                            End If
                        Else

                            n.Type = f()
                            If n.Type IsNot Nothing Then

                                type_fix = True
                                Return True
                            End If
                        End If

                        Return False
                    End Function

                Util.Traverse.NodesOnce(
                    node,
                    CType(ns, IScope),
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        If TypeOf child Is FunctionNode AndAlso CType(child, FunctionNode).Function.HasGeneric Then Return

                        If TypeOf child Is IfCastNode Then

                            Dim node_if = CType(child, IfCastNode)
                            set_type(node_if.Var, Function() node_if.Declare.Type)

                        ElseIf TypeOf child Is CaseCastNode Then

                            Dim node_case = CType(child, CaseCastNode)
                            set_type(node_case.Var, Function() node_case.Declare.Type)

                        ElseIf TypeOf child Is CaseArrayNode Then

                            Dim node_switch = CType(parent, SwitchNode)
                            Dim node_case = CType(child, CaseArrayNode)

                            Dim xs = node_switch.Expression.Type
                            If root.IsArray(xs) Then

                                Dim x = root.GetArrayType(xs)
                                If x IsNot Nothing Then

                                    If node_case.Pattern.Count = 1 Then

                                        set_type(node_case.Pattern(0), Function() x)
                                        Coverage.Case()

                                    ElseIf node_case.Pattern.Count > 1 Then

                                        node_case.Pattern.Each(Sub(p, i) set_type(p, Function() If(i < node_case.Pattern.Count - 1, x, xs)))
                                        Coverage.Case()
                                    End If
                                End If
                            End If
                        End If

                        next_(child, If(TypeOf child Is IHaveScopeType, CType(CType(child, IHaveScopeType).Type, IScope), current))

                        If TypeOf child Is VariableNode Then

                            Dim node_var = CType(child, VariableNode)
                            If node_var.Type Is Nothing Then

                                set_type(node_var,
                                    Function()

                                        If node_var.Scope IsNot Nothing Then

                                            Dim x = node_var.Scope.Lets(node_var.Name)
                                            If TypeOf x Is IEvaluableNode AndAlso CType(x, IEvaluableNode).Type IsNot Nothing Then Return CType(x, IEvaluableNode).Type
                                            If TypeOf x Is IHaveScopeType AndAlso CType(x, IHaveScopeType).Type IsNot Nothing Then Return CType(x, IHaveScopeType).Type
                                        End If
                                        Return New RkByName With {.Scope = current, .Name = node_var.Name}
                                    End Function)
                            End If

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node_type = CType(child, TypeNode)
                            If node_type.IsGeneric Then set_type(node_type, Function() get_generic(node_type.Name, current))
                            If node_type.Nullable AndAlso Not node_type.NullAdded Then

                                Dim t = node_type.Type
                                If TypeOf t IsNot RkUnionType Then t = New RkUnionType({t})
                                CType(t, RkUnionType).Add(root.NullType)
                                node_type.Type = t
                                node_type.NullAdded = True
                                type_fix = True
                            End If
                            Coverage.Case()

                        ElseIf TypeOf child Is LetNode Then

                            Dim node_let = CType(child, LetNode)
                            If set_type(node_let, Function() If(node_let.Expression Is Nothing, node_let.Declare.Type, node_let.Expression.Type)) Then

                                set_type(node_let.Var, Function() node_let.Type)
                                If node_let.Expression IsNot Nothing Then node_let.IsInstance = node_let.Expression.IsInstance
                                Coverage.Case()

                            ElseIf node_let.Type IsNot Nothing Then

                                If node_let.Type.HasIndefinite Then

                                    If node_let.Declare IsNot Nothing Then

                                        CType(node_let.Type, RkUnionType).Merge(node_let.Declare.Type)
                                        Coverage.Case()
                                    End If

                                    If TypeOf node_let.Expression Is IFeedback AndAlso CType(node_let.Expression, IFeedback).Feedback(node_let.Var.Type) Then

                                        set_type(node_let, Function() node_let.Expression.Type)
                                        Coverage.Case()
                                    End If

                                ElseIf node_let.Type.HasGeneric AndAlso TypeOf node_let.Expression Is IFeedback Then

                                    If CType(node_let.Expression, IFeedback).Feedback(node_let.Var.Type) Then

                                        set_type(node_let, Function() node_let.Expression.Type)
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

                        ElseIf TypeOf child Is TupleNode Then

                            Dim node_tuple = CType(child, TupleNode)
                            set_type(node_tuple,
                                Function()

                                    Dim tuple As New RkTuple
                                    node_tuple.Items.Each(Sub(x, i) tuple.AddLet((i + 1).ToString, x.Type))
                                    Return tuple
                                End Function)

                        ElseIf TypeOf child Is PropertyNode Then

                            Dim node_prop = CType(child, PropertyNode)
                            Dim r = fixed_var(node_prop.Left.Type)
                            If TypeOf r Is RkStruct Then

                                If set_type(node_prop,
                                    Function()

                                        Coverage.Case()
                                        Dim struct = CType(r, RkStruct)
                                        If struct.Local.ContainsKey(node_prop.Right.Name) Then

                                            Return struct.Local.FindFirstOrNull(Function(x) x.Key.Equals(node_prop.Right.Name)).Value
                                        End If

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

                        ElseIf TypeOf child Is ProgramNode Then

                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionNode Then

                            set_func(CType(child, FunctionNode))
                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node_call = CType(child, FunctionCallNode)
                            If node_call.Function Is Nothing Then

                                node_call.Function = fixed_function(node_call)
                                If node_call.Function Is Nothing OrElse
                                    (TypeOf node_call.Function Is RkUnionType AndAlso CType(node_call.Function, RkUnionType).Types.Count = 0) Then Throw New CompileErrorException(node_call, "function is not found")

                                If TypeOf node_call.Function Is RkUnionType AndAlso CType(node_call.Function, RkUnionType).Types.Count > 1 Then

                                    Coverage.Case()
                                Else

                                    apply_feedback(node_call.Function, node_call)
                                    If node_call.Function.GenericBase?.FunctionNode IsNot Nothing Then

                                        node.AddFixedGenericFunction(node_call.Function)
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

                            For Each s In node_struct.Lets.Where(Function(x) TypeOf x.Value Is LetNode)

                                Dim t = CType(s.Value, LetNode).Type
                                If Not rk_struct.Local.ContainsKey(s.Key) OrElse rk_struct.Local(s.Key) Is Nothing Then

                                    rk_struct.Local(s.Key) = t
                                    If rk_struct.Local(s.Key) IsNot Nothing Then type_fix = True
                                    Coverage.Case()
                                End If
                                If rk_struct.HasGeneric AndAlso TypeOf t IsNot RkGenericEntry Then

                                    For Each fix In rk_struct.Scope.FindCurrentStruct(rk_struct.Name).By(Of RkStruct)

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
                                        Return LoadStruct(root, "Array", fixed_var(item0.Type))
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
                        Dim union = CType(t, RkUnionType).Types.Where(Function(x) x IsNot root.VoidType).ToList
                        Dim not_num = union.FindFirstOrNull(Function(x) root.NumericTypes.FindFirstOrNull(Function(a) a.Is(x)) Is Nothing)
                        t = var_normalize(If(not_num, If(union.Count > 0, union(0), root.VoidType)))

                    ElseIf TypeOf t Is RkFunction Then

                        Coverage.Case()
                        Dim f = CType(t, RkFunction)
                        f.Arguments.Each(Sub(x) x.Value = var_normalize(x.Value))
                        f.Return = var_normalize(f.Return)

                    ElseIf TypeOf t Is RkStruct Then

                        Coverage.Case()
                        Dim s = CType(t, RkStruct)
                        s.Local.Keys.ToList.Each(Sub(x) s.Local(x) = var_normalize(s.Local(x)))

                    ElseIf TypeOf t Is RkTuple Then

                        Coverage.Case()
                        Dim s = CType(t, RkTuple)
                        s.Local.Keys.ToList.Each(Sub(x) s.Local(x) = var_normalize(s.Local(x)))
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
                            Dim ret_type = var_normalize(func?.Function?.Return)
                            If ret_type Is Nothing OrElse ret_type Is root.VoidType OrElse lambda.Type Is Nothing Then

                                If ret_type Is root.VoidType Then func.Function.Return = Nothing
                                If TypeOf lambda.Expression IsNot IStatementNode Then Throw New Exception("lambda isnot statement")
                                block.Statements(block.Statements.Count - 1) = CType(lambda.Expression, IStatementNode)
                            Else

                                If func.ImplicitReturn Then

                                    Dim retf = New RkNativeFunction With {.Operator = InOperator.Return, .Scope = func.Function, .Name = "return", .Parent = func.Function}
                                    retf.Arguments.Add(New NamedValue With {.Name = "x", .Value = ret_type})
                                    func.Function.AddFunction(retf)
                                End If

                                Dim v As New VariableNode("$ret") With {.Type = lambda.Type}
                                Dim let_ As New LetNode With {.Var = v, .Type = lambda.Type, .Expression = lambda.Expression}
                                Dim ret As New VariableNode("return") With {.Type = New RkByName With {.Scope = block.Owner.Function, .Name = "return"}}
                                Dim fcall As New FunctionCallNode With {.Expression = ret, .Arguments = New IEvaluableNode() {v}}
                                v.AppendLineNumber(lambda)
                                let_.AppendLineNumber(lambda)
                                ret.AppendLineNumber(lambda)
                                fcall.AppendLineNumber(lambda)
                                fcall.Function = fixed_function(fcall)
                                block.Statements(block.Statements.Count - 1) = let_
                                block.Statements.Add(fcall)
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

        Public Shared Sub AnonymouseTypeAllocation(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is IEvaluableNode Then

                        Dim e = CType(child, IEvaluableNode)
                        If TypeOf e.Type Is RkTuple Then e.Type = root.CreateTuple(CType(e.Type, RkTuple))
                        Coverage.Case()
                    End If

                    next_(child, current + 1)
                End Sub)
        End Sub

    End Class

End Namespace

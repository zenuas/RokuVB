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

        Public Shared Function DefineType(root As SystemLibrary, t As IType, base As TypeBaseNode) As IType

            If base Is Nothing Then Return Nothing

            If base.IsGeneric Then

                base.Type = t.DefineGeneric(base.Name)

            ElseIf TypeOf base Is TypeNode Then

                Dim node = CType(base, TypeNode)
                If node.Arguments.Count > 0 Then

                    node.Arguments.Each(Sub(x) DefineType(root, t, x))
                    If TypeOf node.Type Is RkClass Then

                        Dim class_ = CType(node.Type, RkClass)
                        If class_.Generics.Count = 0 Then Return base.Type
                    End If
                    If base.Type.HasGeneric Then base.Type = base.Type.FixedGeneric(node.Arguments.Map(Function(x) x.Type).ToArray)
                End If

            ElseIf TypeOf base Is TypeFunctionNode Then

                Dim node = CType(base, TypeFunctionNode)
                Dim func = CType(node.Type, RkFunction)
                node.Arguments.Each(Sub(x, i) func.Arguments(i).Value = DefineType(root, t, x))
                If node.Return IsNot Nothing Then func.Return = DefineType(root, t, node.Return)

            ElseIf TypeOf base Is TypeArrayNode Then

                If base.Type Is Nothing Then base.Type = LoadStruct(root, "Array", DefineType(root, t, CType(base, TypeArrayNode).Item))

            ElseIf TypeOf base Is TypeTupleNode Then

                Dim node = CType(base, TypeTupleNode)
                node.Items.List.Each(Sub(x, i) DefineType(root, t, x))
            End If

            Return base.Type
        End Function

        Public Shared Sub Prototype(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                New With {.Scope = CType(ns, IScope), .Block = CType(ns, IScope)},
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is StructNode Then

                        Dim node = CType(child, StructNode)
                        Dim struct = New RkStruct With {.Name = node.Name, .StructNode = node, .Scope = current.Scope, .Parent = current.Scope}
                        node.Type = struct
                        node.Generics.Each(Sub(x) struct.DefineGeneric(x.Name))
                        current.Scope.AddStruct(struct)

                        If node.Parent IsNot node.Owner Then

                            node.Owner.Function.AddStruct(struct, $"##{node.LineNumber.Value}")
                        End If

                        next_(child, New With {.Scope = CType(struct, IScope), .Block = CType(struct, IScope)})
                        Return

                    ElseIf TypeOf child Is ClassNode Then

                        Dim node = CType(child, ClassNode)
                        Dim class_ = New RkClass With {.Name = node.Name, .ClassNode = node, .Scope = current.Scope, .Parent = current.Scope}
                        node.Type = class_
                        node.Generics.Each(Sub(x) class_.DefineGeneric(x.Name))
                        current.Scope.AddStruct(class_)

                        next_(child, New With {.Scope = CType(class_, IScope), .Block = CType(class_, IScope)})
                        Return

                    ElseIf TypeOf child Is UnionNode Then

                        Dim node = CType(child, UnionNode)
                        Dim union = New RkUnionType With {.UnionName = node.Name}
                        node.Type = union
                        If Not String.IsNullOrEmpty(node.Name) Then current.Scope.AddStruct(union)

                    ElseIf TypeOf child Is ProgramNode Then

                        Dim node = CType(child, ProgramNode)
                        Dim ctor As New RkFunction With {.Name = node.Name, .FunctionNode = pgm, .Scope = ns, .Parent = ns}
                        node.Scope = ctor
                        node.Function = ctor
                        current.Scope.AddFunction(ctor)
                        Coverage.Case()

                        next_(child, New With {.Scope = current.Scope, .Block = CType(ctor, IScope)})
                        Return

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node = CType(child, FunctionNode)
                        Dim func = New RkFunction With {.Name = node.Name, .FunctionNode = node, .Scope = current.Scope, .Parent = current.Scope}
                        node.Type = func

                        current.Scope.AddFunction(func)
                        Coverage.Case()

                        next_(child, New With {.Scope = CType(func, IScope), .Block = CType(func, IScope)})
                        Return

                    ElseIf TypeOf child Is BlockNode Then

                        Dim node = CType(child, BlockNode)
                        Dim scope = New RkScope With {.Name = $"#{child.LineNumber}", .Parent = current.Scope}
                        node.Scope = scope
                        current.Block.AddInnerScope(scope)
                        Coverage.Case()

                        next_(child, New With {.Scope = CType(scope, IScope), .Block = CType(scope, IScope)})
                        Return
                    End If

                    next_(child, current)
                End Sub)
        End Sub

        Public Shared Sub PrototypeStruct(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return
                    next_(child, current)

                    If TypeOf child Is TypeFunctionNode Then

                        Dim node = CType(child, TypeFunctionNode)
                        Dim func As New RkFunction With {.Scope = ns}
                        node.Arguments.Each(Sub(x) func.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = Nothing}))
                        node.Type = func
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeArrayNode Then

                        Dim node = CType(child, TypeArrayNode)
                        If Not node.HasGeneric Then node.Type = LoadStruct(root, "Array", node.Item.Type)
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeTupleNode Then

                        Dim node = CType(child, TypeTupleNode)
                        If Not node.HasGeneric Then

                            If String.IsNullOrEmpty(node.Name) Then

                                node.Type = root.CreateTuple(node.Items.List.Map(Function(x) x.Type).ToArray)
                            Else
                                node.Type = root.CreateTuple(node.Name, node.Items.List.Map(Function(x) x.Type).ToArray)
                            End If
                        End If
                        Coverage.Case()

                    ElseIf TypeOf child Is UnionNode Then

                        Dim node = CType(child, UnionNode)
                        If Not node.IsGeneric Then

                            Dim t = CType(node.Type, RkUnionType)
                            t.Merge(node.Union.List.Map(Function(x) LoadStruct(ns, x.Name)))
                            If node.Nullable Then

                                t.Add(root.NullType)
                                node.NullAdded = True
                            End If
                        End If
                        Coverage.Case()

                    ElseIf TypeOf child Is TypeNode Then

                        Dim node = CType(child, TypeNode)
                        If node.IsTypeClass Then

                            Dim base = CType(If(node.Namespace Is Nothing, ns, node.Namespace.Type), IScope)
                            node.Type = LoadClass(base, node.Name, node.Arguments.Map(Function(x) x.Type).ToArray)
                            Coverage.Case()

                        ElseIf Not node.IsGeneric Then

                            Dim base = CType(If(node.Namespace Is Nothing, ns, node.Namespace.Type), IScope)
                            Dim t As IType
                            If node.IsNamespace Then

                                t = LoadNamespace(base, node.Name)
                            Else

                                If node.Arguments.Count > 0 Then

                                    t = LoadStruct(base, node.Name, node.Arguments.Map(Function(x) x.Type).ToArray)
                                Else
                                    t = LoadStruct(base, node.Name)
                                End If
                            End If
                            If node.Nullable Then

                                If TypeOf t IsNot RkUnionType Then t = New RkUnionType({t})
                                CType(t, RkUnionType).Add(root.NullType)
                                node.NullAdded = True
                            End If
                            node.Type = t
                            Coverage.Case()
                        End If

                    ElseIf TypeOf child Is StructNode Then

                        Dim node = CType(child, StructNode)
                        Dim struct = CType(node.Type, RkStruct)

                        For Each let_ In node.Lets.Values.By(Of LetNode)

                            Dim t = If(DefineType(root, struct, let_.Declare), let_.Expression?.Type)
                            If t?.HasGeneric AndAlso TypeOf t IsNot RkGenericEntry Then

                                t = t.FixedGeneric(CType(let_.Declare, TypeNode).Arguments.Map(Function(x) DefineType(root, struct, x)).ToArray)
                            End If
                            let_.Type = t
                            struct.AddLet(let_.Var.Name, t)
                        Next
                        Coverage.Case()
                    End If
                End Sub)
        End Sub

        Public Shared Sub PrototypeFunction(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is StructNode Then

                        Dim node = CType(child, StructNode)
                        Dim struct = CType(node.Type, RkStruct)

                        If struct.HasGeneric Then

                            Dim alloc = New RkNativeFunction With {.Name = "#Alloc", .Operator = InOperator.Alloc, .Scope = struct.Scope, .Parent = struct.Scope}
                            Dim gens = node.Generics.Map(Function(x) alloc.DefineGeneric(x.Name)).ToArray
                            Dim self = struct.FixedGeneric(gens)
                            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = self})
                            gens.Each(Sub(x) alloc.Arguments.Add(New NamedValue With {.Name = x.Name, .Value = x}))
                            alloc.Return = self
                            alloc.Scope.AddFunction(alloc)
                            Coverage.Case()
                        Else

                            'struct.Initializer = CType(root.LoadFunction("#Alloc", struct), RkNativeFunction)
                            Coverage.Case()
                        End If

                    ElseIf TypeOf child Is TypeTupleNode Then

                        Dim node = CType(child, TypeTupleNode)
                        Dim tuple = CType(node.Type, RkStruct)

                        If Not String.IsNullOrEmpty(node.Name) Then

                            Dim alloc = New RkNativeFunction With {.Name = "#Alloc", .Operator = InOperator.Alloc, .Scope = tuple.Scope, .Parent = tuple.Scope}
                            alloc.Arguments.Add(New NamedValue With {.Name = "x", .Value = tuple})
                            node.Items.List.Each(Sub(x, i) alloc.Arguments.Add(New NamedValue With {.Name = (i + 1).ToString, .Value = x.Type}))
                            alloc.Return = tuple
                            alloc.Scope.AddFunction(alloc)
                            Coverage.Case()
                        End If

                    ElseIf TypeOf child Is ProgramNode Then

                        ' nothing

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node = CType(child, FunctionNode)
                        Dim func = CType(node.Type, RkFunction)

                        node.Arguments.Each(
                            Sub(x, i)
                                If TypeOf x.Type.Type Is RkClass Then

                                    Dim class_ = CType(x.Type, TypeNode)
                                    class_.IsTypeClass = True
                                    class_.Arguments.Add(New TypeNode With {.Name = $"@{i + 1}", .IsGeneric = True})
                                    node.Where.Add(class_)
                                    x.Type = New TypeNode With {.Name = $"@{i + 1}", .IsGeneric = True}
                                End If
                            End Sub)

                        node.Arguments.Each(Sub(x) func.Arguments.Add(New NamedValue With {.Name = x.Name.Name, .Value = DefineType(root, func, x.Type)}))
                        node.Where.Each(Sub(x) func.Where.Add(CType(DefineType(root, func, x), RkClass)))
                        If node.Return IsNot Nothing Then

                            func.Return = DefineType(root, func, node.Return)

                        ElseIf node.ImplicitReturn Then

                            func.Return = New RkUnionType
                        End If
                        Coverage.Case()
                    End If

                    next_(child, current)
                End Sub)
        End Sub

        Public Shared Sub TypeStatic(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return
                    next_(child, current + 1)

                    If TypeOf child Is NumericNode Then

                        Dim node = CType(child, NumericNode)
                        node.Type = New RkUnionType(root.NumericTypes)
                        Coverage.Case()

                    ElseIf TypeOf child Is StringNode Then

                        Dim node = CType(child, StringNode)
                        node.Type = LoadStruct(root, "String")
                        Coverage.Case()

                    ElseIf TypeOf child Is NullNode Then

                        Dim node = CType(child, NullNode)
                        node.Type = root.NullType
                        Coverage.Case()

                    ElseIf TypeOf child Is BoolNode Then

                        Dim node = CType(child, BoolNode)
                        node.Type = root.BoolType
                        Coverage.Case()

                    ElseIf TypeOf child Is DeclareNode Then

                        Dim node = CType(child, DeclareNode)
                        node.Name.Type = node.Type.Type
                        Coverage.Case()

                    ElseIf TypeOf child Is ProgramNode Then

                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node = CType(child, FunctionNode)
                        Dim func = node.Function

                        If Not node.ImplicitReturn Then

                            Dim ret = New RkNativeFunction With {.Operator = InOperator.Return, .Scope = func, .Name = "return", .Parent = func}
                            If node.Return IsNot Nothing Then

                                ret.Arguments.Add(New NamedValue With {.Name = "x", .Value = DefineType(root, ret, node.Return)})
                                func.Return = DefineType(root, func, node.Return)
                                Coverage.Case()
                            End If
                            func.AddFunction(ret)
                            Coverage.Case()
                        End If

                    End If
                End Sub)
        End Sub

        Public Shared Function FunctionFeedback(root As SystemLibrary, node As FunctionNode) As RkFunction

            Dim func = node.Function
            If Not func.HasGeneric Then

                For Each arg In node.Arguments

                    func.Arguments.FindFirst(Function(x) x.Name.Equals(arg.Name.Name)).Value = arg.Type.Type
                Next

                If node.ImplicitReturn Then

                    Dim t = CType(node.Statements(node.Statements.Count - 1), LambdaExpressionNode).Type
                    If TypeOf t Is RkUnionType AndAlso CType(t, RkUnionType).Types?.Count > 0 Then

                        CType(func.Return, RkUnionType).Merge({CType(root.VoidType, IType)}.Join(CType(t, RkUnionType).Types))

                    ElseIf t IsNot Nothing Then

                        CType(func.Return, RkUnionType).Merge({root.VoidType, t})
                    Else

                        CType(func.Return, RkUnionType).Merge(root.VoidType)
                    End If
                Else

                    If TypeOf func.Return Is RkGenericEntry Then func.Return = node.Return?.Type
                End If
            End If

            Return func
        End Function

        Public Shared Function GetGeneric(name As String, scope As IScope) As IType

            If TypeOf scope Is IFunction Then

                Dim f = CType(scope, IFunction)
                If f.HasGeneric Then

                    Coverage.Case()
                    Dim x = f.Generics.FindFirstOrNull(Function(g) name.Equals(g.Name))
                    If x IsNot Nothing Then Return x
                End If

                Coverage.Case()
                Return f.Apply(CType(f.GenericBase, IFunction).Else(Function() f).Generics.FindFirst(Function(x) name.Equals(x.Name)).ApplyIndex)

            ElseIf TypeOf scope Is RkStruct Then

                Coverage.Case()
                Return CType(scope, RkStruct).Generics.FindFirst(Function(x) name.Equals(x.Name))
            End If
            Throw New Exception("generic not found")
        End Function

        Public Shared Function FixedVar(e As IType) As IType

            If TypeOf e Is RkGenericEntry Then

                Return FixedVar(CType(e, RkGenericEntry).ToType)

            ElseIf TypeOf e Is RkByNameWithReceiver Then

                Dim byname = CType(e, RkByNameWithReceiver)
                Dim r = FixedVar(byname.Receiver.Type)

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
                If byname.Type IsNot Nothing Then Return FixedVar(byname.Type)

                Coverage.Case()
                Dim t = TryLoadStruct(byname.Scope, byname.Name)
                If t IsNot Nothing Then

                    Coverage.Case()
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

        Public Shared Function ApplyFunction(target As IScope, union As RkUnionType, f As FunctionCallNode) As Boolean

            Dim before = union.Types.Count
            Dim args = f.Arguments.Map(Function(x) FixedByName(FixedVar(x.Type))).ToArray
            union.Types = union.Types.
                By(Of IFunction).
                Where(Function(x) x.Arguments.Count = args.Length AndAlso x.Arguments.And(Function(arg, i) args(i) Is Nothing OrElse arg.Value.Is(args(i)))).
                Where(Function(x) x.WhereFunction(target, args)).
                Map(Function(x) x.ApplyFunction(target, args)).
                By(Of IType).
                UniqueList(Function(a, b) a.Is(b)).
                ToList
            Return before <> union.Types.Count
        End Function

        Public Shared Function FixedFunction(root As SystemLibrary, ns As RkNamespace, f As FunctionCallNode) As IFunction

            Dim expr = FixedVar(f.Expression.Type)
            Dim args = f.Arguments.Map(Function(x) FixedByName(FixedVar(x.Type))).ToList

            If TypeOf expr Is RkUnionType Then

                Coverage.Case()
                Dim union = CType(expr, RkUnionType)
                ApplyFunction(ns, union, f)
                union.Unique()
                If union.Types.IsNull Then Throw New Exception("function not found")
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

                    Dim self = FixedByName(FixedVar(receiver.Type))
                    args.Insert(0, self)
                    r = TryLoadFunction(ns, byname.Name, args.ToArray)
                    If r Is Nothing AndAlso TypeOf self Is RkCILStruct Then r = TryLoadFunction(CType(self, RkCILStruct).FunctionNamespace, byname.Name, args.ToArray)

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
                Dim r = TryLoadFunction(struct.Scope, struct.Name, args.ToArray)
                If r IsNot Nothing Then Return r

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
                Return r.ApplyFunction(ns, args.ToArray)

            ElseIf TypeOf expr Is RkUnionType Then

                Coverage.Case()
                Return CType(expr, RkUnionType)
            End If

            Return Nothing
        End Function

        Public Shared Function VarFeedback(pgm As ProgramNode, root As SystemLibrary, from As IType, to_ As IType) As IType

            from = FixedByName(from, True)
            to_ = FixedByName(to_, True)
            If to_ Is Nothing Then Return from
            If from Is Nothing Then Return to_

            If from.HasIndefinite Then

                If TypeOf from Is RkUnionType Then

                    CType(from, RkUnionType).Merge(to_)
                    Coverage.Case()

                ElseIf TypeOf from Is IApply Then

                    Coverage.Case()
                    Dim to_apply = CType(to_, IApply)
                    CType(from, IApply).Apply.Done(Function(x, i) If(i >= to_apply.Apply.Count, x, VarFeedback(pgm, root, x, to_apply.Apply(i))))
                Else

                    Debug.Fail("feedback error")
                End If

            ElseIf from.HasGeneric Then

                Dim x = from.FixedGeneric(from.TypeToApply(to_))
                If TypeOf x Is IFunction Then

                    pgm.AddFixedGenericFunction(CType(x, IFunction))
                End If
                Coverage.Case()
                Return x

            ElseIf TypeOf from Is RkByName Then

                Coverage.Case()
                Dim byname = CType(from, RkByName)
                byname.Type = VarFeedback(pgm, root, byname.Type, to_)

            ElseIf TypeOf from Is IApply Then

                Coverage.Case()
                Dim to_apply = CType(FixedByName(to_), IApply)
                CType(from, IApply).Apply.Done(Function(x, i) If(i >= to_apply.Apply.Count, x, VarFeedback(pgm, root, x, to_apply.Apply(i))))
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

        Public Shared Sub ApplyFeedback(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace, f As IFunction, node As FunctionCallNode, current As IScope)

            If TypeOf f Is RkNativeFunction Then

                Dim native = CType(f, RkNativeFunction)
                If native.Operator = InOperator.Alloc Then

                    Coverage.Case()
                    Return

                ElseIf native.Operator = InOperator.Return Then

                    If TypeOf current Is RkFunction Then

                        Dim func = CType(current, RkFunction)
                        If TypeOf func.Return Is RkGenericEntry Then

                            Dim apply_index = CType(func.Return, RkGenericEntry).ApplyIndex
                            If func.Apply(apply_index) Is Nothing Then

                                Dim x = f.Apply(0)
                                func.Apply(apply_index) = x
                                Do While True

                                    Dim apply = func.ApplyToWhere(ns, func.Apply.ToArray)
                                    If apply(apply_index) Is x Then Exit Do
                                    func.Apply.Clear()
                                    func.Apply.AddRange(apply)
                                    x = apply(apply_index)
                                Loop
                                f.Apply(0) = x
                            End If

                            f.Arguments(0).Value = VarFeedback(pgm, root, f.Arguments(0).Value, func.Return)
                        End If
                    End If
                End If
            End If

            f.Arguments.
                Where(Function(x) TypeOf x.Value IsNot RkStruct OrElse Not CType(x.Value, RkStruct).ClosureEnvironment).
                Each(Sub(x, i) node.Arguments(i).Type = VarFeedback(pgm, root, node.Arguments(i).Type, x.Value))
            Coverage.Case()
        End Sub

        Public Shared Sub TypeInference(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Do While True

                Dim type_fix = False

                Dim set_type =
                    Function(n As IEvaluableNode, f As Func(Of IType))

                        If n.Type IsNot Nothing AndAlso TypeOf FixedByName(n.Type) IsNot RkGenericEntry Then

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

                            Dim x = f()
                            If x Is n.Type Then Return False
                            n.Type = x
                            If n.Type IsNot Nothing Then

                                type_fix = True
                                Return True
                            End If
                        End If

                        Return False
                    End Function

                Util.Traverse.NodesOnce(
                    pgm,
                    CType(ns, IScope),
                    Sub(parent, ref, child, current, isfirst, next_)

                        If Not isfirst Then Return
                        If TypeOf child Is FunctionNode AndAlso CType(child, FunctionNode).Function.HasArgumentsGeneric Then Return

                        If TypeOf child Is IfCastNode Then

                            Dim node = CType(child, IfCastNode)
                            set_type(node.Var, Function() node.Declare.Type)

                        ElseIf TypeOf child Is CaseCastNode Then

                            Dim node = CType(child, CaseCastNode)
                            set_type(node.Var, Function() node.Declare.Type)
                        End If

                        next_(child, If(TypeOf child Is IHaveScopeType, CType(CType(child, IHaveScopeType).Type, IScope), current))

                        If TypeOf child Is VariableNode Then

                            Dim node = CType(child, VariableNode)
                            If node.Type Is Nothing OrElse TypeOf FixedByName(node.Type) Is RkGenericEntry Then

                                set_type(node,
                                    Function()

                                        If node.Scope IsNot Nothing Then

                                            Dim x = node.Scope.Lets(node.Name)
                                            If TypeOf x Is IEvaluableNode AndAlso CType(x, IEvaluableNode).Type IsNot Nothing Then Return CType(x, IEvaluableNode).Type
                                            If TypeOf x Is IHaveScopeType AndAlso CType(x, IHaveScopeType).Type IsNot Nothing Then Return CType(x, IHaveScopeType).Type
                                        End If
                                        Return New RkByName With {.Scope = current, .Name = node.Name}
                                    End Function)
                            End If

                        ElseIf TypeOf child Is TypeNode Then

                            Dim node = CType(child, TypeNode)
                            If node.IsGeneric Then set_type(node, Function() GetGeneric(node.Name, current))
                            If node.Nullable AndAlso Not node.NullAdded Then

                                Dim t = node.Type
                                If TypeOf t IsNot RkUnionType Then t = New RkUnionType({t})
                                CType(t, RkUnionType).Add(root.NullType)
                                node.Type = t
                                node.NullAdded = True
                                type_fix = True
                            End If
                            Coverage.Case()

                        ElseIf TypeOf child Is LetNode Then

                            Dim node = CType(child, LetNode)
                            If set_type(node, Function() If(node.Expression Is Nothing, node.Declare?.Type, node.Expression.Type)) Then

                                set_type(node.Var, Function() node.Type)
                                If node.Expression IsNot Nothing Then node.IsInstance = node.Expression.IsInstance
                                Coverage.Case()

                            ElseIf node.Type IsNot Nothing Then

                                If node.Type.HasIndefinite Then

                                    If node.Declare IsNot Nothing Then

                                        CType(node.Type, RkUnionType).Merge(node.Declare.Type)
                                        Coverage.Case()
                                    End If
                                End If

                                If TypeOf node.Expression Is IFeedback Then

                                    If CType(node.Expression, IFeedback).Feedback(node.Var.Type) Then

                                        type_fix = True
                                        Coverage.Case()
                                    End If

                                    node.Type = node.Expression.Type
                                    node.Var.Type = node.Expression.Type
                                End If
                            End If

                        ElseIf TypeOf child Is ExpressionNode Then

                            Dim node = CType(child, ExpressionNode)
                            set_type(node,
                                Function()

                                    If node.Function Is Nothing Then node.Function = LoadFunction(current, node.Operator, node.Left.Type, node.Right.Type)
                                    Coverage.Case()
                                    Return node.Function.Return
                                End Function)

                        ElseIf TypeOf child Is TupleNode Then

                            Dim node = CType(child, TupleNode)
                            set_type(node,
                                Function()

                                    Dim tuple As New RkTuple
                                    node.Items.Each(Sub(x, i) tuple.AddLet((i + 1).ToString, x.Type))
                                    Return tuple
                                End Function)

                        ElseIf TypeOf child Is PropertyNode Then

                            Dim node = CType(child, PropertyNode)
                            Dim r = FixedByName(FixedVar(node.Left.Type))
                            If TypeOf r Is RkStruct Then

                                If set_type(node,
                                    Function()

                                        Coverage.Case()
                                        Dim struct = CType(r, RkStruct)
                                        If struct.Local.ContainsKey(node.Right.Name) Then

                                            Return struct.Local.FindFirstOrNull(Function(x) x.Key.Equals(node.Right.Name)).Value
                                        End If

                                        ' method call syntax sugar
                                        If struct.GenericBase IsNot Nothing Then struct = struct.GenericBase
                                        If TypeOf struct Is RkCILStruct Then

                                            Coverage.Case()
                                            Dim cstruct = CType(struct, RkCILStruct)
                                            Return New RkByNameWithReceiver With {.Scope = cstruct.FunctionNamespace, .Name = node.Right.Name, .Receiver = node.Left}
                                        Else

                                            Coverage.Case()
                                            Return New RkByNameWithReceiver With {.Scope = struct, .Name = node.Right.Name, .Receiver = node.Left}
                                        End If
                                    End Function) Then

                                    node.Right.Type = node.Type
                                End If
                            Else

                                set_type(node, Function() New RkByNameWithReceiver With {.Scope = node.Left.Type.Scope, .Name = node.Right.Name, .Receiver = node.Left})
                                Coverage.Case()
                            End If

                        ElseIf TypeOf child Is DeclareNode Then

                            Dim node = CType(child, DeclareNode)
                            node.Name.Type = node.Type.Type
                            Coverage.Case()

                        ElseIf TypeOf child Is ProgramNode Then

                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionNode Then

                            FunctionFeedback(root, CType(child, FunctionNode))
                            Coverage.Case()

                        ElseIf TypeOf child Is FunctionCallNode Then

                            Dim node = CType(child, FunctionCallNode)
                            If node.Function Is Nothing Then

                                node.Function = FixedFunction(root, ns, node)
                                If node.Function IsNot Nothing Then

                                    If TypeOf node.Function Is RkUnionType AndAlso CType(node.Function, RkUnionType).Types.Count > 1 Then

                                        Coverage.Case()
                                    Else

                                        ApplyFeedback(pgm, root, ns, node.Function, node, current)
                                        If node.Function.GenericBase?.FunctionNode IsNot Nothing Then

                                            pgm.AddFixedGenericFunction(node.Function)
                                            Coverage.Case()
                                        End If
                                    End If

                                    type_fix = True
                                    Coverage.Case()
                                End If
                            Else

                                If node.Function.HasGeneric Then

                                    Dim f = FixedFunction(root, ns, node)

                                    If f IsNot Nothing AndAlso f IsNot node.Function Then

                                        node.Function = f
                                        ApplyFeedback(pgm, root, ns, node.Function, node, current)
                                        If node.Function.GenericBase?.FunctionNode IsNot Nothing Then

                                            pgm.AddFixedGenericFunction(node.Function)
                                            Coverage.Case()
                                        End If

                                        type_fix = True
                                        Coverage.Case()
                                    End If

                                ElseIf Not node.Function.HasIndefinite Then

                                    ApplyFeedback(pgm, root, ns, node.Function, node, current)
                                End If

                                If TypeOf node.Function Is RkUnionType Then

                                    Dim union = CType(node.Function, RkUnionType)
                                    Dim before = union.Types.Count
                                    If union.Return Is Nothing OrElse (TypeOf union.Return Is RkUnionType AndAlso CType(union.Return, RkUnionType).Types.Count = 0) Then

                                        If ApplyFunction(ns, union, node) Then type_fix = True
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
                            End If

                        ElseIf TypeOf child Is StructNode Then

                            Dim node = CType(child, StructNode)
                            Dim struct = CType(node.Type, RkStruct)

                            For Each s In node.Lets.Where(Function(x) TypeOf x.Value Is LetNode)

                                Dim t = CType(s.Value, LetNode).Type
                                If Not struct.Local.ContainsKey(s.Key) OrElse struct.Local(s.Key) Is Nothing Then

                                    struct.Local(s.Key) = t
                                    If struct.Local(s.Key) IsNot Nothing Then type_fix = True
                                    Coverage.Case()
                                End If
                                If struct.HasGeneric AndAlso TypeOf t IsNot RkGenericEntry Then

                                    For Each fix In struct.Scope.FindCurrentStruct(struct.Name).By(Of RkStruct)

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
                                        Return LoadStruct(root, "Array", FixedVar(item0.Type))
                                    End If

                                End Function)
                        End If
                    End Sub)

                If Not type_fix Then Exit Do
            Loop
        End Sub

        Public Shared Sub LambdaExpressionConvert(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        If block.Statements.Count > 0 AndAlso TypeOf block.Statements(block.Statements.Count - 1) Is LambdaExpressionNode Then

                            Dim func = CType(block.Owner, FunctionNode)
                            Dim lambda = CType(block.Statements(block.Statements.Count - 1), LambdaExpressionNode)
                            Dim ret_type = FixedByName(func?.Function?.Return)
                            If TypeOf ret_type Is RkUnionType Then ret_type = root.ChoosePriorityType(CType(ret_type, RkUnionType).Types)
                            If ret_type Is Nothing OrElse lambda.Type Is Nothing OrElse ret_type.Is(root.VoidType) Then

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
                                fcall.Function = FixedFunction(root, ns, fcall)
                                block.Statements(block.Statements.Count - 1) = let_
                                block.Statements.Add(fcall)
                            End If
                            Coverage.Case()
                        End If
                    End If

                    next_(child, current + 1)
                End Sub)
        End Sub

        Public Shared Sub Normalize(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Dim normalized As New Dictionary(Of IType, Boolean)
            Dim var_normalize As Func(Of IType, IType) =
                Function(t)

                    If t Is Nothing OrElse t Is root.VoidType Then Return Nothing
                    If normalized.ContainsKey(t) Then Return t

                    If TypeOf t Is RkByName Then

                        Coverage.Case()
                        t = var_normalize(CType(t, RkByName).Type)

                    ElseIf TypeOf t Is RkUnionType Then

                        Coverage.Case()
                        Dim types = CType(t, RkUnionType).Types
                        t = var_normalize(root.ChoosePriorityType(types))

                    ElseIf TypeOf t Is RkGenericEntry Then

                        Coverage.Case()
                        t = var_normalize(CType(t, RkGenericEntry).ToType)

                    ElseIf TypeOf t Is RkFunction Then

                        Coverage.Case()
                        normalized(t) = True
                        Dim f = CType(t, RkFunction)
                        f.Arguments.Each(Sub(x) x.Value = var_normalize(x.Value))
                        f.Return = var_normalize(f.Return)

                    ElseIf TypeOf t Is RkStruct Then

                        Coverage.Case()
                        normalized(t) = True
                        Dim s = CType(t, RkStruct)
                        s.Local.Keys.ToList.Each(Sub(x) s.Local(x) = var_normalize(s.Local(x)))

                    ElseIf TypeOf t Is RkTuple Then

                        Coverage.Case()
                        normalized(t) = True
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
                pgm,
                0,
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    next_(child, current + 1)

                    If TypeOf child Is FunctionCallNode Then

                        Dim node = CType(child, FunctionCallNode)
                        node.Function = CType(var_normalize(node.Function), IFunction)
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

            Dim scope_normalize As Action(Of IScope) = Nothing
            Dim function_normalize As Action(Of IFunction) =
                Sub(f)

                    f.Arguments.Each(Sub(x) x.Value = var_normalize(x.Value))
                    f.Return = var_normalize(f.Return)

                    If TypeOf f Is IScope Then scope_normalize(CType(f, IScope))
                End Sub

            scope_normalize =
                Sub(scope)

                    scope.Functions.Each(Sub(kv) kv.Value.Each(Sub(f) function_normalize(f)))
                End Sub

            scope_normalize(ns)
        End Sub

        Public Shared Sub AnonymouseTypeAllocation(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
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

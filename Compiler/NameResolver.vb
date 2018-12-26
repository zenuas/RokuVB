Imports System
Imports Roku.Node
Imports Roku.Util


Namespace Compiler

    Public Class NameResolver

        Public Shared Sub ResolveName(pgm As ProgramNode)

            Dim resolve_name As Func(Of IScopeNode, String, IScopeNode) =
                Function(current, name)

                    If current.Lets.ContainsKey(name) Then Return current
                    If current.Parent Is Nothing Then Return Nothing
                    Return resolve_name(current.Parent, name)
                End Function

            Dim resolve_var As Func(Of IScopeNode, VariableNode, IScopeNode) =
                Function(current, v)

                    Dim x = resolve_name(current, v.Name)
                    If x Is Nothing Then Return v.Scope
                    Return x
                End Function

            Util.Traverse.NodesOnce(
                pgm,
                CType(pgm, IScopeNode),
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is ProgramNode Then

                        Dim node = CType(child, ProgramNode)
                        next_(child, node)
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node = CType(child, FunctionNode)
                        node.Parent = current
                        For Each x In node.Arguments

                            x.Name.Scope = node
                            node.Lets.Add(x.Name.Name, x.Name)
                        Next
                        next_(child, node)
                        Coverage.Case()

                    ElseIf TypeOf child Is StructNode Then

                        Dim node = CType(child, StructNode)
                        node.Parent = current
                        next_(child, node)
                        Coverage.Case()

                    ElseIf TypeOf child Is IfCastNode Then

                        Dim node = CType(child, IfCastNode)
                        node.Then.Lets.Add(node.Var.Name, node.Var)
                        node.Then.Parent = current
                        next_(child, node.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is CaseCastNode Then

                        Dim node = CType(child, CaseCastNode)
                        node.Then.Lets.Add(node.Var.Name, node.Var)
                        node.Then.Parent = current
                        next_(child, node.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is CaseArrayNode Then

                        Dim node = CType(child, CaseArrayNode)
                        If node.Then IsNot Nothing Then

                            node.Pattern.Each(Sub(x) node.Then.Lets.Add(x.Name, x))
                            node.Then.Parent = current
                        End If
                        next_(child, node.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is LetNode Then

                        Dim node = CType(child, LetNode)
                        If node.Receiver Is Nothing AndAlso Not node.IsIgnore Then

                            node.Var.Scope = current
                            If TypeOf current IsNot StructNode Then current.Lets.Add(node.Var.Name, node.Var)
                        End If
                        next_(child, current)
                        Coverage.Case()

                    ElseIf TypeOf child Is PropertyNode Then

                        Dim node = CType(child, PropertyNode)
                        Dim t = node.Right
                        node.Right = Nothing
                        next_(child, current)
                        node.Right = t
                        Coverage.Case()

                    ElseIf TypeOf child Is IScopeNode Then

                        Dim scope = CType(child, IScopeNode)
                        next_(child, scope)
                        Coverage.Case()
                    Else

                        next_(child, current)
                        If TypeOf child Is ImplicitParameterNode Then

                            Dim imp = CType(child, ImplicitParameterNode)
                            Coverage.Case()
                            Do While Not TypeOf current Is FunctionNode

                                current = current.Parent
                            Loop
                            Dim func = CType(current, FunctionNode)
                            For i = func.ImplicitArgumentsCount.Value To imp.Index - 1UI

                                Dim args = func.Arguments.ToList
                                Dim arg = New ImplicitParameterNode($"${i + 1}", i + 1UI) With {.Scope = func}
                                args.Add(New DeclareNode(arg, New TypeNode With {.Name = $"#{i}", .IsGeneric = True}))
                                func.Arguments = args
                                func.Lets.Add(arg.Name, arg)
                            Next
                            func.ImplicitArgumentsCount = Math.Max(func.ImplicitArgumentsCount.Value, imp.Index)
                            imp.Scope = current

                        ElseIf TypeOf child Is VariableNode Then

                            Dim var = CType(child, VariableNode)
                            Coverage.Case()
                            var.Scope = resolve_var(current, var)
                        End If
                        Coverage.Case()
                    End If
                End Sub)

        End Sub

    End Class

End Namespace

Imports System
Imports Roku.Node
Imports Roku.Util


Namespace Compiler

    Public Class NameResolver

        Public Shared Sub ResolveName(node As ProgramNode)

            Dim var_index = 0

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
                node,
                CType(node, IScopeNode),
                Sub(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is ProgramNode Then

                        Dim pgm = CType(child, ProgramNode)
                        next_(child, pgm)
                        Coverage.Case()

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)
                        func.Parent = current
                        For Each x In func.Arguments

                            x.Name.Scope = func
                            func.Lets.Add(x.Name.Name, x.Name)
                        Next
                        next_(child, func)
                        Coverage.Case()

                    ElseIf TypeOf child Is StructNode Then

                        Dim struct = CType(child, StructNode)
                        struct.Parent = current
                        next_(child, struct)
                        Coverage.Case()

                    ElseIf TypeOf child Is IfCastNode Then

                        Dim node_if = CType(child, IfCastNode)
                        node_if.Then.Lets.Add(node_if.Var.Name, node_if.Var)
                        node_if.Then.Parent = current
                        next_(child, node_if.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is CaseCastNode Then

                        Dim node_case = CType(child, CaseCastNode)
                        node_case.Then.Lets.Add(node_case.Var.Name, node_case.Var)
                        node_case.Then.Parent = current
                        next_(child, node_case.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is CaseArrayNode Then

                        Dim node_case = CType(child, CaseArrayNode)
                        If node_case.Then IsNot Nothing Then

                            node_case.Pattern.Each(Sub(x) node_case.Then.Lets.Add(x.Name, x))
                            node_case.Then.Parent = current
                        End If
                        next_(child, node_case.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is LetNode Then

                        Dim node_let = CType(child, LetNode)
                        If node_let.Receiver Is Nothing Then

                            node_let.Var.Scope = current
                            If TypeOf current IsNot StructNode Then current.Lets.Add(node_let.Var.Name, node_let.Var)
                        End If
                        next_(child, current)
                        Coverage.Case()

                    ElseIf TypeOf child Is PropertyNode Then

                        Dim prop = CType(child, PropertyNode)
                        Dim t = prop.Right
                        prop.Right = Nothing
                        next_(child, current)
                        prop.Right = t
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
                            For i = func.ImplicitArgumentsCount.Value To imp.Index - 1

                                Dim args = func.Arguments.ToList
                                args.Add(New DeclareNode(imp, New TypeNode With {.Name = $"#{i}", .IsGeneric = True}))
                                func.Arguments = args.ToArray
                                func.Lets.Add(imp.Name, imp)
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

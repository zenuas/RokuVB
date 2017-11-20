Imports System
Imports Roku.Node
Imports Roku.Util


Namespace Compiler

    Public Class NameResolver

        Public Shared Sub ResolveName(node As ProgramNode)

            Dim var_index = 0

            Dim resolve_name As Func(Of IScopeNode, String, INode) =
                Function(current As IScopeNode, name As String)

                    If current.Scope.ContainsKey(name) Then

                        Dim x = current.Scope(name)
                        If TypeOf x IsNot IEvaluableNode Then Return Nothing
                        Return x
                    End If
                    If current.Parent Is Nothing Then Return Nothing
                    Return resolve_name(current.Parent, name)
                End Function

            Dim resolve_var As Func(Of IScopeNode, VariableNode, INode) =
                Function(current As IScopeNode, v As VariableNode)

                    Dim x = resolve_name(current, v.Name)
                    Return If(x Is Nothing, v, If(TypeOf x Is LetNode, CType(x, LetNode).Var, x))
                End Function

            Util.Traverse.NodesReplaceOnce(
                node,
                CType(node, IScopeNode),
                Function(parent, ref, child, current, isfirst, next_)

                    If Not isfirst Then Return child

                    If TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)
                        Dim body = func.Body
                        body.Parent = current
                        For Each x In func.Arguments

                            x.Name.Scope = body
                            body.Scope.Add(x.Name.Name, x.Name)
                        Next
                        next_(child, body)
                        Coverage.Case()

                    ElseIf TypeOf child Is StructNode Then

                        Dim struct = CType(child, StructNode)
                        struct.Parent = current
                        next_(child, struct)
                        Coverage.Case()

                    ElseIf TypeOf child Is IfCastNode Then

                        Dim node_if = CType(child, IfCastNode)
                        node_if.Then.Scope.Add(node_if.Var.Name, node_if.Var)
                        next_(child, node_if.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is CaseCastNode Then

                        Dim node_case = CType(child, CaseCastNode)
                        node_case.Then.Scope.Add(node_case.Var.Name, node_case.Var)
                        next_(child, node_case.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is CaseArrayNode Then

                        Dim node_case = CType(child, CaseArrayNode)
                        If node_case.Then IsNot Nothing Then node_case.Pattern.Do(Sub(x) node_case.Then.Scope.Add(x.Name, x))
                        next_(child, node_case.Then)
                        Coverage.Case()

                    ElseIf TypeOf child Is LetNode Then

                        Dim node_let = CType(child, LetNode)
                        If node_let.Receiver Is Nothing Then

                            node_let.Var.Scope = current
                            If TypeOf current IsNot StructNode Then

                                current.Scope.Add(node_let.Var.Name, node_let.Var)
                                If current.Parent IsNot Nothing AndAlso resolve_name(current.Parent, node_let.Var.Name) IsNot Nothing Then

                                    node_let.Var.Name = $"##{var_index}"
                                    var_index += 1
                                End If
                            End If
                        End If
                        next_(child, current)
                        Coverage.Case()

                    ElseIf TypeOf child Is PropertyNode Then

                        Dim prop = CType(child, PropertyNode)
                        If TypeOf prop.Left Is VariableNode Then

                            Dim var = CType(prop.Left, VariableNode)
                            prop.Left = CType(resolve_var(current, var), IEvaluableNode)
                            Coverage.Case()
                        End If
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
                        If TypeOf child Is VariableNode Then

                            Dim var = CType(child, VariableNode)
                            Coverage.Case()
                            Return resolve_var(current, var)
                        End If
                        Coverage.Case()
                    End If

                    Return child
                End Function)

        End Sub

    End Class

End Namespace

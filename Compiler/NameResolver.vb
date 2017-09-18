Imports System
Imports Roku.Node
Imports Roku.Util


Namespace Compiler

    Public Class NameResolver

        Public Shared Sub ResolveName(node As ProgramNode)

            Dim resolve_name As Func(Of IScopeNode, String, INode) =
                Function(current As IScopeNode, name As String)

                    If current.Scope.ContainsKey(name) Then Return current.Scope(name)
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

                    ElseIf TypeOf child Is CaseArrayNode Then

                        Dim node_case = CType(child, CaseArrayNode)
                        If node_case.Then IsNot Nothing Then node_case.Pattern.Do(Sub(x) node_case.Then.Scope.Add(x.Name, x))
                        Coverage.Case()

                    Else

                        next_(child, current)
                        If TypeOf child Is VariableNode Then

                            Dim var = CType(child, VariableNode)
                            If TypeOf parent Is LetNode AndAlso CType(parent, LetNode).NameBinding AndAlso ref.Equals("Var") Then

                                var.Scope = current
                                If TypeOf current IsNot StructNode Then current.Scope.Add(var.Name, child)
                                Coverage.Case()
                            Else

                                Coverage.Case()
                                Return resolve_var(current, var)
                            End If

                        End If
                        Coverage.Case()
                    End If

                    Return child
                End Function)

        End Sub

    End Class

End Namespace

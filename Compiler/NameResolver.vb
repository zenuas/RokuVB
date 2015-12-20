Imports System
Imports Roku.Node


Namespace Compiler

    Public Class NameResolver

        Public Shared Sub ResolveName(node As INode)

            Dim resolve_name As Func(Of IScopeNode, String, INode) =
                Function(current As IScopeNode, name As String)

                    If current.Scope.ContainsKey(name) Then Return current.Scope(name)
                    If current.Parent Is Nothing Then Return Nothing
                    Return resolve_name(current.Parent, name)
                End Function

            Dim resolve_var As Func(Of IScopeNode, VariableNode, INode) =
                Function(current As IScopeNode, v As VariableNode)

                    Dim x = resolve_name(current, v.Name)
                    Return If(x Is Nothing, v, x)
                End Function

            Util.Traverse.NodesReplaceOnce(
                node,
                CType(node, BlockNode),
                Function(parent, ref, child, current, isfirst, next_)

                    If parent IsNot Nothing AndAlso TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        If TypeOf block.Owner Is FunctionNode Then

                            Dim func = CType(block.Owner, FunctionNode)
                            For Each x In func.Arguments

                                x.Name.Scope = current
                                block.Scope.Add(x.Name.Name, x.Name)
                            Next
                        End If
                        block.Parent = current
                        next_(block, block)

                    Else

                        next_(child, current)
                        If TypeOf child Is VariableNode Then

                            Dim var = CType(child, VariableNode)
                            If TypeOf parent Is LetNode AndAlso ref.Equals("Var") Then

                                var.Scope = current
                                current.Scope.Add(var.Name, child)
                            Else

                                Return resolve_var(current, var)
                            End If

                        End If
                    End If

                    Return child
                End Function)

        End Sub

    End Class

End Namespace

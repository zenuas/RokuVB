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

            Dim scope_search As Action(Of INode) =
                Sub(x As INode)

                    If Not TypeOf x Is BlockNode Then Return
                    Dim current = CType(x, BlockNode)

                    Util.Traverse.Nodes(current,
                        Function(parent, ref, child, replace, isfirst)

                            If parent IsNot Nothing AndAlso TypeOf child Is BlockNode Then

                                CType(child, BlockNode).Parent = current
                                scope_search(child)
                                Return False

                            ElseIf TypeOf child Is VariableNode Then

                                Dim var = CType(child, VariableNode)
                                If TypeOf parent Is LetNode AndAlso ref.Equals("Var") Then

                                    var.Scope = current
                                    current.Scope.Add(var.Name, child)
                                Else

                                    replace(resolve_var(current, var))
                                End If

                            End If

                            Return True
                        End Function)
                End Sub

            scope_search(node)

        End Sub

    End Class

End Namespace

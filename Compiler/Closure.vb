Imports System
Imports Roku.Node
Imports Roku.Util


Namespace Compiler

    Public Class Closure

        Public Shared Sub Capture(node As ProgramNode)

            Dim get_inner_scope As Func(Of IScopeNode, IScopeNode) = Function(x) If(x Is Nothing OrElse Not x.InnerScope, x, get_inner_scope(x.Parent))

            Util.Traverse.NodesOnce(
                node,
                CType(node, IScopeNode),
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is IScopeNode Then current = CType(child, IScopeNode)

                    If TypeOf child Is VariableNode Then

                        Dim var = CType(child, VariableNode)
                        Dim var_scope = get_inner_scope(var.Scope)
                        Dim inner_scope = get_inner_scope(current)

                        If var_scope IsNot Nothing AndAlso var_scope IsNot inner_scope AndAlso TypeOf var.Scope.Lets(var.Name) Is VariableNode Then

                            var.ClosureEnvironment = True
                            CType(var.Scope.Lets(var.Name), VariableNode).ClosureEnvironment = True
                            Dim scope = inner_scope
                            Do
                                If TypeOf scope.Owner Is FunctionNode Then

                                    Dim func = CType(scope.Owner, FunctionNode)
                                    If func.Bind.ContainsKey(var.Scope) Then Exit Do
                                    func.Bind.Add(var.Scope, True)
                                End If
                                scope = scope.Parent

                            Loop While var_scope IsNot scope
                            Coverage.Case()
                        End If
                    End If

                    next_(child, current)
                End Sub)

        End Sub

    End Class

End Namespace

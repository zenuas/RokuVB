﻿Imports Roku.Node
Imports Roku.Util


Namespace Compiler

    Public Class Closure

        Public Shared Sub Capture(node As ProgramNode)

            Util.Traverse.NodesOnce(
                node,
                CType(node, IScopeNode),
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is FunctionNode Then current = CType(child, FunctionNode).Body
                    If TypeOf child Is IScopeNode Then current = CType(child, IScopeNode)

                    If TypeOf child Is VariableNode Then

                        Dim var = CType(child, VariableNode)

                        If var.Scope IsNot Nothing AndAlso var.Scope IsNot current Then

                            Do While current IsNot Nothing AndAlso var.Scope IsNot current AndAlso current.InnerScope

                                current = current.Parent
                            Loop
                            Coverage.Case()
                        End If

                        If var.Scope IsNot Nothing AndAlso var.Scope IsNot current Then

                            var.ClosureEnvironment = True
                            Dim scope = current
                            Do
                                If TypeOf scope.Owner Is FunctionNode Then

                                    Dim func = CType(scope.Owner, FunctionNode)
                                    If func.Bind.ContainsKey(var.Scope) Then Exit Do
                                    func.Bind.Add(var.Scope, True)
                                End If
                                scope = scope.Parent

                            Loop While var.Scope IsNot scope
                            Coverage.Case()
                        End If
                    End If

                    next_(child, current)
                End Sub)

        End Sub

    End Class

End Namespace

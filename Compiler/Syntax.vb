Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Syntax

        Public Shared Sub SwitchCaseArray(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                New With {.VarIndex = 0},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is CaseArrayNode Then

                        Dim switch = CType(parent, SwitchNode)
                        Dim case_array = CType(child, CaseArrayNode)

                        If case_array.Pattern.Count = 0 Then

                            ' if ! isnull(switch.Expression) then break
                            'case_array.Statements.Add()

                        ElseIf case_array.Pattern.Count = 1 Then

                            ' if isnull(switch.Expression) then break
                            ' case_array.Pattern[0] = car(switch.Expression)
                        Else

                            ' if isnull(switch.Expression) then break
                            ' case_array.Pattern[0] = car(switch.Expression)
                            ' $1 = cdr(switch.Expression)
                            ' if isnull($1) then break
                            ' case_array.Pattern[1] = car($1)
                            ' $2 = cdr($1)
                            ' if isnull($2) then break
                            ' case_array.Pattern[3] = car($2)
                            ' ...
                        End If
                    End If

                    next_(child, user)
                End Sub)
        End Sub

        Public Shared Sub Coroutine(node As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                0,
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    next_(child, user)
                End Sub)
        End Sub

    End Class

End Namespace

Imports System
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Parser.MyParser
Imports Roku.Util.Extensions


Namespace Compiler

    Public Class Syntax

        Public Shared Sub SwitchCaseConvert(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                New With {.VarIndex = 0, .Scope = CType(pgm, IScopeNode)},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is IScopeNode Then

                        Dim old = user.Scope
                        user.Scope = CType(child, IScopeNode)
                        next_(child, user)
                        user.Scope = old
                        Return

                    ElseIf TypeOf child Is CaseValueNode Then

                        Dim switch = CType(parent, SwitchNode)
                        Dim case_value = CType(child, CaseValueNode)

                        Dim last = CType(case_value.Value.Statements(case_value.Value.Statements.Count - 1), LetNode)
                        ' $case1 = $ret == switch.Expression
                        ' if $case1 else break
                        Dim s1 = CreateVariableNode($"$case{user.VarIndex}", case_value)
                        case_value.Value.Statements.Add(CreateLetNode(s1, CreateFunctionCallNode(CreateVariableNode("==", case_value), CreateVariableNode("$ret", case_value), switch.Expression)))
                        case_value.Value.Statements.Add(CreateIfNode(s1, Nothing, ToBlock(user.Scope, New BreakNode)))

                    ElseIf TypeOf child Is CaseArrayNode Then

                        Dim switch = CType(parent, SwitchNode)
                        Dim case_array = CType(child, CaseArrayNode)

                        If case_array.Pattern.Count = 0 Then

                            ' $s1 = isnull(switch.Expression)
                            ' if $s1 else break
                            Dim s1 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s1, CreateFunctionCallNode(CreateVariableNode("isnull", case_array), switch.Expression)))
                            case_array.Statements.Add(CreateIfNode(s1, Nothing, ToBlock(user.Scope, New BreakNode)))
                        Else

                            ' $s1 = isnull(switch.Expression)
                            ' if $s1 then break
                            Dim s1 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s1, CreateFunctionCallNode(CreateVariableNode("isnull", case_array), switch.Expression)))
                            case_array.Statements.Add(CreateIfNode(s1, ToBlock(user.Scope, New BreakNode)))

                            ' case_array.Pattern[0] = car(switch.Expression)
                            case_array.Statements.Add(CreateLetNode(case_array.Pattern(0), CreateFunctionCallNode(CreateVariableNode("car", case_array), switch.Expression)))

                            ' $s2 = cdr(switch.Expression)
                            Dim s2 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s2, CreateFunctionCallNode(CreateVariableNode("cdr", case_array), switch.Expression)))

                            If case_array.Pattern.Count = 1 Then

                                ' $s3 = isnull($s2)
                                Dim s3 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                user.VarIndex += 1
                                case_array.Statements.Add(CreateLetNode(s3, CreateFunctionCallNode(CreateVariableNode("isnull", case_array), s2)))

                                ' if $s3 else break
                                case_array.Statements.Add(CreateIfNode(s3, Nothing, ToBlock(user.Scope, New BreakNode)))
                            Else

                                For i = 1 To case_array.Pattern.Count - 2

                                    ' $s3 = isnull($s2)
                                    Dim s3 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s3, CreateFunctionCallNode(CreateVariableNode("isnull", case_array), s2)))

                                    ' if $s3 then break
                                    case_array.Statements.Add(CreateIfNode(s3, ToBlock(user.Scope, New BreakNode)))

                                    ' case_array.Pattern[i] = car($s2)
                                    case_array.Statements.Add(CreateLetNode(case_array.Pattern(i), CreateFunctionCallNode(CreateVariableNode("car", case_array), s2)))

                                    ' $s4 = cdr($s2)
                                    Dim s4 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s4, CreateFunctionCallNode(CreateVariableNode("cdr", case_array), s2)))

                                    s2 = s4
                                Next

                                ' case_array.Pattern[N] = next
                                case_array.Statements.Add(CreateLetNode(case_array.Pattern(case_array.Pattern.Count - 1), s2))
                            End If
                        End If
                    End If

                    next_(child, user)
                End Sub)
        End Sub

        Public Shared Sub ArrayClass(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                New With {.VarIndex = 0},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is FunctionNode Then

                        Dim f = CType(child, FunctionNode)

                        Dim replace As Func(Of TypeBaseNode, TypeBaseNode) =
                            Function(t As TypeBaseNode) As TypeBaseNode

                                If TypeOf t Is TypeNode Then

                                ElseIf TypeOf t Is TypeFunctionNode Then

                                ElseIf TypeOf t Is TypeArrayNode Then

                                    Dim list As New TypeNode(New VariableNode("List")) With {.IsTypeClass = True}
                                    Dim tx As New TypeNode(New VariableNode($"@@{user.VarIndex}")) With {.IsGeneric = True}
                                    user.VarIndex += 1
                                    list.Arguments.Add(tx)
                                    list.Arguments.Add(replace(CType(t, TypeArrayNode).Item))
                                    f.Where.Add(list)
                                    Return tx

                                ElseIf TypeOf t Is TypeTupleNode Then
                                End If
                                Return t
                            End Function

                        f.Arguments?.Each(Sub(x) x.Type = replace(x.Type))
                        f.Return = replace(f.Return)
                    End If

                    next_(child, user)
                End Sub)
        End Sub

        Public Shared Sub Coroutine(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                0,
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    next_(child, user)
                End Sub)
        End Sub

    End Class

End Namespace

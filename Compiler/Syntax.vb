Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Manager
Imports Roku.Parser
Imports Roku.Parser.MyParser
Imports Roku.Util.Extensions


Namespace Compiler

    Public Class Syntax

        Public Shared Sub TupleDeconstruction(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                New With {.VarIndex = 0, .Scope = CType(pgm, IScopeNode)},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        Dim program_pointer = 0

                        Do While program_pointer < block.Statements.Count

                            Dim v = block.Statements(program_pointer)
                            If TypeOf v Is LetNode Then

                                Dim let_ = CType(v, LetNode)
                                If let_.TupleAssignment Then

                                    Dim tuples = CType(let_.Receiver, ListNode(Of LetNode))
                                    let_.Var = CreateVariableNode($"$tuple{user.VarIndex}", let_)
                                    let_.Receiver = Nothing
                                    For i = 0 To tuples.List.Count - 1

                                        Dim tuple = tuples.List(i)
                                        If tuple.IsIgnore Then Continue For
                                        tuple.Expression = CreatePropertyNode(let_.Var, Nothing, CreateVariableNode($"{i + 1}", let_.Var))
                                        tuple.Expression.AppendLineNumber(let_.Var)
                                        block.Statements.Insert(program_pointer + 1, tuple)
                                        program_pointer += 1
                                    Next
                                End If
                            End If
                            program_pointer += 1
                        Loop
                    End If

                    next_(child, user)
                End Sub)
        End Sub

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

                            ' $s1 = switch.Expression.isnull
                            ' $s2 = $s1()
                            ' if $s2 else break
                            Dim s1 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            Dim s2 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s1, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("isnull", case_array))))
                            case_array.Statements.Add(CreateLetNode(s2, CreateFunctionCallNode(s1)))
                            case_array.Statements.Add(CreateIfNode(s2, Nothing, ToBlock(user.Scope, New BreakNode)))
                        Else

                            ' $s1 = switch.Expression.isnull
                            ' $s2 = $s1()
                            ' if $s2 then break
                            Dim s1 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            Dim s2 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s1, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("isnull", case_array))))
                            case_array.Statements.Add(CreateLetNode(s2, CreateFunctionCallNode(s1)))
                            case_array.Statements.Add(CreateIfNode(s2, ToBlock(user.Scope, New BreakNode)))

                            ' $s3 = switch.Expression.car
                            ' case_array.Pattern[0] = $s3()
                            Dim s3 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s3, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("car", case_array))))
                            case_array.Statements.Add(CreateLetNode(case_array.Pattern(0), CreateFunctionCallNode(s3)))

                            ' $s4 = switch.Expression.cdr
                            ' $s5 = $s4()
                            Dim s4 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            Dim s5 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s4, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("cdr", case_array))))
                            case_array.Statements.Add(CreateLetNode(s5, CreateFunctionCallNode(s4)))

                            If case_array.Pattern.Count = 1 Then

                                ' $s6 = $s5.isnull
                                ' $s7 = $s6()
                                Dim s6 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                user.VarIndex += 1
                                Dim s7 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                user.VarIndex += 1
                                case_array.Statements.Add(CreateLetNode(s6, CreatePropertyNode(s5, Nothing, CreateVariableNode("isnull", case_array))))
                                case_array.Statements.Add(CreateLetNode(s7, CreateFunctionCallNode(s6)))

                                ' if $s7 else break
                                case_array.Statements.Add(CreateIfNode(s7, Nothing, ToBlock(user.Scope, New BreakNode)))
                            Else

                                For i = 1 To case_array.Pattern.Count - 2

                                    ' $s6 = $s5.isnull
                                    ' $s7 = $s6()
                                    Dim s6 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    Dim s7 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s6, CreatePropertyNode(s5, Nothing, CreateVariableNode("isnull", case_array))))
                                    case_array.Statements.Add(CreateLetNode(s7, CreateFunctionCallNode(s6)))

                                    ' if $s7 then break
                                    case_array.Statements.Add(CreateIfNode(s7, ToBlock(user.Scope, New BreakNode)))

                                    ' $s8 = $s5.car
                                    ' case_array.Pattern[i] = $s7()
                                    Dim s8 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s8, CreatePropertyNode(s5, Nothing, CreateVariableNode("car", case_array))))
                                    case_array.Statements.Add(CreateLetNode(case_array.Pattern(i), CreateFunctionCallNode(s8)))

                                    ' $s9 = $s5.cdr
                                    ' $s10 = $s9()
                                    Dim s9 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    Dim s10 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s9, CreatePropertyNode(s5, Nothing, CreateVariableNode("cdr", case_array))))
                                    case_array.Statements.Add(CreateLetNode(s10, CreateFunctionCallNode(s9)))

                                    s5 = s10
                                Next

                                ' case_array.Pattern[N] = next
                                case_array.Statements.Add(CreateLetNode(case_array.Pattern(case_array.Pattern.Count - 1), s5))
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

                    If TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)
                        If func.Coroutine Then

                            Dim p = func.Parent
                            Dim self_type = New TypeNode(CreateVariableNode(func.Name, func))
                            Dim t = func.Where.FindFirst(Function(x) x.Name.Equals("List") AndAlso x.Arguments(0) Is func.Return).Arguments(1)
                            Dim scope = CType(p, IAddFunction)

                            ' struct "func.Name"
                            '     var state = 0
                            '     var next  = 0
                            '     var value: "t"
                            Dim co = New StructNode(func.LineNumber.Value) With {.Name = func.Name, .Parent = p}
                            co.Lets.Add("state", CreateLetNode(CreateVariableNode("state", func), New NumericNode("0", 0)))
                            co.Lets.Add("next", CreateLetNode(CreateVariableNode("next", func), New NumericNode("0", 0)))
                            co.Lets.Add("value", CreateLetNode(CreateVariableNode("value", func), t))
                            p.Lets.Add(co.Name, co)

                            ' sub co() [t] -> sub cdr(self: co) [t]
                            Dim cdr = New FunctionNode(func.LineNumber.Value) With {.Name = "cdr", .Arguments = New List(Of DeclareNode)}
                            cdr.Arguments.Add(New DeclareNode(CreateVariableNode("#self", func), self_type))
                            cdr.Where.Add(func.Where(0))
                            cdr.Return = func.Return
                            scope.AddFunction(cdr)

                            ' sub co() [t] -> sub car(self: co) t
                            '     
                            func.Name = "car"
                            func.Arguments.Clear()
                            func.Arguments.Add(New DeclareNode(CreateVariableNode("#self", func), self_type))
                            func.Where.Clear()
                            func.Return = t

                            ' sub isnull(self: co) Bool
                            '     var next = self.next
                            '     var unexec = (next == 0)
                            '     if unexec then
                            '         var car = self.car
                            '         car()
                            '         next = self.next
                            '     var m1 = -1
                            '     var isend = (next == m1)
                            '     return(isend)
                            Do
                                Dim self = CreateVariableNode("self", func)
                                Dim isnull = New FunctionNode(func.LineNumber.Value) With {.Name = "isnull", .Arguments = New List(Of DeclareNode)}
                                isnull.Arguments.Add(New DeclareNode(self, self_type))
                                isnull.Return = New TypeNode(CreateVariableNode("Bool", func))
                                Dim then_block = New BlockNode(func.LineNumber.Value) With {.Parent = isnull}

                                Dim next_var = CreateVariableNode("next", func)
                                Dim unexec_var = CreateVariableNode("unexec", func)
                                Dim car_var = CreateVariableNode("car", func)
                                Dim m1_var = CreateVariableNode("m1", func)
                                Dim isend_var = CreateVariableNode("isend", func)

                                isnull.Statements.AddRange({
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), True),
                                        CreateLetNode(unexec_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, New NumericNode("0", 0)), True),
                                        CreateIfNode(unexec_var, then_block),
                                        CreateLetNode(m1_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "-"}, New NumericNode("1", 1)), True),
                                        CreateLetNode(isend_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, m1_var), True),
                                        CreateFunctionCallNode(CreateVariableNode("return", func), isend_var)
                                    })
                                then_block.Statements.AddRange({
                                        CreateLetNode(car_var, CreatePropertyNode(self, Nothing, CreateVariableNode("car", func)), True),
                                        CreateFunctionCallNode(car_var),
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), True)
                                    })
                                scope.AddFunction(isnull)

                            Loop While False
                        End If
                    End If
                    next_(child, user)
                End Sub)
        End Sub

    End Class

End Namespace

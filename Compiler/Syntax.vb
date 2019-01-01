﻿Imports System
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
                New With {.Function = CType(Nothing, FunctionNode), .GotoCount = 0},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)
                        If func.Coroutine Then

                            user = New With {.Function = func, .GotoCount = 0}
                            next_(child, user)
                        End If
                    End If

                    If user.Function?.Coroutine AndAlso TypeOf child Is BlockNode Then

                        Dim block = CType(child, BlockNode)
                        Dim program_pointer = 0
                        Dim self = CreateVariableNode("#self", block)
                        block.Lets.Add(self.Name, self)

                        Do While program_pointer < block.Statements.Count

                            Dim v = block.Statements(program_pointer)

                            If TypeOf v Is FunctionCallNode Then

                                Dim fcall = CType(v, FunctionCallNode)
                                If TypeOf fcall.Expression Is VariableNode Then

                                    Dim var = CType(fcall.Expression, VariableNode)
                                    If var.Name.Equals("yield") Then

                                        ' yield(x) =>
                                        '   self.next = N
                                        '   self.value = x
                                        '   return(x)
                                        '   stateN_:
                                        user.GotoCount += 1
                                        block.Statements.RemoveAt(program_pointer)
                                        block.Statements.InsertRange(program_pointer, {
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("next", v)), New NumericNode(user.GotoCount.ToString, CUInt(user.GotoCount))),
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("value", v)), fcall.Arguments(0)),
                                                CreateFunctionCallNode(CreateVariableNode("return", v), fcall.Arguments(0)),
                                                New LabelNode With {.Label = user.GotoCount}
                                            })
                                        program_pointer += 4
                                        Continue Do

                                    ElseIf var.Name.Equals("return") Then

                                        ' return() =>
                                        '   end_:
                                        '   m1 = -1
                                        '   self.next = m1
                                        '   return()
                                        user.GotoCount += 1
                                    End If
                                End If
                            End If

                            program_pointer += 1
                        Loop
                    End If

                    If TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)
                        If func.Coroutine Then

                            Dim p = func.Parent
                            Dim self_type = New TypeNode(CreateVariableNode(func.Name, func))
                            Dim t = func.Where.FindFirst(Function(x) x.Name.Equals("List") AndAlso x.Arguments(0) Is func.Return).Arguments(1)
                            Dim scope = CType(p, IAddFunction)
                            Dim create_type = Function(x As TypeBaseNode) x ' ToDo: dummy
                            Dim local_vars = func.Arguments.Map(Function(x) Tuple.Create(x.Name.Name, x.Type)).ToList

                            ' struct "func.Name"
                            '     var state = 0
                            '     var next  = 0
                            '     var value: "t"
                            '     var args...
                            Dim co = New StructNode(func.LineNumber.Value) With {.Name = func.Name, .Parent = p}
                            co.Lets.Add("state", CreateLetNode(CreateVariableNode("state", func), New NumericNode("0", 0)))
                            co.Lets.Add("next", CreateLetNode(CreateVariableNode("next", func), New NumericNode("0", 0)))
                            co.Lets.Add("value", CreateLetNode(CreateVariableNode("value", func), t))
                            local_vars.Each(Sub(x) co.Lets.Add(x.Item1, CreateLetNode(CreateVariableNode(x.Item1, func), create_type(x.Item2))))
                            p.Lets.Add(co.Name, co)

                            ' sub "func.Name"(args...) "func.Name"
                            '     var self = "func.Name"()
                            '     self.args = args
                            '     return(self)
                            If func.Arguments.Count > 0 Then

                                Dim self = CreateVariableNode("self", func)
                                Dim co2 = New FunctionNode(func.LineNumber.Value) With {.Name = func.Name, .Parent = p, .Arguments = New List(Of DeclareNode)}
                                func.Arguments.Each(Sub(x) co2.Arguments.Add(New DeclareNode(CreateVariableNode(x.Name.Name, func), New TypeNode(CreateVariableNode(x.Type.Name, func)))))
                                co2.Return = self_type

                                co2.Statements.Add(CreateLetNode(self, CreateFunctionCallNode(CreateVariableNode(func.Name, func)), True))
                                co2.Arguments.Each(Sub(x) co2.Statements.Add(CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode(x.Name.Name, func)), x.Name)))
                                co2.Statements.Add(CreateFunctionCallNode(CreateVariableNode("return", func), self))
                                scope.AddFunction(co2)
                            End If

                            ' sub cdr(self: co) [t]
                            '     var next = self.next
                            '     var unexec = (next == 0)
                            '     if unexec then
                            '         var car = self.car
                            '         car()
                            '         next = self.next
                            '     var xs = co()
                            '     xs.state = next
                            '     xs.args = self.args
                            '     return(xs)
                            Do
                                Dim self = CreateVariableNode("self", func)
                                Dim cdr = New FunctionNode(func.LineNumber.Value) With {.Name = "cdr", .Parent = p, .Arguments = New List(Of DeclareNode)}
                                cdr.Arguments.Add(New DeclareNode(self, self_type))
                                cdr.Where.Add(func.Where(0))
                                cdr.Return = func.Return
                                Dim then_block = New BlockNode(func.LineNumber.Value) With {.Parent = cdr}

                                Dim next_var = CreateVariableNode("next", func)
                                Dim unexec_var = CreateVariableNode("unexec", func)
                                Dim car_var = CreateVariableNode("car", func)
                                Dim xs_var = CreateVariableNode("xs", func)

                                cdr.Statements.AddRange({
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), True),
                                        CreateLetNode(unexec_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, New NumericNode("0", 0)), True),
                                        CreateIfNode(unexec_var, then_block),
                                        CreateLetNode(xs_var, CreateFunctionCallNode(CreateVariableNode(func.Name, func))),
                                        CreateLetNode(CreatePropertyNode(xs_var, Nothing, CreateVariableNode("state", func)), next_var)
                                    })
                                then_block.Statements.AddRange({
                                        CreateLetNode(car_var, CreatePropertyNode(self, Nothing, CreateVariableNode("car", func)), True),
                                        CreateFunctionCallNode(car_var),
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), True)
                                    })
                                local_vars.Each(Sub(x) cdr.Statements.Add(CreateLetNode(CreatePropertyNode(xs_var, Nothing, CreateVariableNode(x.Item1, func)), CreatePropertyNode(self, Nothing, CreateVariableNode(x.Item1, func)))))
                                cdr.Statements.AddRange({
                                        CreateFunctionCallNode(CreateVariableNode("return", func), xs_var)
                                    })
                                scope.AddFunction(cdr)

                            Loop While False

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
                                Dim isnull = New FunctionNode(func.LineNumber.Value) With {.Name = "isnull", .Parent = p, .Arguments = New List(Of DeclareNode)}
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

                            ' sub car(self: co) t
                            '     var next = self.next
                            '     var flag = (next >= 1)
                            '     if flag then
                            '         var value = self.value
                            '         return(value)
                            '     var m1 = -1
                            '     flag = (next == m1)
                            '     if flag then goto end_
                            '     var args = self.args
                            '     var state = self.state
                            '     flag = (state == N)
                            '     if flag then goto stateN_
                            '     ...
                            '     m1 = -1
                            '     self.next = m1
                            '     end_:
                            Do
                                Dim self = CType(func.Lets("#self"), VariableNode)
                                func.Name = "car"
                                func.Arguments.Clear()
                                func.Arguments.Add(New DeclareNode(self, self_type))
                                func.Where.Clear()
                                func.Return = t
                                Dim then_block = New BlockNode(func.LineNumber.Value) With {.Parent = func}
                                Dim create_goto =
                                    Function(label As Integer)

                                        Dim block = New BlockNode(func.LineNumber.Value) With {.Parent = func}
                                        block.Statements.Add(New GotoNode With {.Label = label})
                                        Return block
                                    End Function

                                Dim next_var = CreateVariableNode("next", func)
                                Dim flag_var = CreateVariableNode("flag", func)
                                Dim value_var = CreateVariableNode("value", func)
                                Dim m1_var = CreateVariableNode("m1", func)
                                Dim state_var = CreateVariableNode("state", func)

                                Dim stmts As New List(Of IStatementNode) From {
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), True),
                                        CreateLetNode(flag_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = ">="}, next_var, New NumericNode("1", 1)), True),
                                        CreateIfNode(flag_var, then_block),
                                        CreateLetNode(m1_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "-"}, New NumericNode("1", 1)), True),
                                        CreateLetNode(flag_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, m1_var), True),
                                        CreateIfNode(flag_var, create_goto(0))
                                    }
                                local_vars.Each(Sub(x) stmts.Add(CreateLetNode(CreateVariableNode(x.Item1, func), CreatePropertyNode(self, Nothing, CreateVariableNode(x.Item1, func)))))
                                stmts.AddRange({
                                        CreateLetNode(state_var, CreatePropertyNode(self, Nothing, CreateVariableNode("state", func)), True)
                                    })
                                For i = 1 To user.GotoCount

                                    stmts.AddRange({
                                            CreateLetNode(flag_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, state_var, New NumericNode(i.ToString, CUInt(i))), True),
                                            CreateIfNode(flag_var, create_goto(i))
                                        })
                                Next
                                func.Statements.InsertRange(0, stmts)
                                func.Statements.AddRange({
                                        CreateLetNode(m1_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "-"}, New NumericNode("1", 1)), True),
                                        CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), m1_var),
                                        New LabelNode With {.Label = 0}
                                    })
                                then_block.Statements.AddRange({
                                        CreateLetNode(value_var, CreatePropertyNode(self, Nothing, CreateVariableNode("value", func)), True),
                                        CreateFunctionCallNode(CreateVariableNode("return", func), value_var)
                                    })
                            Loop While False

                            Return
                        End If
                    End If
                    next_(child, user)
                End Sub)
        End Sub

    End Class

End Namespace

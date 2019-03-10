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
                        If TypeOf last.Expression Is FunctionCallNode AndAlso CType(last.Expression, FunctionCallNode).OwnerSwitchNode IsNot Nothing Then

                            ' if $ret else break
                            case_value.Value.Statements.Add(CreateIfNode(CreateVariableNode("$ret", case_value), Nothing, ToBlock(user.Scope, New BreakNode)))
                        Else

                            ' $case1 = $ret == switch.Expression
                            ' if $case1 else break
                            Dim s1 = CreateVariableNode($"$case{user.VarIndex}", case_value)
                            case_value.Value.Statements.Add(CreateLetNode(s1, CreateFunctionCallNode(CreateVariableNode("==", case_value), CreateVariableNode("$ret", case_value), switch.Expression), True, False))
                            case_value.Value.Statements.Add(CreateIfNode(s1, Nothing, ToBlock(user.Scope, New BreakNode)))
                        End If

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
                            case_array.Statements.Add(CreateLetNode(s1, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("isnull", case_array)), True, False))
                            case_array.Statements.Add(CreateLetNode(s2, CreateFunctionCallNode(s1), True, False))
                            case_array.Statements.Add(CreateIfNode(s2, Nothing, ToBlock(user.Scope, New BreakNode)))
                        Else

                            ' $s1 = switch.Expression.isnull
                            ' $s2 = $s1()
                            ' if $s2 then break
                            Dim s1 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            Dim s2 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s1, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("isnull", case_array)), True, False))
                            case_array.Statements.Add(CreateLetNode(s2, CreateFunctionCallNode(s1), True, False))
                            case_array.Statements.Add(CreateIfNode(s2, ToBlock(user.Scope, New BreakNode)))

                            ' $s3 = switch.Expression.car
                            ' case_array.Pattern[0] = $s3()
                            Dim s3 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s3, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("car", case_array)), True, False))
                            case_array.Statements.Add(CreateLetNode(case_array.Pattern(0), CreateFunctionCallNode(s3), True, False))

                            ' $s4 = switch.Expression.cdr
                            ' $s5 = $s4()
                            Dim s4 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            Dim s5 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                            user.VarIndex += 1
                            case_array.Statements.Add(CreateLetNode(s4, CreatePropertyNode(switch.Expression, Nothing, CreateVariableNode("cdr", case_array)), True, False))
                            case_array.Statements.Add(CreateLetNode(s5, CreateFunctionCallNode(s4), True, False))

                            If case_array.Pattern.Count = 1 Then

                                ' $s6 = $s5.isnull
                                ' $s7 = $s6()
                                Dim s6 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                user.VarIndex += 1
                                Dim s7 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                user.VarIndex += 1
                                case_array.Statements.Add(CreateLetNode(s6, CreatePropertyNode(s5, Nothing, CreateVariableNode("isnull", case_array)), True, False))
                                case_array.Statements.Add(CreateLetNode(s7, CreateFunctionCallNode(s6), True, False))

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
                                    case_array.Statements.Add(CreateLetNode(s6, CreatePropertyNode(s5, Nothing, CreateVariableNode("isnull", case_array)), True, False))
                                    case_array.Statements.Add(CreateLetNode(s7, CreateFunctionCallNode(s6), True, False))

                                    ' if $s7 then break
                                    case_array.Statements.Add(CreateIfNode(s7, ToBlock(user.Scope, New BreakNode)))

                                    ' $s8 = $s5.car
                                    ' case_array.Pattern[i] = $s7()
                                    Dim s8 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s8, CreatePropertyNode(s5, Nothing, CreateVariableNode("car", case_array)), True, False))
                                    case_array.Statements.Add(CreateLetNode(case_array.Pattern(i), CreateFunctionCallNode(s8), True, False))

                                    ' $s9 = $s5.cdr
                                    ' $s10 = $s9()
                                    Dim s9 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    Dim s10 = CreateVariableNode($"$s{user.VarIndex}", case_array)
                                    user.VarIndex += 1
                                    case_array.Statements.Add(CreateLetNode(s9, CreatePropertyNode(s5, Nothing, CreateVariableNode("cdr", case_array)), True, False))
                                    case_array.Statements.Add(CreateLetNode(s10, CreateFunctionCallNode(s9), True, False))

                                    s5 = s10
                                Next

                                ' case_array.Pattern[N] = next
                                case_array.Statements.Add(CreateLetNode(case_array.Pattern(case_array.Pattern.Count - 1), s5, True, False))
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

        Public Shared Function CreateLocalVariable(name As String, scope As IScopeNode) As VariableNode

            If scope.Lets.ContainsKey(name) Then Return CType(scope.Lets(name), VariableNode)
            Dim x = CreateVariableNode(name, scope)
            x.Scope = scope
            scope.Lets.Add(name, x)
            Return x
        End Function

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
                        Dim self = CreateLocalVariable("#self", CType(block.Owner, IScopeNode))

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
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("next", v)), New NumericNode(user.GotoCount.ToString, CUInt(user.GotoCount)), False),
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("value", v)), fcall.Arguments(0), False),
                                                CreateFunctionCallNode(CreateVariableNode("return", v), fcall.Arguments(0)),
                                                New LabelNode With {.Label = user.GotoCount}
                                            })
                                        program_pointer += 4
                                        Continue Do

                                    ElseIf var.Name.Equals("yields") Then

                                        ' yields(xs) =>
                                        '   var x0 = xs
                                        '   var x1 = isnull(x0)
                                        '   var x2 = ! x1
                                        '   if x2
                                        '       self.next = N
                                        '       var x3 = car(x0)
                                        '       self.value = x3
                                        '       var valuesN = cdr(x0) # user-definition
                                        '       return(x3)
                                        '       stateN_:
                                        '       var x4 = valuesN
                                        '       x1 = isnull(x4)
                                        '       x2 = ! x1
                                        '       if x2
                                        '           self.next = N
                                        '           x3 = car(x4)
                                        '           self.value = x3
                                        '           valuesN = cdr(x4) # user-definition
                                        '           return(x3)
                                        Dim then_block = New BlockNode(block.LineNumber.Value) With {.Parent = block}
                                        Dim then_block2 = New BlockNode(block.LineNumber.Value) With {.Parent = then_block}
                                        Dim x0 = CreateLocalVariable($"#x{program_pointer + 0}", block)
                                        Dim x1 = CreateLocalVariable($"#x{program_pointer + 1}", block)
                                        Dim x2 = CreateLocalVariable($"#x{program_pointer + 2}", block)
                                        Dim x3 = CreateLocalVariable($"#x{program_pointer + 3}", then_block)
                                        Dim x4 = CreateLocalVariable($"#x{program_pointer + 4}", then_block)
                                        Dim valuesN = CreateLocalVariable($"#values{user.GotoCount + 1}", then_block)

                                        user.GotoCount += 1
                                        block.Statements.RemoveAt(program_pointer)
                                        block.Statements.InsertRange(program_pointer, {
                                                CreateLetNode(x0, fcall.Arguments(0), False, False),
                                                CreateLetNode(x1, CreateFunctionCallNode(CreateVariableNode("isnull", v), x0), False, False),
                                                CreateLetNode(x2, CreateFunctionCallNode(CreateVariableNode("!", v), x1), False, False),
                                                CreateIfNode(x2, then_block)
                                            })
                                        then_block.Statements.AddRange({
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("next", v)), New NumericNode(user.GotoCount.ToString, CUInt(user.GotoCount)), False),
                                                CreateLetNode(x3, CreateFunctionCallNode(CreateVariableNode("car", v), x0), False, False),
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("value", v)), x3, False),
                                                CreateLetNode(valuesN, CreateFunctionCallNode(CreateVariableNode("cdr", v), x0)),
                                                CreateFunctionCallNode(CreateVariableNode("return", v), x3),
                                                New LabelNode With {.Label = user.GotoCount},
                                                CreateLetNode(x4, valuesN, False, False),
                                                CreateLetNode(x1, CreateFunctionCallNode(CreateVariableNode("isnull", v), x4), False, False),
                                                CreateLetNode(x2, CreateFunctionCallNode(CreateVariableNode("!", v), x1), False, False),
                                                CreateIfNode(x2, then_block2)
                                            })
                                        then_block2.Statements.AddRange({
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("next", v)), New NumericNode(user.GotoCount.ToString, CUInt(user.GotoCount)), False),
                                                CreateLetNode(x3, CreateFunctionCallNode(CreateVariableNode("car", v), x4), False, False),
                                                CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("value", v)), x3, False),
                                                CreateLetNode(valuesN, CreateFunctionCallNode(CreateVariableNode("cdr", v), x4)),
                                                CreateFunctionCallNode(CreateVariableNode("return", v), x3)
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
                            Dim local_vars = func.Arguments.Map(Function(x) Tuple.Create(x.Name.Name, x.Type)).ToList

                            ' struct "func.Name"
                            '     var state = 0
                            '     var next  = 0
                            '     var value: "t"
                            '     var args...
                            Dim co = New StructNode(func.LineNumber.Value) With {.Name = func.Name, .Parent = p}
                            co.Lets.Add("state", CreateLetNode(CreateVariableNode("state", co), New NumericNode("0", 0)))
                            co.Lets.Add("next", CreateLetNode(CreateVariableNode("next", co), New NumericNode("0", 0)))
                            co.Lets.Add("value", CreateLetNode(CreateVariableNode("value", co), t))
                            local_vars.Each(Sub(x) co.Lets.Add(x.Item1, CreateLetNode(CreateVariableNode(x.Item1, co), x.Item2)))
                            p.Lets.Add(co.Name, co)

                            ' sub "func.Name"(args...) "func.Name"
                            '     var self = "func.Name"()
                            '     self.args = args
                            '     return(self)
                            If func.Arguments.Count > 0 Then

                                Dim co2 = New FunctionNode(func.LineNumber.Value) With {.Name = func.Name, .Parent = p, .Arguments = New List(Of DeclareNode)}
                                Dim self = CreateLocalVariable("self", co2)
                                func.Arguments.Each(Sub(x) co2.Arguments.Add(New DeclareNode(CreateVariableNode(x.Name.Name, co2), New TypeNode(CreateVariableNode(x.Type.Name, co2)))))
                                co2.Return = self_type

                                co2.Statements.Add(CreateLetNode(self, CreateFunctionCallNode(CreateVariableNode(func.Name, co2)), True, False))
                                co2.Arguments.Each(Sub(x) co2.Statements.Add(CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode(x.Name.Name, co2)), x.Name, False)))
                                co2.Statements.Add(CreateFunctionCallNode(CreateVariableNode("return", co2), self))
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
                            '     xs.local_var = self.local_var # mark-only
                            '     return(xs)
                            Do
                                Dim cdr = New FunctionNode(func.LineNumber.Value) With {.Name = "cdr", .Parent = p, .Arguments = New List(Of DeclareNode)}
                                Dim self = CreateLocalVariable("self", cdr)
                                cdr.Arguments.Add(New DeclareNode(self, self_type))
                                cdr.Where.Add(func.Where(0))
                                cdr.Return = func.Return
                                Dim then_block = New BlockNode(cdr.LineNumber.Value) With {.Parent = cdr}

                                Dim next_var = CreateLocalVariable("next", cdr)
                                Dim unexec_var = CreateLocalVariable("unexec", cdr)
                                Dim car_var = CreateLocalVariable("car", cdr)
                                Dim xs_var = CreateLocalVariable("xs", cdr)

                                cdr.Statements.AddRange({
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", cdr)), True, False),
                                        CreateLetNode(unexec_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, New NumericNode("0", 0)), True, False),
                                        CreateIfNode(unexec_var, then_block),
                                        CreateLetNode(xs_var, CreateFunctionCallNode(CreateVariableNode(func.Name, cdr)), True, False),
                                        CreateLetNode(CreatePropertyNode(xs_var, Nothing, CreateVariableNode("state", cdr)), next_var, False)
                                    })
                                then_block.Statements.AddRange({
                                        CreateLetNode(car_var, CreatePropertyNode(self, Nothing, CreateVariableNode("car", cdr)), True, False),
                                        CreateFunctionCallNode(car_var),
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", cdr)), True, False)
                                    })
                                local_vars.Each(Sub(x) cdr.Statements.Add(CreateLetNode(CreatePropertyNode(xs_var, Nothing, CreateVariableNode(x.Item1, cdr)), CreatePropertyNode(self, Nothing, CreateVariableNode(x.Item1, func)), False)))
                                cdr.Statements.AddRange({
                                        New LabelNode With {.Label = -1},
                                        CreateFunctionCallNode(CreateVariableNode("return", cdr), xs_var)
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
                                Dim isnull = New FunctionNode(func.LineNumber.Value) With {.Name = "isnull", .Parent = p, .Arguments = New List(Of DeclareNode)}
                                Dim self = CreateLocalVariable("self", isnull)
                                isnull.Arguments.Add(New DeclareNode(self, self_type))
                                isnull.Return = New TypeNode(CreateVariableNode("Bool", isnull))
                                Dim then_block = New BlockNode(isnull.LineNumber.Value) With {.Parent = isnull}

                                Dim next_var = CreateLocalVariable("next", isnull)
                                Dim unexec_var = CreateLocalVariable("unexec", isnull)
                                Dim car_var = CreateLocalVariable("car", isnull)
                                Dim m1_var = CreateLocalVariable("m1", isnull)
                                Dim isend_var = CreateLocalVariable("isend", isnull)

                                isnull.Statements.AddRange({
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", isnull)), True, False),
                                        CreateLetNode(unexec_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, New NumericNode("0", 0)), True, False),
                                        CreateIfNode(unexec_var, then_block),
                                        CreateLetNode(m1_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "-"}, New NumericNode("1", 1)), True, False),
                                        CreateLetNode(isend_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, m1_var), True, False),
                                        CreateFunctionCallNode(CreateVariableNode("return", isnull), isend_var)
                                    })
                                then_block.Statements.AddRange({
                                        CreateLetNode(car_var, CreatePropertyNode(self, Nothing, CreateVariableNode("car", isnull)), True, False),
                                        CreateFunctionCallNode(car_var),
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", isnull)), True, False)
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
                            '     var local_var = self.local_var # mark-only
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

                                Dim next_var = CreateLocalVariable("#next", func)
                                Dim flag_var = CreateLocalVariable("#flag", func)
                                Dim value_var = CreateLocalVariable("#value", func)
                                Dim m1_var = CreateLocalVariable("#m1", func)
                                Dim state_var = CreateLocalVariable("#state", func)

                                Dim stmts As New List(Of IStatementNode) From {
                                        CreateLetNode(next_var, CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), True, False),
                                        CreateLetNode(flag_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = ">="}, next_var, New NumericNode("1", 1)), True, False),
                                        CreateIfNode(flag_var, then_block),
                                        CreateLetNode(m1_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "-"}, New NumericNode("1", 1)), True, False),
                                        CreateLetNode(flag_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, next_var, m1_var), True, False),
                                        CreateIfNode(flag_var, create_goto(0))
                                    }
                                local_vars.Each(Sub(x) stmts.Add(CreateLetNode(CType(func.Lets(x.Item1), VariableNode), CreatePropertyNode(self, Nothing, CreateVariableNode(x.Item1, func)), True, False)))
                                stmts.AddRange({
                                        New LabelNode With {.Label = -1},
                                        CreateLetNode(state_var, CreatePropertyNode(self, Nothing, CreateVariableNode("state", func)), True, False)
                                    })
                                For i = 1 To user.GotoCount

                                    stmts.AddRange({
                                            CreateLetNode(flag_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "=="}, state_var, New NumericNode(i.ToString, CUInt(i))), True, False),
                                            CreateIfNode(flag_var, create_goto(i))
                                        })
                                Next
                                func.Statements.InsertRange(0, stmts)
                                func.Statements.AddRange({
                                        CreateLetNode(m1_var, CreateFunctionCallNode(New Token(SymbolTypes.OPE) With {.Name = "-"}, New NumericNode("1", 1)), True, False),
                                        CreateLetNode(CreatePropertyNode(self, Nothing, CreateVariableNode("next", func)), m1_var, False),
                                        New LabelNode With {.Label = 0}
                                    })
                                then_block.Statements.AddRange({
                                        CreateLetNode(value_var, CreatePropertyNode(self, Nothing, CreateVariableNode("value", func)), True, False),
                                        CreateFunctionCallNode(CreateVariableNode("return", func), value_var)
                                    })
                            Loop While False

                            Return
                        End If
                    End If
                    next_(child, user)
                End Sub)
        End Sub

        Public Shared Sub CoroutineLocalCapture(pgm As ProgramNode, root As SystemLibrary, ns As RkNamespace)

            Util.Traverse.NodesOnce(
                pgm,
                New With {.Function = CType(Nothing, FunctionNode), .LocalVars = CType(Nothing, Dictionary(Of String, LetNode))},
                Sub(parent, ref, child, user, isfirst, next_)

                    If Not isfirst Then Return

                    If TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)
                        If func.Coroutine Then

                            user = New With {.Function = func, .LocalVars = New Dictionary(Of String, LetNode)}
                            next_(child, user)
                        End If
                    End If

                    If user.Function?.Coroutine AndAlso TypeOf child Is BlockNode Then

                        ' sub car(self: co) t
                        '     var local_var = ... =>
                        '       self.local_var = ...
                        Dim block = CType(child, BlockNode)
                        Dim self = user.Function.Arguments(0).Name
                        Dim program_pointer = 0

                        Do While program_pointer < block.Statements.Count

                            Dim v = block.Statements(program_pointer)

                            If TypeOf v Is LetNode Then

                                Dim let_ = CType(v, LetNode)
                                If let_.UserDefinition AndAlso let_.Receiver Is Nothing Then

                                    let_.Var.UniqueName = $"{let_.Var.Name}:{let_.Var.Scope.LineNumber}"
                                    let_.Receiver = self
                                    If Not user.LocalVars.ContainsKey(let_.Var.UniqueName) Then user.LocalVars.Add(let_.Var.UniqueName, let_)
                                End If
                            End If

                            program_pointer += 1
                        Loop
                    End If

                    If TypeOf child Is FunctionNode Then

                        Dim func = CType(child, FunctionNode)

                        If func.Coroutine Then

                            Dim p = func.Parent
                            Dim scope = CType(p, IAddFunction)

                            ' struct "func.Name"
                            '     var local_var...
                            Dim co = CType(func.Arguments(0).Type.Type, RkStruct)
                            user.LocalVars.Each(Sub(kv) co.Local.Add(kv.Key, kv.Value.Type))

                            ' sub cdr(self: co) [t]
                            '     mark =>
                            '       xs.local_var = self.local_var
                            Do
                                Dim cdr = scope.Functions.FindFirst(Function(x) x.Name.Equals("cdr") AndAlso x.Arguments.Count = 1 AndAlso x.Arguments(0).Type.Type.Is(func.Arguments(0).Type.Type))
                                Dim self = cdr.Arguments(0).Name
                                Dim xs_var = CType(cdr.Lets("xs"), VariableNode)
                                Dim index = cdr.Statements.IndexOf(Function(x) TypeOf x Is LabelNode AndAlso CType(x, LabelNode).Label = -1)

                                cdr.Statements.RemoveAt(index)
                                user.LocalVars.Each(
                                    Sub(kv)

                                        Dim t = kv.Value.Type
                                        Dim left = CreatePropertyNode(xs_var, Nothing, CreateVariableNode(kv.Key, cdr))
                                        Dim right = CreatePropertyNode(self, Nothing, CreateVariableNode(kv.Key, cdr))
                                        left.Right.Type = t
                                        left.Type = t
                                        right.Right.Type = t
                                        right.Type = t
                                        Dim let_ = CreateLetNode(left, right, False)
                                        let_.Type = t
                                        cdr.Statements.Insert(index, let_)
                                    End Sub)

                            Loop While False

                            ' sub car(self: co) t
                            '     mark =>
                            '       var local_var = self.local_var
                            Do
                                Dim self = func.Arguments(0).Name
                                Dim index = func.Statements.IndexOf(Function(x) TypeOf x Is LabelNode AndAlso CType(x, LabelNode).Label = -1)

                                func.Statements.RemoveAt(index)
                                user.LocalVars.Each(
                                    Sub(kv)

                                        Dim t = kv.Value.Type
                                        Dim left = kv.Value.Var
                                        Dim right = CreatePropertyNode(self, Nothing, CreateVariableNode(kv.Key, func))
                                        right.Right.Type = t
                                        right.Type = t
                                        Dim let_ = CreateLetNode(left, right, True, False)
                                        let_.Type = t
                                        func.Statements.Insert(index, let_)
                                    End Sub)

                            Loop While False

                            Return
                        End If
                    End If
                    next_(child, user)
                End Sub)
        End Sub

    End Class

End Namespace

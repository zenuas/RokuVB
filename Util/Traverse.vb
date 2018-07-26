Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Node
Imports Roku.Util.TypeHelper
Imports System.Diagnostics


Namespace Util

    Public Class Traverse

        Public Shared Iterator Function Fields(
                x As Object,
                Optional flag As BindingFlags = BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.GetField
            ) As IEnumerable(Of Tuple(Of Object, FieldInfo))

            If x Is Nothing Then Return

            If TypeOf x Is Array Then

                Dim xs = CType(x, Array)
                For i = 0 To xs.Length - 1

                    Yield New Tuple(Of Object, FieldInfo)(xs.GetValue(i), Nothing)
                Next
            Else
                Dim t = x.GetType
                For Each m In t.GetFields(flag)

                    Yield Tuple.Create(m.GetValue(x), m)
                Next

                If (flag And BindingFlags.NonPublic) = BindingFlags.NonPublic Then

                    flag = flag And Not BindingFlags.Public
                    Do While t.BaseType IsNot Nothing

                        t = t.BaseType

                        For Each m In t.GetFields(flag)

                            Yield Tuple.Create(m.GetValue(x), m)
                        Next
                    Loop
                End If
            End If

        End Function

        Public Shared Iterator Function Properties(
                x As Object,
                Optional flag As BindingFlags = BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.GetProperty
            ) As IEnumerable(Of Tuple(Of Object, PropertyInfo))

            If x Is Nothing Then Return

            For Each m In x.GetType.GetProperties(flag)

                Yield Tuple.Create(m.GetValue(x), m)
            Next

        End Function

        Public Delegate Sub NodesNext(Of T)(node As INode, user As T)
        Public Delegate Sub NodesCallback(Of T)(parent As INode, ref As String, node As INode, user As T, next_ As NodesNext(Of T))
        Public Delegate Function NodesReplaceCallback(Of T)(parent As INode, ref As String, node As INode, user As T, next_ As NodesNext(Of T)) As INode

        Public Shared Sub NodesOnce(Of T)(
                node As INode,
                user As T,
                callback As Action(Of INode, String, INode, T, Boolean, NodesNext(Of T))
            )

            Dim dummy_next = Sub(node_ As INode, user_ As T) Return

            Dim mark As New Dictionary(Of Integer, Boolean)
            Traverse.Nodes(
                node,
                user,
                Sub(parent, ref, child, user_, next_)

                    Dim child_hash = child.GetHashCode
                    Dim isfirst = Not mark.ContainsKey(child_hash)
                    If isfirst Then mark.Add(child_hash, True)
                    callback(parent, ref, child, user_, isfirst, If(isfirst, next_, dummy_next))
                End Sub)
        End Sub

        Public Shared Sub NodesReplaceOnce(Of T)(
                node As INode,
                user As T,
                callback As Func(Of INode, String, INode, T, Boolean, NodesNext(Of T), INode)
            )

            Dim dummy_next = Sub(node_ As INode, user_ As T) Return

            Dim mark As New Dictionary(Of Integer, Boolean)
            Traverse.NodesReplace(
                node,
                user,
                Function(parent, ref, child, user_, next_)

                    Dim child_hash = child.GetHashCode
                    Dim isfirst = Not mark.ContainsKey(child_hash)
                    If isfirst Then mark.Add(child_hash, True)
                    Return callback(parent, ref, child, user_, isfirst, If(isfirst, next_, dummy_next))
                End Function)
        End Sub

        Public Shared Sub Nodes(Of T)(
                node As INode,
                user As T,
                callback As NodesCallback(Of T)
            )

            If node Is Nothing Then Return

            Dim next_node As NodesNext(Of T) =
                Sub(node_ As INode, user_ As T)

                    If node_ Is Nothing Then Return

                    Dim f =
                        Sub(ref As String, v As INode)

                            If v Is Nothing Then Return
                            callback(node_, ref, v, user_, next_node)
                        End Sub

                    Select Case True

                        Case TypeOf node_ Is StructNode

                            Dim x = CType(node_, StructNode)
                            f("Owner", x.Owner)
                            For Each key In x.Lets.Keys

                                f($"`{key}", x.Lets(key))
                            Next

                        Case TypeOf node_ Is ProgramNode

                            Dim x = CType(node_, ProgramNode)
                            For Each value In x.FixedGenericFunction.Values.ToList

                                f($"`{value.Name}", value)
                            Next
                            GoTo BLOCK_NODE_

                        Case TypeOf node_ Is FunctionNode

                            Dim x = CType(node_, FunctionNode)
                            For i = 0 To x.Arguments.Count - 1

                                f($"[{i}]", x.Arguments(i))
                            Next
                            f("Return", x.Return)
                            GoTo BLOCK_NODE_

                        Case TypeOf node_ Is BlockNode

BLOCK_NODE_:
                            Dim x = CType(node_, BlockNode)
                            f("Owner", x.Owner)
                            For i = 0 To x.Statements.Count - 1

                                f($"[{i}]", x.Statements(i))
                            Next

                            For Each key In x.Lets.Keys

                                f($"`{key}", x.Lets(key))
                            Next

                            For i = 0 To x.Functions.Count - 1

                                f($"`{x.Functions(i).Name}", x.Functions(i))
                            Next

                        Case TypeOf node_ Is LetNode

                            Dim x = CType(node_, LetNode)
                            f("Receiver", x.Receiver)
                            f("Var", x.Var)
                            f("Declare", x.Declare)
                            f("Expression", x.Expression)

                        Case TypeOf node_ Is ExpressionNode

                            Dim x = CType(node_, ExpressionNode)
                            f("Left", x.Left)
                            f("Right", x.Right)

                        Case TypeOf node_ Is PropertyNode

                            Dim x = CType(node_, PropertyNode)
                            f("Left", x.Left)
                            f("Right", x.Right)

                        Case TypeOf node_ Is TupleNode

                            Dim x = CType(node_, TupleNode)
                            For i = 0 To x.Items.Length - 1

                                f($"[{i}]", x.Items(i))
                            Next

                        Case TypeOf node_ Is FunctionCallNode

                            Dim x = CType(node_, FunctionCallNode)
                            f("Expression", x.Expression)
                            For i = 0 To x.Arguments.Length - 1

                                f($"[{i}]", x.Arguments(i))
                            Next

                        Case TypeOf node_ Is DeclareNode

                            Dim x = CType(node_, DeclareNode)
                            f("Name", x.Name)
                            f("Type", x.Type)

                        Case TypeOf node_ Is IfCastNode

                            Dim x = CType(node_, IfCastNode)
                            f("Var", x.Var)
                            f("Declare", x.Declare)
                            f("Condition", x.Condition)
                            f("Then", x.Then)
                            f("Else", x.Else)

                        Case TypeOf node_ Is IfNode

                            Dim x = CType(node_, IfNode)
                            f("Condition", x.Condition)
                            f("Then", x.Then)
                            f("Else", x.Else)

                        Case TypeOf node_ Is SwitchNode

                            Dim x = CType(node_, SwitchNode)
                            f("Expression", x.Expression)
                            For i = 0 To x.Case.Count - 1

                                f($"[{i}]", x.Case(i))
                            Next

                        Case TypeOf node_ Is CaseCastNode

                            Dim x = CType(node_, CaseCastNode)
                            f("Declare", x.Declare)
                            f("Var", x.Var)
                            f("Then", x.Then)

                        Case TypeOf node_ Is CaseArrayNode

                            Dim x = CType(node_, CaseArrayNode)
                            For i = 0 To x.Pattern.Count - 1

                                f($"[{i}]", x.Pattern(i))
                            Next
                            For i = 0 To x.Statements.Count - 1

                                f($"Statements[{i}]", x.Statements(i))
                            Next
                            f("Then", x.Then)

                        Case TypeOf node_ Is LambdaExpressionNode

                            Dim x = CType(node_, LambdaExpressionNode)
                            f("Expression", x.Expression)

                        Case TypeOf node_ Is TypeFunctionNode

                            Dim x = CType(node_, TypeFunctionNode)
                            For i = 0 To x.Arguments.Count - 1

                                f($"[{i}]", x.Arguments(i))
                            Next
                            f("Return", x.Return)

                        Case TypeOf node_ Is TypeArrayNode

                            Dim x = CType(node_, TypeArrayNode)
                            f("Item", x.Item)

                        Case TypeOf node_ Is TypeTupleNode

                            Dim x = CType(node_, TypeTupleNode)
                            For i = 0 To x.Items.List.Count - 1

                                f($"[{i}]", x.Items.List(i))
                            Next

                        Case TypeOf node_ Is TypeNode

                            Dim x = CType(node_, TypeNode)
                            f("Namespace", x.Namespace)
                            For i = 0 To x.Arguments.Count - 1

                                f($"[{i}]", x.Arguments(i))
                            Next

                        Case TypeOf node_ Is UnionNode

                            Dim x = CType(node_, UnionNode)
                            For i = 0 To x.Union.List.Count - 1

                                f($"[{i}]", x.Union.List(i))
                            Next

                        Case TypeOf node_ Is VariableNode,
                             TypeOf node_ Is NumericNode,
                             TypeOf node_ Is StringNode,
                             TypeOf node_ Is NullNode,
                             TypeOf node_ Is BreakNode

                            ' nothing

                        Case TypeOf node_ Is RootNode

                            Dim x = CType(node_, RootNode)
                            For Each key In x.Namespaces.Keys

                                f($"`{key}", x.Namespaces(key))
                            Next

                        Case IsGeneric(node_.GetType, GetType(ListNode(Of )))

                            Dim list = node_.GetType.GetProperty("List").GetValue(node_)
                            Dim count = list.GetType.GetProperty("Count")
                            Dim item = list.GetType.GetProperty("Item")
                            Dim index = New Object() {0}
                            For i = 0 To CInt(count.GetValue(list)) - 1

                                index(0) = i
                                f($"`{i}", CType(item.GetValue(list, index), INode))
                            Next

                        Case Else

                            Debug.Fail("unknown node")
                    End Select
                End Sub

            callback(Nothing, "", node, user, next_node)
        End Sub

        Public Shared Sub NodesReplace(Of T)(
                node As INode,
                user As T,
                callback As NodesReplaceCallback(Of T)
            )

            If node Is Nothing Then Return

            Dim next_node As NodesNext(Of T) =
                Sub(node_ As INode, user_ As T)

                    If node_ Is Nothing Then Return

                    Dim f =
                        Function(ref As String, v As INode)

                            If v Is Nothing Then Return Nothing
                            Return callback(node_, ref, v, user_, next_node)
                        End Function

                    Select Case True

                        Case TypeOf node_ Is StructNode

                            Dim x = CType(node_, StructNode)
                            'x.Owner = CType(f("Owner", x.Owner), INamedFunction)
                            For Each key In x.Lets.Keys.ToList

                                x.Lets(key) = f($"`{key}", x.Lets(key))
                            Next

                        Case TypeOf node_ Is ProgramNode

                            Dim x = CType(node_, ProgramNode)
                            For Each key In x.FixedGenericFunction.Keys.ToList

                                Dim value = x.FixedGenericFunction(key)
                                x.FixedGenericFunction(key) = CType(f($"`{value.Name}", value), FunctionNode)
                            Next
                            GoTo BLOCK_NODE_

                        Case TypeOf node_ Is FunctionNode

                            Dim x = CType(node_, FunctionNode)
                            For i = 0 To x.Arguments.Count - 1

                                x.Arguments(i) = CType(f($"[{i}]", x.Arguments(i)), DeclareNode)
                            Next
                            x.Return = CType(f("Return", x.Return), TypeNode)
                            GoTo BLOCK_NODE_

                        Case TypeOf node_ Is BlockNode

BLOCK_NODE_:
                            Dim x = CType(node_, BlockNode)
                            'x.Owner = CType(f("Owner", x.Owner), INamedFunction)
                            For i = 0 To x.Statements.Count - 1

                                x.Statements(i) = CType(f($"[{i}]", x.Statements(i)), IStatementNode)
                            Next

                            For Each key In x.Lets.Keys.ToList

                                x.Lets(key) = f($"`{key}", x.Lets(key))
                            Next

                            For i = 0 To x.Functions.Count - 1

                                x.Functions(i) = CType(f($"`{x.Functions(i).Name}", x.Functions(i)), FunctionNode)
                            Next

                        Case TypeOf node_ Is LetNode

                            Dim x = CType(node_, LetNode)
                            x.Receiver = CType(f("Receiver", x.Receiver), IEvaluableNode)
                            x.Var = CType(f("Var", x.Var), VariableNode)
                            x.Declare = CType(f("Declare", x.Declare), TypeNode)
                            x.Expression = CType(f("Expression", x.Expression), IEvaluableNode)

                        Case TypeOf node_ Is ExpressionNode

                            Dim x = CType(node_, ExpressionNode)
                            x.Left = CType(f("Left", x.Left), IEvaluableNode)
                            x.Right = CType(f("Right", x.Right), IEvaluableNode)

                        Case TypeOf node_ Is PropertyNode

                            Dim x = CType(node_, PropertyNode)
                            x.Left = CType(f("Left", x.Left), IEvaluableNode)
                            x.Right = CType(f("Right", x.Right), VariableNode)

                        Case TypeOf node_ Is TupleNode

                            Dim x = CType(node_, TupleNode)
                            For i = 0 To x.Items.Length - 1

                                x.Items(i) = CType(f($"[{i}]", x.Items(i)), IEvaluableNode)
                            Next

                        Case TypeOf node_ Is FunctionCallNode

                            Dim x = CType(node_, FunctionCallNode)
                            x.Expression = CType(f("Expression", x.Expression), IEvaluableNode)
                            For i = 0 To x.Arguments.Length - 1

                                x.Arguments(i) = CType(f($"[{i}]", x.Arguments(i)), IEvaluableNode)
                            Next

                        Case TypeOf node_ Is DeclareNode

                            Dim x = CType(node_, DeclareNode)
                            x.Name = CType(f("Name", x.Name), VariableNode)
                            x.Type = CType(f("Type", x.Type), TypeNode)

                        Case TypeOf node_ Is IfCastNode

                            Dim x = CType(node_, IfCastNode)
                            x.Var = CType(f("Var", x.Var), VariableNode)
                            x.Declare = CType(f("Declare", x.Declare), TypeNode)
                            x.Condition = CType(f("Condition", x.Condition), IEvaluableNode)
                            x.Then = CType(f("Then", x.Then), BlockNode)
                            x.Else = CType(f("Else", x.Else), BlockNode)

                        Case TypeOf node_ Is IfNode

                            Dim x = CType(node_, IfNode)
                            x.Condition = CType(f("Condition", x.Condition), IEvaluableNode)
                            x.Then = CType(f("Then", x.Then), BlockNode)
                            x.Else = CType(f("Else", x.Else), BlockNode)

                        Case TypeOf node_ Is SwitchNode

                            Dim x = CType(node_, SwitchNode)
                            x.Expression = CType(f("Expression", x.Expression), IEvaluableNode)
                            For i = 0 To x.Case.Count - 1

                                x.Case(i) = CType(f($"[{i}]", x.Case(i)), CaseNode)
                            Next

                        Case TypeOf node_ Is CaseCastNode

                            Dim x = CType(node_, CaseCastNode)
                            x.Declare = CType(f("Declare", x.Declare), TypeNode)
                            x.Var = CType(f("Var", x.Var), VariableNode)
                            x.Then = CType(f("Then", x.Then), BlockNode)

                        Case TypeOf node_ Is CaseArrayNode

                            Dim x = CType(node_, CaseArrayNode)
                            For i = 0 To x.Pattern.Count - 1

                                x.Pattern(i) = CType(f($"[{i}]", x.Pattern(i)), VariableNode)
                            Next
                            For i = 0 To x.Statements.Count - 1

                                x.Statements(i) = CType(f($"Statements[{i}]", x.Statements(i)), IStatementNode)
                            Next
                            x.Then = CType(f("Then", x.Then), BlockNode)

                        Case TypeOf node_ Is LambdaExpressionNode

                            Dim x = CType(node_, LambdaExpressionNode)
                            x.Expression = CType(f("Expression", x.Expression), IEvaluableNode)

                        Case TypeOf node_ Is TypeFunctionNode

                            Dim x = CType(node_, TypeFunctionNode)
                            For i = 0 To x.Arguments.Count - 1

                                x.Arguments(i) = CType(f($"[{i}]", x.Arguments(i)), TypeBaseNode)
                            Next
                            x.Return = CType(f("Return", x.Return), TypeBaseNode)

                        Case TypeOf node_ Is TypeArrayNode

                            Dim x = CType(node_, TypeArrayNode)
                            x.Item = CType(f("Item", x.Item), TypeBaseNode)

                        Case TypeOf node_ Is TypeTupleNode

                            Dim x = CType(node_, TypeTupleNode)
                            For i = 0 To x.Items.List.Count - 1

                                x.Items.List(i) = CType(f($"[{i}]", x.Items.List(i)), TypeNode)
                            Next

                        Case TypeOf node_ Is TypeNode

                            Dim x = CType(node_, TypeNode)
                            x.Namespace = CType(f("Namespace", x.Namespace), TypeNode)
                            For i = 0 To x.Arguments.Count - 1

                                x.Arguments(i) = CType(f($"[{i}]", x.Arguments(i)), TypeNode)
                            Next

                        Case TypeOf node_ Is UnionNode

                            Dim x = CType(node_, UnionNode)
                            For i = 0 To x.Union.List.Count - 1

                                x.Union.List(i) = CType(f($"[{i}]", x.Union.List(i)), TypeNode)
                            Next

                        Case TypeOf node_ Is VariableNode,
                             TypeOf node_ Is NumericNode,
                             TypeOf node_ Is StringNode,
                             TypeOf node_ Is NullNode,
                             TypeOf node_ Is BreakNode

                            ' nothing

                        Case TypeOf node_ Is RootNode

                            Dim x = CType(node_, RootNode)
                            For Each key In x.Namespaces.Keys.ToList

                                x.Namespaces(key) = CType(f($"`{key}", x.Namespaces(key)), ProgramNode)
                            Next

                        Case IsGeneric(node_.GetType, GetType(ListNode(Of )))

                            Dim list = node_.GetType.GetProperty("List").GetValue(node_)
                            Dim count = list.GetType.GetProperty("Count")
                            Dim item = list.GetType.GetProperty("Item")
                            Dim index = New Object() {0}
                            For i = 0 To CInt(count.GetValue(list)) - 1

                                index(0) = i
                                item.SetValue(list, f($"`{i}", CType(item.GetValue(list, index), INode)), index)
                            Next

                        Case Else

                            Debug.Fail("unknown node")
                    End Select
                End Sub

            callback(Nothing, "", node, user, next_node)
        End Sub

    End Class

End Namespace

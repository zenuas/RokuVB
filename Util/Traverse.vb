Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Node


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
                For Each m In x.GetType.GetFields(flag)

                    Yield Tuple.Create(m.GetValue(x), m)
                Next
            End If

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

                        Case TypeOf node_ Is BlockNode

                            Dim x = CType(node_, BlockNode)
                            f("Owner", x.Owner)
                            For i = 0 To x.Statements.Count - 1

                                f($"[{i}]", x.Statements(i))
                            Next

                            For Each key In x.Scope.Keys

                                f($"`{key}", x.Scope(key))
                            Next

                        Case TypeOf node_ Is StructNode

                            Dim x = CType(node_, StructNode)
                            f("Owner", x.Owner)

                            For Each key In x.Scope.Keys

                                f($"`{key}", x.Scope(key))
                            Next

                        Case TypeOf node_ Is FunctionNode

                            Dim x = CType(node_, FunctionNode)
                            For i = 0 To x.Arguments.Length - 1

                                f($"[{i}]", x.Arguments(i))
                            Next
                            f("Return", x.Return)
                            f("Body", x.Body)

                        Case TypeOf node_ Is LetNode

                            Dim x = CType(node_, LetNode)
                            f("Var", x.Var)
                            f("Declare", x.Declare)
                            f("Expression", x.Expression)

                        Case TypeOf node_ Is ExpressionNode

                            Dim x = CType(node_, ExpressionNode)
                            f("Left", x.Left)
                            f("Right", x.Right)

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

                        Case TypeOf node_ Is IfNode

                            Dim x = CType(node_, IfNode)
                            f("Condition", x.Condition)
                            f("Then", x.Then)
                            f("Else", x.Else)
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

                        Case TypeOf node_ Is BlockNode

                            Dim x = CType(node_, BlockNode)
                            x.Owner = CType(f("Owner", x.Owner), IEvaluableNode)
                            For i = 0 To x.Statements.Count - 1

                                x.Statements(i) = CType(f($"[{i}]", x.Statements(i)), IEvaluableNode)
                            Next

                            For Each key In New List(Of String)(x.Scope.Keys)

                                x.Scope(key) = f($"`{key}", x.Scope(key))
                            Next

                        Case TypeOf node_ Is FunctionNode

                            Dim x = CType(node_, FunctionNode)
                            For i = 0 To x.Arguments.Length - 1

                                x.Arguments(i) = CType(f($"[{i}]", x.Arguments(i)), DeclareNode)
                            Next
                            x.Return = CType(f("Return", x.Return), TypeNode)
                            x.Body = CType(f("Body", x.Body), BlockNode)

                        Case TypeOf node_ Is LetNode

                            Dim x = CType(node_, LetNode)
                            x.Var = CType(f("Var", x.Var), VariableNode)
                            x.Expression = CType(f("Expression", x.Expression), IEvaluableNode)

                        Case TypeOf node_ Is ExpressionNode

                            Dim x = CType(node_, ExpressionNode)
                            x.Left = CType(f("Left", x.Left), IEvaluableNode)
                            x.Right = CType(f("Right", x.Right), IEvaluableNode)

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

                        Case TypeOf node_ Is IfNode

                            Dim x = CType(node_, IfNode)
                            x.Condition = CType(f("Condition", x.Condition), IEvaluableNode)
                            x.Then = CType(f("Then", x.Then), BlockNode)
                            x.Else = CType(f("Else", x.Else), IEvaluableNode)
                    End Select
                End Sub

            callback(Nothing, "", node, user, next_node)
        End Sub

    End Class

End Namespace

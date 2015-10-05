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

        Public Shared Sub Nodes(
                node As INode,
                callback As Action(Of INode, String, INode, Boolean)
            )

            Traverse.Nodes(node,
                Function(parent, ref, child, isfirst)

                    callback(parent, ref, child, isfirst)
                    Return isfirst
                End Function)

        End Sub

        Public Shared Sub Nodes(
                node As INode,
                callback As Func(Of INode, String, INode, Boolean, Boolean)
            )

            Traverse.Nodes(node,
                Function(parent, ref, child, replace, isfirst)

                    Return callback(parent, ref, child, isfirst)
                End Function)

        End Sub

        Public Shared Sub Nodes(
                node As INode,
                callback As Func(Of INode, String, INode, Action(Of INode), Boolean, Boolean)
            )

            Dim mark As New Dictionary(Of Integer, Boolean)
            Traverse.Nodes(node,
                Function(parent As INode, ref As String, child As INode, replace As Action(Of INode))

                    Dim child_hash = child.GetHashCode
                    Dim isfirst = Not mark.ContainsKey(child_hash)
                    If isfirst Then mark.Add(child_hash, True)
                    Return callback(parent, ref, child, replace, isfirst)
                End Function)

        End Sub

        Public Shared Sub Nodes(
                node As INode,
                callback As Func(Of INode, String, INode, Action(Of INode), Boolean)
            )

            If node Is Nothing Then Return
            If Not callback(Nothing, "", node, Nothing) Then Return

            Dim enum_nodes As Action(Of INode) =
                Sub(node_ As INode)

                    Dim f =
                        Sub(ref As String, v As INode, replace As Action(Of INode))

                            If v Is Nothing Then Return
                            If callback(node_, ref, v, replace) Then enum_nodes(v)
                        End Sub

                    Select Case True

                        Case TypeOf node_ Is BlockNode

                            Dim x = CType(node_, BlockNode)
                            f("Owner", x.Owner, Sub(new_node) x.Owner = CType(new_node, IEvaluableNode))
                            For i = 0 To x.Statements.Count - 1

                                Dim i_ = i
                                f(String.Format("[{0}]", i), x.Statements(i), Sub(new_node) x.Statements(i_) = CType(new_node, IEvaluableNode))
                            Next

                            Dim after As Dictionary(Of String, INode) = Nothing
                            For Each key In x.Scope.Keys

                                f(String.Format("`{0}", key), x.Scope(key),
                                    Sub(new_node)
                                        If after Is Nothing Then after = New Dictionary(Of String, INode)
                                        after(key) = new_node
                                    End Sub)
                            Next
                            If after IsNot Nothing Then

                                For Each key In after.Keys

                                    x.Scope(key) = after(key)
                                Next
                            End If

                        Case TypeOf node_ Is LetNode

                            Dim x = CType(node_, LetNode)
                            f("Var", x.Var, Sub(new_node) x.Var = CType(new_node, VariableNode))
                            f("Expression", x.Expression, Sub(new_node) x.Expression = CType(new_node, IEvaluableNode))

                        Case TypeOf node_ Is ExpressionNode

                            Dim x = CType(node_, ExpressionNode)
                            f("Left", x.Left, Sub(new_node) x.Left = CType(new_node, IEvaluableNode))
                            f("Right", x.Right, Sub(new_node) x.Right = CType(new_node, IEvaluableNode))

                        Case TypeOf node_ Is FunctionCallNode

                            Dim x = CType(node_, FunctionCallNode)
                            f("Expression", x.Expression, Sub(new_node) x.Expression = CType(new_node, IEvaluableNode))
                            For i = 0 To x.Arguments.Length - 1

                                Dim i_ = i
                                f(String.Format("[{0}]", i), x.Arguments(i), Sub(new_node) x.Arguments(i_) = CType(new_node, IEvaluableNode))
                            Next
                    End Select
                End Sub
            enum_nodes(node)

        End Sub


    End Class

End Namespace

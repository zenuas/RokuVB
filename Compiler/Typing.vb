Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Typing

        Public Shared Sub TypeInference(node As INode, root As RkNamespace)

            ' create prototype
            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = New RkStruct With {.Name = node_struct.Name}
                        node_struct.Type = rk_struct
                        current.Local.Add(rk_struct.Name, rk_struct)

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name}
                        node_func.Type = rk_func
                        current.Local.Add(rk_func.Name, rk_func)
                    End If

                    next_(child, current)
                End Sub)

            ' typing
            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = CType(node_struct.Type, RkStruct)

                        For Each x In node_struct.Scope.Values

                            If TypeOf x Is LetNode Then

                            End If
                        Next

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = New RkFunction With {.Name = node_func.Name}

                    End If

                    next_(child, current)
                End Sub)

        End Sub

    End Class

End Namespace

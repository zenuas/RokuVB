Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Translater

        Public Shared Sub Translate(node As INode, root As RkNamespace)

            Util.Traverse.NodesOnce(
                node,
                root,
                Sub(parent, ref, child, current, isfirst, next_)

                    If TypeOf child Is StructNode Then

                        Dim node_struct = CType(child, StructNode)
                        Dim rk_struct = CType(node_struct.Type, RkStruct)

                    ElseIf TypeOf child Is FunctionNode Then

                        Dim node_func = CType(child, FunctionNode)
                        Dim rk_func = CType(node_func.Type, RkFunction)

                    End If
                    next_(child, current)
                End Sub)
        End Sub

    End Class

End Namespace

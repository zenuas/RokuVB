Imports System
Imports Roku.Node
Imports Roku.Manager


Namespace Compiler

    Public Class Typing

        Public Shared Function Prototype(node As INode) As RkStruct

            Dim g As New RkStruct With {.Name = "Global"}

            Util.Traverse.NodesOnce(
                node,
                g,
                Sub(parent, ref, child, current, isfirst, next_)

                End Sub)

            Return g
        End Function

        Public Shared Function TypeInference(node As INode, sys As RkStruct) As RkStruct

            Dim g = Prototype(node)

            g.Local.Add(sys.Name, sys)

            Return g
        End Function

    End Class

End Namespace

Imports System
Imports Roku.Node


Namespace Compiler

    Public Class Normalize

        Public Shared Sub Normalization(node As INode)

            Dim block_search As Action(Of INode) =
                Sub(x As INode)

                    If TypeOf x Is BlockNode Then Return

                    Util.Traverse.Nodes(x,
                        Function(parent, ref, child, isfirst)

                            If Not isfirst Then Return isfirst

                            If TypeOf child Is BlockNode Then

                                ' x = a + b + c + d
                                ' 
                                ' $1 = a + b
                                ' $2 = $1 + c
                                ' $3 = $2 + d
                                ' x  = $3
                                Dim block = CType(child, BlockNode)
                                Dim var_index = 0
                                Dim i = 0
                                Do While i < block.Statements.Count

                                    Dim v = block.Statements(i)
                                    i += 1
                                Loop

                                Return False
                            End If

                            Return isfirst
                        End Function)
                End Sub

            block_search(node)
        End Sub

    End Class

End Namespace

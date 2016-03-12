Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class UseNode
        Inherits BaseNode


        Public Overridable Property [Alias] As String = ""
        Public Overridable Property [Namespace] As IEvaluableNode

        Public Overridable Function GetNamespace() As String

            Dim name_combine As Func(Of IEvaluableNode, String) =
                Function(x)
                    If TypeOf x Is VariableNode Then Return CType(x, VariableNode).Name
                    Dim expr = CType(x, ExpressionNode)
                    Return $"{name_combine(expr.Left)}{System.IO.Path.PathSeparator}{name_combine(expr.Right)}"
                End Function

            Return name_combine(Me.Namespace)
        End Function
    End Class

End Namespace

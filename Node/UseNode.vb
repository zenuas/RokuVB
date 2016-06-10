Imports System
Imports System.Collections.Generic
Imports Roku.Util.ArrayExtension


Namespace Node

    Public Class UseNode
        Inherits BaseNode


        Public Overridable Property [Alias] As String = ""
        Public Overridable Property [Namespace] As IEvaluableNode

        Public Overridable Function GetNamespace() As String

            Return String.Join(".", Me.GetNamespaceHierarchy)
        End Function

        Public Overridable Function GetNamespaceHierarchy() As String()

            Dim name_combine As Func(Of IEvaluableNode, IEnumerable(Of String)) =
                Function(x)
                    If TypeOf x Is VariableNode Then Return {CType(x, VariableNode).Name}
                    Dim expr = CType(x, ExpressionNode)
                    Return name_combine(expr.Left).Join(name_combine(expr.Right))
                End Function

            Return name_combine(Me.Namespace).ToArray
        End Function
    End Class

End Namespace

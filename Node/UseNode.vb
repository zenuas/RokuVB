Imports System
Imports System.Collections.Generic
Imports Roku.Util.Extensions


Namespace Node

    Public Class UseNode
        Inherits BaseNode


        Public Overridable Property [Alias] As String = ""
        Public Overridable Property [Namespace] As TypeNode
        Public Overridable Property [Module] As String

        Public Overridable Function GetNamespace() As String

            Return String.Join(".", Me.GetNamespaceHierarchy)
        End Function

        Public Overridable Function GetNamespaceHierarchy() As String()

            Dim name_combine As Func(Of TypeNode, IEnumerable(Of String)) =
                Function(x)
                    If x.Namespace Is Nothing Then Return {x.Name}
                    Return name_combine(x.Namespace).Join({x.Name})
                End Function

            Return name_combine(Me.Namespace).ToArray
        End Function
    End Class

End Namespace

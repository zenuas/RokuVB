Imports System.Text
Imports Roku.Manager
Imports Roku.Parser


Namespace Node

    Public Class StringNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(s As Token)

            Me.String.Append(s.Name)
            Me.AppendLineNumber(s)
        End Sub

        Public Overridable ReadOnly Property [String] As New StringBuilder
        Public Overridable Property Type As IType Implements IEvaluableNode.Type

    End Class

End Namespace

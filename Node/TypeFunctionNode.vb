Imports System.Collections.Generic
Imports Roku.Util.Extensions


Namespace Node

    Public Class TypeFunctionNode
        Inherits TypeBaseNode


        Public Overridable Property Arguments As New List(Of TypeBaseNode)
        Public Overridable Property [Return] As TypeBaseNode

        Public Overrides Function HasGeneric() As Boolean

            If Me.Return IsNot Nothing AndAlso Me.Return.HasGeneric Then Return True
            Return Me.IsGeneric OrElse Me.Arguments.Or(Function(x) x.HasGeneric)
        End Function

        Public Overrides Function ToString() As String

            Return $"{{{String.Join(", ", Me.Arguments)}{If(Me.Return Is Nothing, "", $" => {Me.Return}")}}}"
        End Function
    End Class

End Namespace

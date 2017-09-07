Imports Roku.Util.Extensions


Namespace Node

    Public Class TypeFunctionNode
        Inherits TypeNode


        Public Overridable Property Arguments As TypeNode()
        Public Overridable Property [Return] As TypeNode

        Public Overrides Function HasGeneric() As Boolean

            If Me.Return IsNot Nothing AndAlso Me.Return.HasGeneric Then Return True
            Return Me.IsGeneric OrElse Me.Arguments.Or(Function(x) x.HasGeneric)
        End Function
    End Class

End Namespace

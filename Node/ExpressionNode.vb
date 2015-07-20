Imports Roku.Manager


Namespace Node

    Public Class ExpressionNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property [Operator] As String = ""
        Public Overridable Property Left As IEvaluableNode = Nothing
        Public Overridable Property Right As IEvaluableNode = Nothing

        Public Overridable Property Type As InType Implements IEvaluableNode.Type

        Public Overridable ReadOnly Property Receiver() As InType Implements IEvaluableNode.Receiver
            Get
                Return Me.Left.Type
            End Get
        End Property
    End Class

End Namespace

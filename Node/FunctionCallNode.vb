Imports System
Imports Roku.Manager


Namespace Node

    Public Class FunctionCallNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(expr As IEvaluableNode, ParamArray args() As IEvaluableNode)

            Me.Expression = expr
            Me.Arguments = args
            Me.AppendLineNumber(expr)
        End Sub

        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Arguments As IEvaluableNode()
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
            Get
                Return Me.Function?.Return
            End Get
            Set(value As IType)

                Throw New NotSupportedException
            End Set
        End Property

        Public Overridable Property [Function] As IFunction
        Public Overridable Property FixedGenericFunction As FunctionNode
    End Class

End Namespace

Imports System
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


Namespace Node

    Public Class FunctionCallNode
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


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

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If t Is Me.Type Then Return False
            If TypeOf Me.Function.Return IsNot RkGenericEntry Then Return False

            Me.Function = CType(Me.Function.FixedGeneric(New NamedValue With {.Name = Me.Function.Generics(CType(Me.Function.Return, RkGenericEntry).ApplyIndex).Name, .Value = t}), IFunction)
            Me.Function.Arguments.Do(Sub(x, i) Me.Arguments(i).Type = x.Value)
            Return True
        End Function

        Public Overrides Function ToString() As String

            Return $"sub {Me.Expression}({String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))})"
        End Function
    End Class

End Namespace

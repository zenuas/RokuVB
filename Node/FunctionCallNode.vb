Imports System
Imports Roku.Manager
Imports Roku.Util.Extensions


Namespace Node

    Public Class FunctionCallNode
        Inherits BaseNode
        Implements IEvaluableNode, IStatementNode, IFeedback


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
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Property [Function] As IFunction
        Public Overridable Property FixedGenericFunction As FunctionNode

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If t Is Me.Type Then Return False

            If TypeOf Me.Function.Return Is RkGenericEntry Then

                Me.Function = CType(Me.Function.FixedGeneric(New NamedValue With {.Name = Me.Function.Generics(CType(Me.Function.Return, RkGenericEntry).ApplyIndex).Name, .Value = t}), IFunction)
                Me.Function.Arguments.Do(Sub(x, i) Me.Arguments(i).Type = x.Value)
                Return True

            ElseIf TypeOf Me.Function.Return Is RkSomeType AndAlso CType(Me.Function.Return, RkSomeType).HasIndefinite Then

                Return CType(Me.Function.Return, RkSomeType).Merge(t)
            Else

                Return False
            End If

        End Function

        Public Overrides Function ToString() As String

            Return $"sub {Me.Expression}({String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))})"
        End Function
    End Class

End Namespace

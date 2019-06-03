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
        Public Overridable Property UnaryOperator As Boolean = False
        Public Overridable Property OwnerSwitchNode As SwitchNode = Nothing

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If t Is Me.Type Then Return False

            Dim r = Me.Function.Return
            If TypeOf r Is RkGenericEntry Then

                If Me.Function.HasGeneric Then

                    Me.Function = CType(Me.Function.FixedGeneric(New NamedValue With {.Name = r.Name, .Value = t}), IFunction)
                    Me.Function.Arguments.Each(
                        Sub(x, i)

                            Dim arg = Me.Arguments(i)
                            If TypeOf arg Is IFeedback Then

                                CType(arg, IFeedback).Feedback(x.Value)
                            Else

                                arg.Type = x.Value
                            End If
                        End Sub)
                    Return True
                End If

            ElseIf TypeOf r Is RkUnionType AndAlso CType(r, RkUnionType).HasIndefinite Then

                Return CType(r, RkUnionType).Merge(t)
            End If

            Return False
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Expression}({String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))})"
        End Function
    End Class

End Namespace
